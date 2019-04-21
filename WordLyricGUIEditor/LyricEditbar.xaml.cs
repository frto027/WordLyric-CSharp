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
    public sealed partial class LyricEditbar : UserControl
    {
        LinkedList<AreaBase> areaList = new LinkedList<AreaBase>();
        System.Threading.ReaderWriterLockSlim areaListLock = new System.Threading.ReaderWriterLockSlim();


        private float pixMulti = 2f;
        private Dictionary<Layers, float> pixHeights = new Dictionary<Layers, float>();//每个Layer的高度
        private Dictionary<Layers, float> actualPixHeight;//实际上每个Layer相对于当前行的起始y坐标
        private float allPixHeight;//所有层的总高度
        private Layers currentLayer = Layers.WAVE;


        public float VerticalOffset = 0f;
        public float VerticalHeight { get; private set; }

        private float VerticalBarMax { get => VerticalHeight - 20; }
        public enum Layers { WAVE,  CUSOUR,TEXT,EMPTY };

        //鼠标按下事件触发
        public delegate void MouseClickProgressBarEvent(Layers layer, float timems);
        public event MouseClickProgressBarEvent OnMouseClickProgressBar;
        //歌曲滚动相关
        //public float ScrollRate { set; private get; } = 0;//滚动速率，相对于歌曲播放
        public bool LockScrollRate { set; private get; } = false;
        public float LockScrollOffset { set; private get; } = 40;

        /// <summary>
        /// 歌曲播放当前进度，显示在wave条中
        /// </summary>
        public Func<double> GetMusicPlayTimems;

        public LyricEditbar()
        {
            this.InitializeComponent();

            //layer heights;
            pixHeights.Add(Layers.CUSOUR, 10f);
            pixHeights.Add(Layers.WAVE, 20f);
            pixHeights.Add(Layers.TEXT, 30f);
            pixHeights.Add(Layers.EMPTY, 1f);

            InitPixHeightMap();

            //滚动条相关设置
            FlushProgressBar();
            bar.ValueChanged += (_, __) => {
                if (VerticalBarMax >= bar.Value)
                    VerticalOffset = -(float)bar.Value;
            };
        }

        public void SetLayerHeight(Layers layer,float height)
        {
            if (pixHeights.ContainsKey(layer))
                pixHeights[layer] = height;
            else
                pixHeights.Add(layer, height);
            InitPixHeightMap();
        }

        private void InitPixHeightMap()
        {
            actualPixHeight = new Dictionary<Layers, float>();
            float tail = 0;
            foreach(Layers layer in Enum.GetValues(typeof(Layers)))
            {
                actualPixHeight.Add(layer, tail);
                if (pixHeights.TryGetValue(layer,out float val))
                {
                    tail += val;
                }
            }

            allPixHeight = tail;
        }

        private class AreaBase
        {
            public float timewide;
            public bool isBeginBlock = true;

            public enum AreaType { NoLyric,Lyric_Word,Lyric_Word_End };
            public AreaType areaType = AreaType.NoLyric;

            public string TypeString { get { return areaType.ToString(); } }
        }

        public void InitAreaListDefault(float timewide)
        {
            areaListLock.EnterWriteLock();
      
                areaList.Clear();
                areaList.AddLast(new AreaBase() { areaType = AreaBase.AreaType.NoLyric, timewide = timewide });

            areaListLock.ExitWriteLock();

        }
        void FlushProgressBar()
        {
            bar.Maximum = VerticalBarMax;
            bar.Value = -VerticalOffset;
        }

        private void CanvasAnimatedControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            float MaxWidth = (float)sender.Size.Width;
            /*
            {//处理自动滚动
                float ScrollRate = this.ScrollRate;
                if (ScrollRate > 0)
                {
                    VerticalOffset -= ScrollRate * allPixHeight * (float)sender.TargetElapsedTime.TotalMilliseconds / (MaxWidth / pixMulti);
                }
            }
            */
            float MaxHeight = (float)sender.Size.Height;

            float heightTail = VerticalOffset;
            float initHeightTail = heightTail;
            float widthTail = 0;

            var session = args.DrawingSession;

            float blockBeginTime = 0, blockEndTime = 0;//一个块的起始和终止时间(单位ms)

            Rect rect,allrect;//rect 当前块的内部块矩形 allrect 当前整个块矩形

            bool isMouseIn;
            bool pointClick,pointDown;
            Point mousePoint;
            
            mousePointLock.EnterReadLock();
            if(isMouseIn = this.isMouseIn)
            {
                mousePoint = this.mousePoint;
            }
            if (this.pointClick > 0)
            {
                pointClick = true;
                this.pointClick--;
            }
            else
                pointClick = false;
            pointDown = this.pointDown;
            mousePointLock.ExitReadLock();

            void NextLine()
            {
                widthTail = 0;
                heightTail += allPixHeight;
            }
            //下一个块
            //返回值是false的时候不要绘制这个块，直接开始下一个
            bool NextRect(float width)
            {
                blockBeginTime = blockEndTime;
                blockEndTime += width;
               
                width *= pixMulti;
                if(widthTail + width > MaxWidth)
                {
                    NextLine();
                }

                if (heightTail > MaxHeight || heightTail + allPixHeight < 0)
                {
                    if (LockScrollRate)
                    {
                        //当指针所在行是当前行时，需要绘制光标，以触发坐标调整
                        float curTimems = (float)(GetMusicPlayTimems?.Invoke() ?? 0);
                        if (curTimems >= blockBeginTime && curTimems <= blockEndTime)
                            return true;
                    }
                    return false;
                }
                    


                currentLayer = (Layers)Enum.GetValues(typeof(Layers)).GetValue(0);
                pixHeights.TryGetValue(currentLayer, out float lheight);
                rect = new Rect(widthTail, heightTail, width, lheight);
                allrect = new Rect(widthTail, heightTail, width, allPixHeight);
                widthTail += width;

                session.DrawRectangle(allrect, Colors.Black);
                return true;
            }

            //同块下一行
            void ToLayer(Layers layer)
            {
                if (currentLayer != layer)
                {
                    rect.Y -= actualPixHeight[currentLayer];
                    rect.Y += actualPixHeight[layer];
                    pixHeights.TryGetValue(layer, out float lheight);
                    rect.Height = lheight;
                    currentLayer = layer;
                }
            }
            bool isMouseInRect()
            {
                return isMouseIn && rect.Contains(mousePoint);
            }

            float getMouseTimeMs()
            {
                return (float)(blockBeginTime + (mousePoint.X - rect.X) / pixMulti);
            }
            float getRectXByTimeMs(float timems)
            {
                return (timems - blockBeginTime) * pixMulti + (float)rect.X;
            }
            //画一个鼠标光标到当前块上面，在Layer函数中调用，同时捕获鼠标点击事件
            void DrawMousePoint()
            {
                if (isMouseInRect())
                {
                    float mousex = (float)mousePoint.X;
                    session.DrawLine(mousex, (float)rect.Y, mousex, (float)(rect.Y + rect.Height), pointDown ? Colors.Red : Colors.Blue, 1);
                    if (pointClick)
                    {
                        OnMouseClickProgressBar?.Invoke(currentLayer, getMouseTimeMs());
                    }
                }
            }
            //绘制指针所在行
            void DrawCusorLayer()
            {
                ToLayer(Layers.CUSOUR);
                session.FillRectangle(rect, Colors.LightGreen);
                DrawMousePoint();
                //绘制播放光标指示器
                float curTimems = (float)(GetMusicPlayTimems?.Invoke() ?? 0);
                if(curTimems >= blockBeginTime && curTimems <= blockEndTime)
                {
                    float x = getRectXByTimeMs(curTimems);
                    session.DrawLine(x, (float)rect.Y, x, (float)(rect.Y + rect.Height), Colors.DarkGreen, 1);

                    //锁定光标坐标，更新滚动
                    if(LockScrollRate)
                    {
                        float curHeight = heightTail - initHeightTail;
                        curHeight += allPixHeight * (x / MaxWidth);
                        VerticalOffset = LockScrollOffset - curHeight;
                    }
                }
            }
            //绘制Wave块
            void DrawWaveLayer()
            {
                ToLayer(Layers.WAVE);
                session.FillRectangle(rect, Colors.Gray);
                DrawMousePoint();
            }

            //-------------填充编辑条-----------------

            //TODO

            /*
            if (NextRect(100))
            {
                DrawWaveLayer();
                ToLayer(Layers.TEXT);
                session.FillRectangle(rect, Colors.Purple);
                DrawCusorLayer();
            }



            if (NextRect(400))
            {
                DrawWaveLayer();
                ToLayer(Layers.TEXT);
                session.FillRectangle(rect, Colors.Purple);
                DrawCusorLayer();
            }


            if (NextRect(180))
            {
                DrawCusorLayer();
                DrawWaveLayer();
                ToLayer(Layers.TEXT);
                session.DrawText("TODO", (float)rect.X, (float)rect.Y, Colors.Orange);
                DrawCusorLayer();
                //session.DrawText("Todo", 0, 0, Colors.Black);
            }
            */

            //绘制列表中的控制条
            float remainwidth;
            areaListLock.EnterReadLock();
            foreach(AreaBase area in areaList)
            {
                remainwidth = area.timewide;
                bool enddraw = false;
                int cotinue = 0;
                while(!enddraw)
                {
                    bool willdraw;

                    enddraw = remainwidth * pixMulti + widthTail <= MaxWidth;
                    if (!enddraw)
                    {
                        float drawwidth = (MaxWidth - widthTail) / pixMulti;
                        willdraw = NextRect(drawwidth);
                        remainwidth -= drawwidth;
                    }
                    else
                    {
                        willdraw = NextRect(remainwidth);
                    }
                    if (willdraw)
                    {
                        //实际绘制一块
                        //TODO
                        DrawWaveLayer();
                        ToLayer(Layers.TEXT);
                        string prestr = String.Empty;
                        if (cotinue > 0)
                            prestr = $"c{cotinue}-";
                        session.DrawText(prestr + area.TypeString, (float)rect.X, (float)rect.Y, Colors.Red);
                        DrawCusorLayer();
                    }
                    if (!enddraw)
                        NextLine();
                    cotinue++;
                }
            }
            areaListLock.ExitReadLock();

            NextLine();
            VerticalHeight = heightTail - initHeightTail;
        }

        Point mousePoint;
        bool isMouseIn = false;
        int pointClick = 0;
        bool pointDown = false;
        System.Threading.ReaderWriterLockSlim mousePointLock = new System.Threading.ReaderWriterLockSlim();
        private void Cvs_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            mousePointLock.EnterWriteLock();
            isMouseIn = true;
            mousePoint = e.GetCurrentPoint(cvs).Position;
            mousePointLock.ExitWriteLock();
        }

        private void Cvs_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            mousePointLock.EnterWriteLock();
            isMouseIn = false;
            mousePointLock.ExitWriteLock();
        }

        private void Cvs_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            mousePointLock.EnterWriteLock();
            pointDown = true;
            mousePointLock.ExitWriteLock();
        }

        private void Cvs_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            mousePointLock.EnterWriteLock();
            pointDown = false;
            pointClick++;
            mousePointLock.ExitWriteLock();
        }

        private void Cvs_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (LockScrollRate)
            {
                LockScrollOffset += e.GetCurrentPoint(null).Properties.MouseWheelDelta * 0.1f;
                return;
            }
                
            VerticalOffset += e.GetCurrentPoint(null).Properties.MouseWheelDelta * 0.8f;
            if (-VerticalOffset < 0)
                VerticalOffset = 0;
            if (-VerticalOffset > VerticalBarMax)
                VerticalOffset = -(VerticalBarMax);


            FlushProgressBar();
        }
    }

    
}
