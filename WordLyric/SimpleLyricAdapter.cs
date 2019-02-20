using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLyric
{
    /// <summary>
    /// 实现基本歌词解析、播放的操作，即一般LRC的解析，这个解析是忽略激活组的
    /// </summary>
    public class SimpleLyricAdapter : LyricAdapterBase
    {
        public class LyricLineBundle
        {
            public int LineNumber;
            public string LineText;
        }
        /// <summary>
        /// 需要实现建立歌词列表的操作
        /// </summary>
        /// <param name="bundles">所有行的信息，已排序，不可修改</param>
        public delegate void AddLyricLine(IList<LyricLineBundle> bundles);
        /// <summary>
        /// 撤销激活某一行
        /// </summary>
        public delegate void UnActiveLine(LyricLineBundle bundle);
        /// <summary>
        /// 激活某一行
        /// </summary>
        public delegate void ActiveLine(LyricLineBundle bundle);


        /// <summary>
        /// 当调用LoadLyricLines时会被触发
        /// </summary>
        public event AddLyricLine OnAddLyricLine;
        public event UnActiveLine OnUnActiveLine;
        public event ActiveLine OnActiveLine;

        private int currentLine = -1;

        private SortedList<float, LyricLineBundle> lines = new SortedList<float, LyricLineBundle>();
        public override void LoadLyric(WordLyric lyric)
        {
            currentLine = -1;
            lines.Clear();
            foreach (var x in lyric)
                lines.Add(x.ActiveTime, new LyricLineBundle { LineText = x.Text });

            for(int i = 0; i < lines.Values.Count; i++)
            {
                lines.Values[i].LineNumber = i;
            }
            OnAddLyricLine?.Invoke(lines.Values);
        }

        public override float SeekTo(float time)
        {
            int newline = currentLine;
            while (newline >= 0 && lines.Keys[newline] > time)
                newline--;
            while (newline + 1 < lines.Count && lines.Keys[newline + 1] <= time)
                newline++;
            if (newline != currentLine)
            {
                if (currentLine != -1)
                    OnUnActiveLine?.Invoke(lines.Values[currentLine]);
                if (newline != -1)
                    OnActiveLine?.Invoke(lines.Values[newline]);
                currentLine = newline;
            }
            if (newline + 1 >= lines.Count)
                return float.PositiveInfinity;
            else
                return lines.Keys[newline + 1];
        }
    }
}
