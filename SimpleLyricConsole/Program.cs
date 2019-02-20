using System;
using System.Collections.Generic;
using WordLyric;
namespace SimpleLyricConsole
{
    /// <summary>
    /// 一个非常简单的歌词播放器的实现，使用SimpleLyricAdapter，支持普通LRC格式的解析
    /// </summary>
    class Program
    {
        enum PlayStatus { PLAY,PAUSE };
        static PlayStatus CurrnetPlayStatus;

        static double CurrentTime;

        static DateTime lastupdatetime;
        //这里模拟一个音乐播放器，只要不断调用UpdatePlayerUI即可更新播放器的时间
        static void UpdatePlayerStatus()
        {
            if (lastupdatetime == null)
                lastupdatetime = DateTime.Now;
            DateTime nextTime = DateTime.Now;
            if(CurrnetPlayStatus == PlayStatus.PLAY)
            {
                CurrentTime += (nextTime - lastupdatetime).TotalSeconds;
            }
            lastupdatetime = nextTime;
        }

        static List<string> LyricList = new List<string>();
        static List<bool> LyricIsActive = new List<bool>();
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
            
            for(int i = 0; i < LyricList.Count; i++)
            {
                Console.Write(LyricIsActive[i] ? "[>]" : "[-]");
                Console.WriteLine(LyricList[i]);
            }
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
        static SimpleLyricAdapter simpleLyricAdapter = new SimpleLyricAdapter();
        static string Title = "NONE";

        static float suggestNextUpdate = -1;
        //当修改进度后也应该手动触发
        static void UpdateLyric()
        {
            //如果正常播放，在suggestNextUpdate之前调用这个都没用
            suggestNextUpdate = simpleLyricAdapter.SeekTo((float)CurrentTime);
        }

        static void Load()
        {
            //Reset UI
            LyricList.Clear();
            LyricIsActive.Clear();

            //Load Lyric
            WordLyric.WordLyric wordLyric = WordLyric.WordLyric.FromLrc(LyricPreset);
            simpleLyricAdapter.LoadLyric(wordLyric);
            Title = wordLyric.Title;
            UpdateLyric();
        }

        static void Main(string[] args)
        {
            Stop();
            Console.CursorVisible = false;

            //setup callback
            simpleLyricAdapter.OnAddLyricLine += SimpleLyricAdapter_OnAddLyricLine;
            simpleLyricAdapter.OnUnActiveLine += SimpleLyricAdapter_OnUnActiveLine;
            simpleLyricAdapter.OnActiveLine += SimpleLyricAdapter_OnActiveLine;

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
        //重写这三个函数，这些函数会在将来调用ProcessTo或者LoadLyric的时候被调用
        private static void SimpleLyricAdapter_OnActiveLine(SimpleLyricAdapter.LyricLineBundle bundle)
        {
            LyricIsActive[bundle.LineNumber] = true;
        }

        private static void SimpleLyricAdapter_OnUnActiveLine(SimpleLyricAdapter.LyricLineBundle bundle)
        {
            LyricIsActive[bundle.LineNumber] = false;
        }

        private static void SimpleLyricAdapter_OnAddLyricLine(IList<SimpleLyricAdapter.LyricLineBundle> bundles)
        {
            LyricList.Clear();
            LyricIsActive.Clear();

            foreach(var bundle in bundles)
            {
                LyricList.Add(bundle.LineText);
                LyricIsActive.Add(false);
                //assert(LyricList.size == linecode + 1)
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
[1:00.68]其他歌词
[1:15.68]其他歌词2";
    }

    
}

