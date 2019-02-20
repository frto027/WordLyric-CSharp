using System;
using System.Collections.Generic;
using WordLyric;

namespace WordLyricConsole
{
    /// <summary>
    /// 一个非常简单的歌词播放器的实现，使用WordLyricAdapter，支持普通LRC格式的解析
    /// </summary>
    class Program
    {
        enum PlayStatus { PLAY, PAUSE };
        static PlayStatus CurrnetPlayStatus;

        static double CurrentTime;

        static DateTime lastupdatetime;

        static string LogOutUI = "";
        static int LogCount = 0;
        static void Log(string s) {
            if(LogCount % 10 == 0)
            {
                LogOutUI = "";
            }
            LogOutUI += $"log=>[{LogCount++}]" + s;
            for (int i = s.Length; i < 70; i++)
                LogOutUI += " ";
            LogOutUI +=  "\n";
        }

        //这里模拟一个音乐播放器，只要不断调用UpdatePlayerUI即可更新播放器的时间
        static void UpdatePlayerStatus()
        {
            if (lastupdatetime == null)
                lastupdatetime = DateTime.Now;
            DateTime nextTime = DateTime.Now;
            if (CurrnetPlayStatus == PlayStatus.PLAY)
            {
                CurrentTime += (nextTime - lastupdatetime).TotalSeconds;
            }
            lastupdatetime = nextTime;
        }

        static List<List<string>> LyricList = new List<List<string>>();
        static List<List<bool>> LyricIsActive = new List<List<bool>>();
        enum LineStatusEnum { PREPAIR,SHOWING,SHOWED };//还没出现，正在展示，展示完成
        static List<LineStatusEnum> LineStatus = new List<LineStatusEnum>(); 

        // 65.2 => "1:5.200" 128.7025 => "2:8.703"
        static string getTimeFormat(double time)
        {
            if (double.IsInfinity(time))
                return "∞:∞";
            int sec = ((int)time) / 60;
            double min = time - (sec * 60);
            return $"{sec}:{min.ToString("N3")}";
        }

        static void RenderUI()
        {
            Console.CursorTop = 0;
            Console.CursorLeft = 0;
            Console.WriteLine("[L]Load [Q]exit [Space]Pause/Play [S]Stop [left]-1s [right]+1s [down]-10s [up]+10s");
            Console.WriteLine($"play status:{(CurrnetPlayStatus == PlayStatus.PAUSE ? "PAUSE" : "PLAY ")} ");
            Console.WriteLine($"TITLE:{Title}                                              ");
            Console.WriteLine($"[lrc arg]:next update time:[{getTimeFormat(suggestNextUpdate)}]                 ");

            Console.WriteLine($"progress: [{getTimeFormat(CurrentTime)}]                           ");

            for (int i = 0; i < LyricList.Count; i++)
            {
                Console.Write($"[{(int)LineStatus[i]}]");

                var GroupList = LyricList[i];
                var GroupActiveList = LyricIsActive[i];
                for(int j = 0; j < GroupList.Count; j++)
                {
                    if(LineStatus[i] == LineStatusEnum.PREPAIR)
                    {
                        //这行歌词还没有显示
                        Console.Write($"({GroupList[j]})");
                    }
                    else
                    {
                        //这行歌词正在显示或者已经显示过了
                        if (GroupActiveList[j])
                        {
                            Console.Write($"[{GroupList[j]}]");
                        }
                        else
                        {
                            Console.Write($"<{GroupList[j]}>");
                        }
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine(LogOutUI);
        }

        static void HandleKey()
        {
            if (Console.KeyAvailable)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Spacebar:
                        Pause();
                        break;
                    case ConsoleKey.S:
                        Stop();
                        break;
                    case ConsoleKey.LeftArrow:
                        CurrentTime -= 1;
                        if (CurrentTime < 0)
                            CurrentTime = 0;
                        UpdateLyric();
                        break;
                    case ConsoleKey.RightArrow:
                        CurrentTime += 1;
                        break;
                    case ConsoleKey.UpArrow:
                        CurrentTime += 10;
                        break;
                    case ConsoleKey.DownArrow:
                        CurrentTime -= 10;
                        if (CurrentTime < 0)
                            CurrentTime = 0;
                        UpdateLyric();
                        break;
                    case ConsoleKey.L:
                        Load();
                        break;
                    case ConsoleKey.Q:
                        Environment.Exit(0);
                        break;
                }
            }
        }
        static void Play()
        {
            CurrnetPlayStatus = PlayStatus.PLAY;
        }
        static void Pause()
        {
            if (CurrnetPlayStatus == PlayStatus.PLAY)
                CurrnetPlayStatus = PlayStatus.PAUSE;
            else
                CurrnetPlayStatus = PlayStatus.PLAY;
        }
        static void Stop()
        {
            CurrentTime = 0;
            CurrnetPlayStatus = PlayStatus.PAUSE;
            suggestNextUpdate = -1;

            UpdateLyric();
        }
        //define Lyric Adapter
        static WordLyricAdapter wordLyricAdapter = new WordLyricAdapter();
        static string Title = "NONE";

        static float suggestNextUpdate = -1;
        //当修改进度后也应该手动触发
        static void UpdateLyric()
        {
            //如果正常播放，在suggestNextUpdate之前调用这个都没用
            suggestNextUpdate = wordLyricAdapter.SeekTo((float)CurrentTime);
        }

        static void Load()
        {
            //Reset UI
            LyricList.Clear();
            LyricIsActive.Clear();

            //Load Lyric
            WordLyric.WordLyric wordLyric = WordLyric.WordLyric.FromLrc(LyricPreset);
            wordLyricAdapter.LoadLyric(wordLyric);
            Title = wordLyric.Title;
            UpdateLyric();
        }

        static void Main(string[] args)
        {
            Stop();
            Console.CursorVisible = false;

            wordLyricAdapter.OnAddLyricLine += WordLyricAdapter_OnAddLyricLine;
            wordLyricAdapter.OnActiveLyricLine += WordLyricAdapter_OnActiveLyricLine;
            wordLyricAdapter.OnUnActiveLyricLine += WordLyricAdapter_OnUnActiveLyricLine;
            wordLyricAdapter.OnActiveGroup += WordLyricAdapter_OnActiveGroup;

            while (true)
            {
                HandleKey();
                UpdatePlayerStatus();
                RenderUI();
                //歌词修改没必要时刻触发，触发条件是下次歌词所在行有改动前，或者进度条变动前
                if (CurrentTime >= suggestNextUpdate)
                    UpdateLyric();
                //通常这个sleep时间取决于UI线程
                System.Threading.Thread.Sleep(50);
            }
        }
        private static void WordLyricAdapter_OnAddLyricLine(WordLyric.WordLyric lyricLines)
        {
            LineStatus = new List<LineStatusEnum>();
            LyricList.Clear();
            LyricIsActive.Clear();

            foreach (LyricLine line in lyricLines)
            {
                List<string> grouptexts = new List<string>();
                List<bool> isActive = new List<bool>();
                foreach (WordGroup wordGroup in line.LyricGroup)//当前还有翻译，我就不做显示了，同理
                {
                    grouptexts.Add(wordGroup.Text);
                    isActive.Add(false);
                }

                //所有行默认都是在光标所在行之后的状态
                LineStatus.Add(LineStatusEnum.PREPAIR);
                LyricList.Add(grouptexts);
                LyricIsActive.Add(isActive);
            }
        }

        private static void WordLyricAdapter_OnActiveLyricLine(WordLyricAdapter.LineInfoBundle bundle)
        {
            Log($"Active line{bundle.LineNumber}");
            //assert(bundle.GroupActiveInfo != null)
            //说明正在展示这一行
            LineStatus[bundle.LineNumber] = LineStatusEnum.SHOWING;
            //bundle.GroupActiveInfo数组存储各个激活情况
            for (int i = 0; i < bundle.GroupActiveInfo.Length; i++)
            {
                LyricIsActive[bundle.LineNumber][i] = bundle.GroupActiveInfo[i];
            }
        }

        private static void WordLyricAdapter_OnActiveGroup(WordLyricAdapter.GroupInfoBundle bundle)
        {
            if (bundle.IsTranslate)
                return;//这里不处理翻译歌词的情况

            Log($"Active group ${bundle.GroupId} at line {bundle.LineNumber}");

            LyricIsActive[bundle.LineNumber][bundle.GroupId] = true;
            //Play some anim here
            string Style = bundle.LyricLine.Style;
            //anim is relative to style
            //你该根据Style放动画了
        }

        private static void WordLyricAdapter_OnUnActiveLyricLine(WordLyricAdapter.LineInfoBundle bundle)
        {
            Log($"unactive line {bundle.LineNumber}");
            if (bundle.GroupActiveInfo == null)
                LineStatus[bundle.LineNumber] = LineStatusEnum.PREPAIR;//光标退回到这行前面了
            else
            {
                LineStatus[bundle.LineNumber] = LineStatusEnum.SHOWED;//光标在这行之后
                //每行都有它播放完毕时的激活情况，
                for (int i = 0; i < bundle.GroupActiveInfo.Length; i++)
                {
                    LyricIsActive[bundle.LineNumber][i] = bundle.GroupActiveInfo[i];
                }
            }
        }

        static string LyricPreset = @"[encoding:utf-8]
[:]虽然这是分字lrc格式，但SimpleLyricAdapter并不支持分字
[ttm:=]
[ttsm:-]
[sm:++]
[st:@]
[ti:beautiful world]
[:]@style1
[:]-2,6|99|4.3,3|5,2
[:]=hello, world
[:]++2,3|4.3|5
[1:02.30]你好，世界
[:]@style2
[:]=other lrc
[:]++1|3|4|2
[0:03.68]其他歌词
[1:15.68]其他歌词2";
    }


}

