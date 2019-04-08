using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace WordLyricGUIEditor
{
    public sealed partial class RythmSyncornizerBar : UserControl
    {
        public double StartMs;
        public double BetweenMs {
            get => (double) (_BetweenMs / Factor);
            set => _BetweenMs = (double)(value * Factor);
        }
        public double Factor {
            get => _Factor;
            set {
                _Factor = value;
                RateTextBlock.Text = _Factor.ToString("N7").TrimEnd('0').TrimEnd('.');
            }
        }

        private double _Factor;

        private double _BetweenMs;

        public MainPage mainPage;

        public event Action<RythmSyncornizerBar> OnRemoveMe;


        private double SeekTo(double TimeMs)
        {
            if (_BetweenMs == 0)
                return 0;
            if (StartMs > TimeMs)
            {
                StartMs %= _BetweenMs;
                StartMs -= _BetweenMs;
            }
            double val = ((TimeMs - StartMs) % _BetweenMs) / (double)_BetweenMs;

            return val;
        }

        public RythmSyncornizerBar()
        {
            this.InitializeComponent();
            
        }
        private void CanvasAnimatedControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            
            {
                double value = SeekTo((float)mainPage.GetNowTimeMs());
                // MainPage.LogPrint($"STime {value}");
                double SmallRect = (0.5 - Math.Abs(value - 0.5))*2;

                args.DrawingSession.DrawRectangle(0, 0, (float)sender.Size.Width * 0.7f, (float)sender.Size.Height * 0.5f, Colors.Gray);
                args.DrawingSession.DrawLine(0, (float)sender.Size.Height * 0.25f, (float)SmallRect * (float)sender.Size.Width * 0.7f, (float)sender.Size.Height * 0.25f, Colors.Green);
                args.DrawingSession.DrawLine((float)sender.Size.Width, 0, (float)sender.Size.Width, (float)sender.Size.Height, Colors.Gray);
                args.DrawingSession.FillRectangle(0, (float)sender.Size.Height * 0.5f, (float)(sender.Size.Width * value), (float)sender.Size.Height * 0.5f, Colors.Blue);
            }

            if ((mainPage?.GetPlayStatus() != Windows.Media.Playback.MediaPlaybackState.Playing))
                args.DrawingSession.DrawRectangle(0, 0, (float)sender.Size.Width, (float)sender.Size.Height, Colors.Red);

        }

        private void RemoveButtonClick(object sender, RoutedEventArgs e)
        {
            OnRemoveMe?.Invoke(this);
            cvs.RemoveFromVisualTree();
            cvs = null;
        }

        private void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            if(mainPage == null)
            {
                return;
            }
            double TimeMs = mainPage.GetNowTimeMs();
            if (StartMs > TimeMs)
            {
                StartMs %= _BetweenMs;
                StartMs -= _BetweenMs;
            }
            TimeMs -= (TimeMs - StartMs) % _BetweenMs;
            mainPage.PlayBetween(TimeMs, TimeMs + _BetweenMs);
        }
    }
}
