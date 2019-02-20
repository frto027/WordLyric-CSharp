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
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace WordLyricGUIEditor
{

    public sealed partial class LyricShowControl : UserControl
    {
        List<TextBlock> linePanels;
        public LyricShowControl()
        {
            this.InitializeComponent();
        }
        public void LoadLyric(WordLyric.WordLyric lyriclines,bool isTranslate)
        {
            cvs.Children.Clear();
            if (lyriclines == null)
            {
                linePanels = null;
            }
            else
            {
                linePanels = new List<TextBlock>(lyriclines.Length);
                foreach (var line in lyriclines)
                {
                    linePanels.Add(GenPanel(line, isTranslate));
                }
            }
        }
        
        //输入是歌词的某一行，输出是一个代表这行的控件
        private TextBlock GenPanel(WordLyric.LyricLine line,bool isTranslate)
        {
            TextBlock tb = new TextBlock() {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords
            };
            foreach(WordLyric.WordGroup gp in (isTranslate ? line.TranslateGroup : line.LyricGroup))
            {
                tb.Inlines.Add(GenGroupText(gp.Text));
            }
            return tb; 
        }

        public void SetLyricLine(int linecode,bool[] active)
        {
            if (cvs.Children.Count == 0)
                cvs.Children.Add(linePanels[linecode]);
            else
                cvs.Children[0] = linePanels[linecode];
            for(int i = 0; i < linePanels[linecode].Inlines.Count; i++)
            {
                if(active?[i] ?? false)
                {
                    ActiveGroup(linePanels[linecode].Inlines[i],false);
                }
                else
                {
                    DisableGroup(linePanels[linecode].Inlines[i]);
                }
            }
        }
        public void ActiveGroup(int linecode,int groupid)
        {
            ActiveGroup(linePanels[linecode].Inlines[groupid], true);
        }

        private Inline GenGroupText(string str)
        {
            Inline ret = new Run()
            {
                Text = str
            };
            return ret;
        }
        private void DisableGroup(Inline view)
        {
            (view as Run).TextDecorations = Windows.UI.Text.TextDecorations.None;
        }
        private void ActiveGroup(Inline view,bool animation)
        {
            (view as Run).TextDecorations = Windows.UI.Text.TextDecorations.Underline;
        }
    }
    /* 已抛弃 */
    //public sealed partial class LyricShowControl : UserControl
    //{
    //    List<StackPanel> linePanels;
    //    List<List<UIElement>> linePanelsCtrlList;
    //    public LyricShowControl()
    //    {
    //        this.InitializeComponent();
    //    }
    //    public void LoadLyric(WordLyric.WordLyric lyriclines, bool isTranslate)
    //    {
    //        cvs.Children.Clear();
    //        if (lyriclines == null)
    //        {
    //            linePanels = null;
    //            linePanelsCtrlList = null;
    //        }
    //        else
    //        {
    //            linePanelsCtrlList = new List<List<UIElement>>();

    //            linePanels = new List<StackPanel>(lyriclines.Length);
    //            foreach (var line in lyriclines)
    //            {
    //                linePanels.Add(GenPanel(line, isTranslate));
    //            }
    //        }
    //    }

    //    //输入是歌词的某一行，输出是一个代表这行的控件
    //    private StackPanel GenPanel(WordLyric.LyricLine line, bool isTranslate)
    //    {
    //        //这里直接把对应的UIElement记下来
    //        List<UIElement> elems = new List<UIElement>();

    //        StackPanel panel = new StackPanel() { Orientation = Orientation.Vertical };
    //        double curwidth = 0;
    //        StackPanel curLine = null;
    //        foreach (WordLyric.WordGroup gp in (isTranslate ? line.TranslateGroup : line.LyricGroup))
    //        {
    //            MainPage.LogPrint($"Gen lyric group text = {gp.Text}");
    //            UIElement element = GenGroupText(gp.Text);
    //            if (curLine == null || curwidth + element.DesiredSize.Width > Width)
    //            {
    //                curwidth = 0;
    //                if (curLine != null)
    //                    panel.Children.Add(curLine);
    //                curLine = new StackPanel() { Orientation = Orientation.Horizontal };
    //            }
    //            curwidth += element.DesiredSize.Width;
    //            curLine.Children.Add(element);
    //            elems.Add(element);
    //        }
    //        if (curLine != null)
    //            panel.Children.Add(curLine);

    //        linePanelsCtrlList.Add(elems);

    //        return panel;
    //    }

    //    public void SetLyricLine(int linecode, bool[] active)
    //    {
    //        if (cvs.Children.Count == 0)
    //            cvs.Children.Add(linePanels[linecode]);
    //        else
    //            cvs.Children[0] = linePanels[linecode];
    //        for (int i = 0; i < linePanelsCtrlList[linecode].Count; i++)
    //        {
    //            if (active?[i] ?? false)
    //            {
    //                ActiveGroup(linePanelsCtrlList[linecode][i], false);
    //            }
    //            else
    //            {
    //                DisableGroup(linePanelsCtrlList[linecode][i]);
    //            }
    //        }
    //    }
    //    public void ActiveGroup(int linecode, int groupid)
    //    {
    //        ActiveGroup(linePanelsCtrlList[linecode][groupid], true);
    //    }

    //    private UIElement GenGroupText(string str)
    //    {
    //        var tb = new TextBlock() { Opacity = 0f };

    //        tb.Text = str;
    //        tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
    //        tb.Width = tb.DesiredSize.Width;
    //        return tb;
    //    }
    //    private void DisableGroup(UIElement view)
    //    {
    //        view.Opacity = 0.2f;
    //    }
    //    private void ActiveGroup(UIElement view, bool animation)
    //    {
    //        view.Opacity = 1f;
    //    }
    //}
}
