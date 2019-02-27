using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        public struct TickPoint
        {
            public int id;
            public double timems;
        };

        public List<TickPoint> tickms = new List<TickPoint>();

        public RythmSynchornizer()
        {
            this.InitializeComponent();
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
        }
        private void SyncLoopButtonClick(object sender, RoutedEventArgs e)
        {
            //if(mainPage.player != null)
            {
                tickms.Add(new TickPoint()
                {
                    id = tickms.Count,
                    timems = mainPage.GetNowTimeMs()
                });
            }
            if(tickms.Count > 1)
            {
                CalculateMsTime();
            }
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
            double startms = b;
            double loopms = k;

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
    }
}
