using System;
using System.Collections.Generic;
using System.Text;

namespace WordLyric
{
    /// <summary>
    /// 歌词播放基类，绑定控件
    /// </summary>
    public abstract class LyricAdapterBase
    {
        public abstract void LoadLyric(WordLyric lyric);
        /// <summary>
        /// 调用这个函数触发相应的回调，更新信息
        /// </summary>
        /// <param name="time">当前歌曲的时间</param>
        /// <returns>如果按照正常播放进度，下次调用此函数后能产生效果的最早时间</returns>
        public abstract float SeekTo(float time);
    }
}
