using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
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

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace WordLyricGUIEditor
{
    public sealed partial class RythmSynchornizer : UserControl
    {
        public MainPage mainPage;
        private LinkedList<RythmSyncornizerBar> bars = new LinkedList<RythmSyncornizerBar>();
        private double startms = 0, loopms = 0;

        private static Color[] LoopBlockColors = new Color[5] { Colors.LightBlue, Colors.LightGreen, Colors.LightGray, Colors.LightYellow, Colors.LightPink };

        public struct TickPoint
        {
            public int id;
            public double timems;
        };

        private int nextLoopId = 0;

        public List<TickPoint> tickms = new List<TickPoint>();

        DispatcherTimer updateTimeTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(50)};

        public RythmSynchornizer()
        {
            this.InitializeComponent();

            updateTimeTimer.Tick += (s, e) => UpdateNextLoopId();
            updateTimeTimer.Start();
        }

        private void TimeAlignButtonClick(object sender, RoutedEventArgs e)
        {
            TimeAlignTextBox.Text = mainPage.GetNowTimeMs().ToString();
        }

        private void TimeAlignTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double ms;
            if (!double.TryParse(TimeAlignTextBox.Text, out ms))
                ms = 0;
            foreach(RythmSyncornizerBar bar in BarPanel.Children)
            {
                bar.StartMs = ms;
            }
        }
        private void SyncLoopTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double ms;
            if (!double.TryParse(SyncLoopTimeTextBox.Text, out ms))
                ms = 0;
            foreach (RythmSyncornizerBar bar in BarPanel.Children)
            {
                bar.BetweenMs = ms;
            }
        }

        private void NewLoopButtonClick(object sender, RoutedEventArgs e)
        {
            tickms.Clear();
            UpdateNextLoopId();
        }
        private void SyncLoopButtonClick(object sender, RoutedEventArgs e)
        {
            //if(mainPage.player != null)
            {
                tickms.Add(new TickPoint()
                {
                    id = nextLoopId,
                    timems = mainPage.GetNowTimeMs()
                });
            }
            if(tickms.Count > 1)
            {
                CalculateMsTime();
            }
            UpdateNextLoopId();
        }

        private void CalculateMsTime()
        {
            //最小二乘法 x或者t : 节拍数(1,2,3,4,...) y:节拍x的起始时间
            double xiyi = 0,x_multi_plus = 0;
            double x_avg = 0, y_avg = 0;
            foreach(var item in tickms)
            {
                xiyi += item.id * item.timems;
                x_avg += item.id;
                y_avg += item.timems;
                x_multi_plus += item.id * item.id;
            }
            x_avg /= tickms.Count;
            y_avg /= tickms.Count;
            double k = (xiyi - tickms.Count * x_avg * y_avg) / (x_multi_plus - tickms.Count * x_avg * x_avg);
            double b = y_avg - k * x_avg;
            startms = b;
            loopms = k;

            TimeAlignTextBox.Text = startms.ToString();
            SyncLoopTimeTextBox.Text = loopms.ToString();
        }
 
        private void NewBarMultiButtonClick(object sender, RoutedEventArgs e)
        {
            RythmSyncornizerBar bar = new RythmSyncornizerBar();
            try
            {
                bar.Factor = double.Parse(NewBarFactorTextBox.Text);
            }
            catch (FormatException)
            {
                return;
            }
            
            if (!string.IsNullOrWhiteSpace(TimeAlignTextBox.Text))
                bar.StartMs = double.Parse(TimeAlignTextBox.Text);
            if (!string.IsNullOrWhiteSpace(SyncLoopTimeTextBox.Text))
                bar.BetweenMs = double.Parse(SyncLoopTimeTextBox.Text);

            bar.mainPage = mainPage;
            bar.OnRemoveMe += (b) =>
            {
                bars.Remove(b);
                BarPanel.Children.Remove(b);
            };

            BarPanel.Children.Add(bar);
            bars.AddLast(bar);
        }

        private void SetNextLoopId(int id)
        {
            nextLoopId = id;
            //NextLoopIdTextBlock.Text = id.ToString();
        }

        private void DrawSessionRectangle(CanvasDrawingSession session,float BlockWidth,float Height,float pos,int ii,bool Light = false)
        {
            int index = (ii + 5) % 5;
            if (index < 0)
                index += 5;

            session.FillRectangle((pos+  .5f) * BlockWidth, 0f, BlockWidth, Height * .5f, LoopBlockColors[index]);
            if (Light)
            {
                session.FillRectangle((pos) * BlockWidth, Height * .5f, BlockWidth, Height * .5f, Colors.LightSeaGreen);
            }
            session.DrawText(" " + ii.ToString(), (pos + .5f) * BlockWidth, 0, Light ? Colors.Red : Colors.Gray);
            //session.DrawLine((pos + .5f) * BlockWidth, 0f, (pos + .5f) * BlockWidth, Height * 0.6f, Colors.Black);
        }

        private void NextLoopIdCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            var session = args.DrawingSession;
            double time = mainPage.GetNowTimeMs();
            float width = (float)sender.Size.Width;
            float height = (float)sender.Size.Height;

            session.DrawRectangle(0, 0, width, height, Colors.Black);

            width /= 3;
            if(tickms.Count > 1)
            {
                float nowtime = (float)mainPage.GetNowTimeMs();
                //nowtime -= (loopms * 0.5);
                nowtime -= (float)startms;

                float pos = nowtime / (float)loopms;

                int ii = (int)Math.Round(pos);
                SetNextLoopId(ii);
                pos = 1f - (pos - ii);

                DrawSessionRectangle(session, width, height, pos - 2, ii - 2);
                DrawSessionRectangle(session, width, height, pos - 1, ii - 1);
                DrawSessionRectangle(session, width, height, pos, ii,true);
                DrawSessionRectangle(session, width, height, pos + 1, ii + 1);
                DrawSessionRectangle(session, width, height, pos + 2, ii + 2);
                /*
                session.FillRectangle(pos * width, 0f, width, height, LoopBlockColors[ii % 5]);
                session.DrawText(ii.ToString(), pos * width, height / 3, Colors.Red);

                session.FillRectangle((pos - 1) * width, 0f, width, height, LoopBlockColors[(ii - 1 + 5) % 5]);
                session.DrawText((ii-1).ToString(), (pos - 1) * width, height / 3, Colors.Gray);
                session.FillRectangle((pos + 1) * width, 0f, width, height, LoopBlockColors[(ii + 1) % 5]);
                session.DrawText((ii + 1).ToString(), (pos + 1) * width, height / 3, Colors.Gray);
                session.FillRectangle((pos - 2) * width, 0f, width, height, LoopBlockColors[(ii - 2 + 5) % 5]);
                session.DrawText((ii - 2).ToString(), (pos - 2) * width, height / 3, Colors.Gray);
                session.FillRectangle((pos + 2) * width, 0f, width, height, LoopBlockColors[(ii + 2) % 5]);
                session.DrawText((ii + 2).ToString(), (pos + 2) * width, height / 3, Colors.Gray);
                */
            }

            session.DrawLine((float)sender.Size.Width / 2, 0, (float)sender.Size.Width / 2, height, Colors.Green);
        }

        private void UpdateNextLoopId()
        {
            if(tickms.Count == 0)
            {
                SetNextLoopId(0);
            }else if(tickms.Count == 1)
            {
                SetNextLoopId(1);
            }
            else
            {
                double nowtime = mainPage.GetNowTimeMs();
                //nowtime -= (loopms * 0.5);
                nowtime -= startms;

                int ii = (int)Math.Round(nowtime/loopms);
                if(ii > 0)
                    SetNextLoopId(ii);
            }
        }
    }
}
