using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Audio;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.Media;
using Windows.Media.Core;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace WordLyricGUIEditor
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        WordLyric.WordLyricAdapter lyricAdapter;
        MediaPlayer player;
        MediaTimelineController controller;
        

        string logFolder;
        
        public MainPage()
        {
            this.InitializeComponent();
            InitializeLogStorage();
            InitializeNAudio();
            InitializeWordLyricAdapter();
            InitializeRythmSyncComponent();
            InitializeUIUpdate();

            
        }
        #region RythmSync
        private void InitializeRythmSyncComponent()
        {
            RythmSync.mainPage = this;
            //RythmTimer.Start();
        }

        DispatcherTimer RythmTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100) };
        #endregion

        #region LOGFILE
        private void InitializeLogStorage()
        {
            var cachefolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            logFolder = Path.Combine(cachefolder.Path, "logs");
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);

            LogPringFilePath = Path.Combine(logFolder, "logout.log");
            LogFileTextBox.Text = logFolder ?? "ERROR";
        }
        private static string LogPringFilePath;
        public static void LogPrint(string s)
        {
            File.AppendAllText( LogPringFilePath, DateTime.Now.ToString() + "|" + s + "\n");
        }
        #endregion

        #region VLC
        private void InitializeNAudio()
        {
            player = new MediaPlayer();


            player.PlaybackSession.NaturalDurationChanged += async (s, o) =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                 {
                     ProgressSlider.Maximum = s.NaturalDuration.TotalMilliseconds;
                 });
            };

            player.Volume = VolumeSlider.Value / 100;

            /*
            player.VolumeChanged += (mp, s) =>
            {
                //VolumeSlider.Value = mp.Volume * 100;
            };
            */
            controller = player.TimelineController = new Windows.Media.MediaTimelineController();
        }

        private void UnLoadMedia()
        {
            /*
            if(media != null)
            {
                player.Media = null;
                media.Dispose();
                media = null;

            }
            */
            //UpdateUI();
        }

        private async Task LoadMedia(Windows.Storage.StorageFile mediapath)
        {
            LogPrint($"Load media at [{mediapath.Path}]");
            UnLoadMedia();

            //player.SetMediaSource(FFmpegInterop.FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, true, false).GetMediaStreamSource());
            /*
            FFmpegInterop.FFmpegInteropMSS mss = FFmpegInterop.FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(await mediapath.OpenReadAsync(), true, false);
            MediaStreamSource source = mss.GetMediaStreamSource();
            source.BufferTime = source.Duration;
            source.CanSeek = true;
            
            player.Source = MediaSource.CreateFromMediaStreamSource(source);
            */
            player.Source = MediaSource.CreateFromStorageFile(mediapath); //MediaSource.CreateFromMediaStreamSource(FFmpegInterop.FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, true, false).GetMediaStreamSource());
            
            UpdateUI();
        }

        public long GetNowTimeMs()
        {
            return (long)controller.Position.TotalMilliseconds;
        }
        public MediaPlaybackState GetPlayStatus()
        {
            return player?.PlaybackSession?.PlaybackState ?? MediaPlaybackState.None;
        }

        public void PlayBetween(long startms,long endms)
        {
            LogPrint($"bet {startms} {endms}");
            controller.Duration = TimeSpan.FromMilliseconds(endms);
            controller.Resume();
            controller.Position = TimeSpan.FromMilliseconds(startms);
        }
        #endregion

        #region LRC

        private void InitializeWordLyricAdapter()
        {
            lyricAdapter = new WordLyric.WordLyricAdapter();
            lyricAdapter.OnAddLyricLine += (line) => {
                LyricShowControl.LoadLyric(line, false);
                LyricTranslateShowControl.LoadLyric(line, true);
            };
            lyricAdapter.OnActiveLyricLine += (info) => {
                LyricShowControl.SetLyricLine(info.LineNumber, info.GroupActiveInfo);
                LyricTranslateShowControl.SetLyricLine(info.LineNumber, info.TranslateActiveInfo);
                StyleTextBox.Text = info.LyricLine.Style;
            };
            lyricAdapter.OnActiveGroup += (info) =>
            {
                //LogPrint($"Active group {info.GroupId} in line {info.LineNumber}");
                if (!info.IsTranslate)
                    LyricShowControl.ActiveGroup(info.LineNumber, info.GroupId);
                else
                    LyricTranslateShowControl.ActiveGroup(info.LineNumber, info.GroupId);
            };

            LrcUpdateTimer.Tick += (s, e) =>
            {
                if (player?.PlaybackSession?.PlaybackState == MediaPlaybackState.Playing)
                {
                    //LogPrint($"media seek to {player.Time / 1000f}");
                    UpdateLyricProgress();
                }
                else
                {
                    LrcUpdateTimer.Stop();
                    LogPrint("media player State:" + (player?.PlaybackSession?.PlaybackState)?.ToString()??"NO PLAYER");
                }
            };
        }
        private void UpdateLyricProgress()
        {
            lyricAdapter.SeekTo(GetNowTimeMs() / 1000f);
        }
        private void LoadLyric(byte[] lrcbyte)
        {
            var wlrc = WordLyric.WordLyric.FromBytes(lrcbyte);
            LogPrint($"Lyric has {wlrc.Length} line");
            lyricAdapter.LoadLyric(wlrc);
        }

        #endregion

        #region UI控件触发的函数
        private async void LoadMusicButtonClick(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".mkv");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            
            if(file != null)
            {
                await LoadMedia(file);
            }
        }
        private async void LoadLyricButtonClick(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".lrc");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
           
            if (file != null)
            {
                LogPrint($"Load lyric {file.Path}");
                LogPrint($"Lyric will load.");
                var stream = await file.OpenStreamForReadAsync();

  
                byte[] ret = new byte[stream.Length];
                int over = 0;
                while(over != ret.Length)
                {
                    int r = await stream.ReadAsync(ret, over, ret.Length - over);
                    if (r >= 0)
                        over += r;
                    else
                    {
                        LogPrint("Read file REACH TO END");
                        byte[] narr = new byte[over];
                        Array.Copy(ret, narr, over);
                        ret = narr;
                        break;
                    }
                }

                LogPrint($"Lyric file read over.len = {ret.Length}");
                LoadLyric(ret);
            }
        }
        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            if(player != null)
            {
                player?.TimelineController?.Pause();
                controller.Position = TimeSpan.Zero;
            }

            UpdateUI();
            UpdateLyricProgress();
        }

        private void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            controller.Pause();
            TimeSpan oldtime = controller.Position;
            controller.Duration = null;
            controller.Resume();
            controller.Position = oldtime;
            LrcUpdateTimer.Start();
        }

        private void PauseButtonClick(object sender, RoutedEventArgs e)
        {
            player?.TimelineController?.Pause();
        }
        private void VolumeSliderSlide(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (player != null)
                player.Volume = VolumeSlider.Value / 100;
        }

        private void MusicPlayerRateTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            float rt;
            if(player != null && float.TryParse(MusicPlayerRateTextBox.Text,out rt))
            {
                controller.ClockRate = rt;
            }
        }
        #endregion

        private static string ToTimeFormatString(long ms)
        {
            if (ms < 0)
                ms = 0;
            long sec = ms / 1000;
            ms %= 1000;
            long min = sec / 60;
            sec %= 60;
            long h = min / 60;
            min %= 60;
            string sh = h.ToString();
            if (h < 10) sh = "0" + sh;
            string sm = min.ToString();
            if (min < 10) sm = "0" + sm;
            string ss = sec.ToString();
            if (sec < 10) ss = "0" + ss;
            string fms = ms.ToString();
            while (fms.Length < 3)
                fms = "0" + fms;
            return $"{sh}:{sm}:{ss}.{fms}";
        }

        #region 更新UI的函数
        void InitializeUIUpdate()
        {
            UpdateUITimer.Start();
            UpdateUITimer.Tick += (s, e) =>
            {
                UpdateUI();
            };
        }
        void UpdateUI()
        {
            Update_time_textview();
            ChangeProgressSliderMute(controller.Position.TotalMilliseconds);
        }

        DispatcherTimer UpdateUITimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(50) };

        void Update_time_textview()
        {
            if (player == null)
                all_time_textview.Text = "??";
            else
                all_time_textview.Text = ToTimeFormatString((long)((player?.PlaybackSession?.NaturalDuration)?.TotalMilliseconds));
            if (player == null)
                now_time_textview.Text = "??";
            else
                now_time_textview.Text = ToTimeFormatString(GetNowTimeMs());

        }

        DispatcherTimer LrcUpdateTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };

        int SliderValueChangedHangup = 0;
        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (SliderValueChangedHangup == 0)
            {
                
                controller.Duration = null;
                controller.Position = TimeSpan.FromMilliseconds(ProgressSlider.Value);
            }
        }
        private void ChangeProgressSliderMute(double val)
        {
            SliderValueChangedHangup++;
            ProgressSlider.Value = val;
            SliderValueChangedHangup--;
        }
        #endregion


    }
}
