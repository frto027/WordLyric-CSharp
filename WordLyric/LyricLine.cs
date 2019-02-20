using System;
using System.Collections.Generic;
using System.Text;

namespace WordLyric
{
    public class LyricLine
    {
        /// <summary>
        /// 当更改这个属性时，会自动更新Parent的列表。因此FindLineNumber的返回值会改变
        /// </summary>
        public float ShowTime
        {
            get => _ShowTime;
            set {
                _ShowTime = value;
                if ((Parent?.FindLineNumber(this) ?? -1) >= 0)
                {
                    var temp = Parent;//remove之后Parent会变，需要暂时留住
                    temp.RemoveLyricLine(this);
                    temp.AddLyricLine(this);
                }
            }
        }
        private float _ShowTime;

        public string Style;
        public WordGroupCollection LyricGroup, TranslateGroup;
        public float ActiveTime { get => ShowTime + Parent.Offset; }

        public WordLyric Parent { get => _Parent; }
        internal WordLyric _Parent;

        public bool hasTranslate { get => TranslateGroup != null && TranslateGroup.Count > 0; }

        public LyricLine()
        {
            LyricGroup = new WordGroupCollection(this);
            TranslateGroup = new WordGroupCollection(this);
        }

        public override string ToString()
        {
            return ToString(false);
        }
        
        public string TranslateText { get => ToString(true); set
            {
                TranslateGroup = new WordGroupCollection(this);
                TranslateGroup.Add(new WordGroup()
                {
                    ActiveOffset = 0,
                    Text = value
                });
            } }
        public string Text
        {
            get => ToString(false); set
            {
                LyricGroup = new WordGroupCollection(this);
                LyricGroup.Add(new WordGroup()
                {
                    ActiveOffset = 0,
                    Text = value
                });
            }
        }

        public string ToString(bool translate)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var s in translate ? TranslateGroup : LyricGroup)
            {
                sb.Append(s.ToString());
            }
            return sb.ToString();
        }
    }
}
