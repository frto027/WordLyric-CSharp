using System;
using System.Collections.Generic;
using System.Text;

namespace WordLyric
{

    public class WordLyricAdapter : LyricAdapterBase
    {
        public struct LineInfoBundle
        {
            /// <summary>
            /// 激活行的信息
            /// </summary>
            public LyricLine LyricLine;
            /// <summary>
            /// LyricLine所在的行号(从0开始)
            /// </summary>
            public int LineNumber;
            /// <summary>
            /// 行内各激活组的信息，数组长度与行内激活组的个数相同
            /// 注意可能为null，表示当前行位于光标所在行之后
            /// </summary>
            public bool[] GroupActiveInfo;
            /// <summary>
            /// 行的翻译激活组信息激活状态
            /// 注意可能为null，表示当前行位于光标所在行之后
            /// </summary>
            public bool[] TranslateActiveInfo;
        }
        public struct GroupInfoBundle
        {
            /// <summary>
            /// 组所在的行
            /// </summary>
            public LyricLine LyricLine;
            /// <summary>
            /// LyricLine的行号
            /// </summary>
            public int LineNumber;
            /// <summary>
            /// 这个激活组是否是翻译文本的激活组
            /// </summary>
            public bool IsTranslate;
            /// <summary>
            /// 激活组
            /// </summary>
            public WordGroup Group;
            /// <summary>
            /// 激活组在当前行的编号，编号从0开始，从左到右连续递增
            /// </summary>
            public int GroupId;
        }
        /// <summary>
        /// 歌词初始化时事件触发
        /// </summary>
        /// <param name="lyricLines">整个歌词</param>
        public delegate void AddLyricLine(WordLyric lyricLines);
        /// <summary>
        /// 当某行被激活时触发
        /// </summary>
        public delegate void ActiveLyricLine(LineInfoBundle bundle);
        /// <summary>
        /// 当某行被取消激活时触发
        /// </summary>
        public delegate void UnActiveLyricLine(LineInfoBundle bundle);
        /// <summary>
        /// 激活行内某组
        /// </summary>
        public delegate void ActiveGroup(GroupInfoBundle bundle);
        /*
        /// <summary>
        /// 取消激活行内某组(如进度条向前拖拽但并没有移开当前行)
        /// </summary>
        public delegate void UnActiveGroup(GroupInfoBundle bundle);
        */
        public event AddLyricLine OnAddLyricLine;
        public event ActiveLyricLine OnActiveLyricLine;
        public event UnActiveLyricLine OnUnActiveLyricLine;
        public event ActiveGroup OnActiveGroup;
        /*
        public event UnActiveGroup OnUnActiveGroup;
        */
        private int CurrentCusor = -1;
        private SortedList<float, FlagCommand> commandList = new SortedList<float, FlagCommand>();
        


        public override void LoadLyric(WordLyric lyric)
        {
            OnAddLyricLine?.Invoke(lyric);

            commandList = new SortedList<float, FlagCommand>();
            CurrentCusor = -1;
            for(int i = 0; i < lyric.Length; i++)
            {
                var line = lyric[i];
                //行切换的指令
                LineCommand lineCmd = new LineCommand(line) { linecode = i };
                if (i + 1 < lyric.Length)
                    lineCmd.SetupExitStatus(lyric[i + 1].ActiveTime);
                else
                    lineCmd.SetupExitStatus(float.PositiveInfinity);
                
                commandList.Add(line.ActiveTime,lineCmd);
                //组切换的指令
                HandleGroupCollection(lineCmd,line.LyricGroup, false, lineCmd.enter_group_status, lineCmd.exit_group_status);
                HandleGroupCollection(lineCmd,line.TranslateGroup, true, lineCmd.enter_group_status_translate, lineCmd.exit_group_status_translate);
            }
        }
        //只会加入必要的组
        private void HandleGroupCollection(LineCommand parentCmd,WordGroupCollection coll, bool isTranslate, bool[] enterStatus, bool[] exitStatus)
        {
            int i = 0;
            foreach (var gp in coll)
            {
                if ((!enterStatus[i]) && exitStatus[i])
                {
                    FlagCommand x;

                    if (commandList.ContainsKey(gp.ActiveTime))
                    {
                        x = commandList[gp.ActiveTime];
                        if (x is LineCommand)
                            throw new Exception("duplicate timeline(crash between line and group)");
                    }
                    else
                    {
                        x = new GroupCommand() { ParentLine = parentCmd };
                        commandList.Add(gp.ActiveTime, x);
                    }
                    (x as GroupCommand).gpList.Add(new GroupInfos()
                    {
                        group = gp,
                        id = i,
                        isTranslate = isTranslate
                    });
                }
                i++;
            }
        }

        public override float SeekTo(float time)
        {
            int newcusor = CurrentCusor;
            //从中间刻度修正当前状态
            void FixMidLine()
            {
                LineCommand curLine = commandList.Values[CurrentCusor].GetLineCommand();
                bool[] arr = (bool[])curLine.enter_group_status.Clone();
                bool[] arr_trans = (bool[])curLine.enter_group_status_translate.Clone();
                {
                    //回溯已经激活的组信息
                    int temp = CurrentCusor;
                    while (commandList.Values[temp] != curLine)
                    {
                        GroupCommand group = (GroupCommand)commandList.Values[temp];
                        foreach (var gp in group.gpList)
                        {
                            if (gp.isTranslate)
                                arr_trans[gp.id] = true;
                            else
                                arr[gp.id] = true;
                        }
                        temp--;
                    }
                    OnActiveLyricLine?.Invoke(new LineInfoBundle()
                    {
                        LyricLine = curLine.line,
                        LineNumber = curLine.linecode,
                        GroupActiveInfo = arr,
                        TranslateActiveInfo = arr_trans
                    });
                }
            }
            if (CurrentCusor < 0 || commandList.Keys[CurrentCusor] < time)
            {
                //CorrentCusor++
                
                while (newcusor + 1 < commandList.Count && commandList.Keys[newcusor + 1] <= time)
                    newcusor++;
                if(newcusor > CurrentCusor)
                {
                    //now turn to newcusor
                    //CurrentCusor may equals -1
                    if(CurrentCusor != newcusor - 1)
                    {
                        //需要重置状态
                        while(CurrentCusor + 1< newcusor)
                        {
                            FlagCommand ncmd = commandList.Values[CurrentCusor + 1];
                            if(ncmd is LineCommand)
                            {
                                if(CurrentCusor != -1)
                                {
                                    //DeActive CurrentCusor as exit status
                                    LineCommand lcmd = commandList.Values[CurrentCusor].GetLineCommand();
                                    OnUnActiveLyricLine?.Invoke(new LineInfoBundle()
                                    {
                                        LyricLine = lcmd.line,
                                        LineNumber = lcmd.linecode,
                                        GroupActiveInfo = lcmd.exit_group_status,
                                        TranslateActiveInfo = lcmd.exit_group_status_translate
                                    });
                                }
                            }
                            CurrentCusor++;
                        }//while(CurrentCusor + 1< newcusor)
                        if(commandList.Values[newcusor] is GroupCommand)
                        {
                            //现在位于行中间，回溯激活当前行
                            FixMidLine();
                        }
                        
                    }
                    //现在CurrentCusor == newcusor - 1了，而且状态正常
                    //下一行是Line，则UnActive当前Line，并Active下一行的Line，下一行是Group，则Active这个Line，并激活Group
                    if (commandList.Values[newcusor] is GroupCommand)
                    {
                        //激活新Group
                        CurrentCusor++;
                        LineCommand curLine = commandList.Values[CurrentCusor].GetLineCommand();
                        //assert CurrentCusor == newcusor
                        foreach (var x in ((GroupCommand)commandList.Values[CurrentCusor]).gpList)
                        {
                            OnActiveGroup?.Invoke(new GroupInfoBundle()
                            {
                                Group = x.group,
                                GroupId = x.id,
                                IsTranslate = x.isTranslate,
                                LineNumber = curLine.linecode,
                                LyricLine = curLine.line
                            });
                        }
                    }
                    else
                    {//下面是新的一行(正常换行走这里)，UnActive当前行并激活新行
                        if (CurrentCusor >= 0)
                        {
                            LineCommand lcmd = commandList.Values[CurrentCusor].GetLineCommand();
                            OnUnActiveLyricLine?.Invoke(new LineInfoBundle()
                            {
                                LyricLine = lcmd.line,
                                LineNumber = lcmd.linecode,
                                GroupActiveInfo = lcmd.exit_group_status,
                                TranslateActiveInfo = lcmd.exit_group_status_translate
                            });
                        }
                        CurrentCusor++;
                        {//Active new line
                            LineCommand lcmd = (LineCommand)commandList.Values[CurrentCusor];
                            OnActiveLyricLine?.Invoke(new LineInfoBundle()
                            {
                                LyricLine = lcmd.line,
                                LineNumber = lcmd.linecode,
                                GroupActiveInfo = lcmd.enter_group_status,
                                TranslateActiveInfo = lcmd.enter_group_status_translate
                            });
                        }
                    }

                }
                //CurrentCusor ++ end
            }
            else
            {
                //CurrentCusor-- or keep
                while (newcusor >= 0 && commandList.Keys[newcusor] > time)
                    newcusor--;
                if(newcusor < CurrentCusor)
                {
                    //new turn to newcusor
                    //newcusor may equals -1
                    while(newcusor < CurrentCusor)
                    {
                        if(commandList.Values[CurrentCusor] is LineCommand)
                        {
                            //UnActinve this line and all group
                            LineCommand lcmd = (LineCommand)commandList.Values[CurrentCusor];
                            OnUnActiveLyricLine?.Invoke(new LineInfoBundle()
                            {
                                LineNumber = lcmd.linecode,
                                LyricLine = lcmd.line,
                                GroupActiveInfo = null,//传值全false
                                TranslateActiveInfo = null
                            });
                        }
                        CurrentCusor--;
                    }
                    //回溯当前行
                    if (CurrentCusor >= 0)
                        FixMidLine();
                }
                //CurrentCusor-- end
            }
            if (CurrentCusor + 1 < commandList.Count)
                return commandList.Keys[CurrentCusor + 1];
            else
                return float.PositiveInfinity;
        }

        private abstract class FlagCommand {
            public abstract LineCommand GetLineCommand();
        }
        private class LineCommand : FlagCommand {
            public LyricLine line;
            public int linecode;
            public bool[] enter_group_status,enter_group_status_translate;
            public bool[] exit_group_status, exit_group_status_translate;

            public float next_line_time;

            private bool[] getEnterStatus(WordGroupCollection coll)
            {
                bool[] r = new bool[coll.Count];
                for(int i = 0; i < r.Length; i++)
                {
                    r[i] = coll[i].ActiveOffset <= 0;
                }
                return r;
            }

            private bool[] getExitStatus(WordGroupCollection coll,float nextLineTime)
            {
                next_line_time = nextLineTime;
                bool[] r = new bool[coll.Count];
                for(int i = 0; i < r.Length; i++)
                {
                    r[i] = coll[i].isActive(nextLineTime);
                }
                return r;
            }

            public LineCommand(LyricLine line)
            {
                this.line = line;
                enter_group_status = getEnterStatus(line.LyricGroup);
                enter_group_status_translate = getEnterStatus(line.TranslateGroup);
            }
            public void SetupExitStatus(float nextLineTime)
            {
                exit_group_status = getExitStatus(line.LyricGroup, nextLineTime);
                exit_group_status_translate = getExitStatus(line.TranslateGroup, nextLineTime);
            }

            public override LineCommand GetLineCommand()
            {
                return this;
            }
        }
        private struct GroupInfos
        {
            public WordGroup group;
            public int id;
            public bool isTranslate;
        }
        private class GroupCommand : FlagCommand {
            public LineCommand ParentLine;
            public List<GroupInfos> gpList = new List<GroupInfos>();

            public override LineCommand GetLineCommand()
            {
                return ParentLine;
            }
        }
    }
}
