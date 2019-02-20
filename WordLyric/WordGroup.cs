using System;
using System.Collections.Generic;
using System.Text;

namespace WordLyric
{
    public class WordGroup
    {
        public string Text;
        public float ActiveOffset;
        public float ActiveTime { get => Parent.ActiveTime + ActiveOffset; }
        public LyricLine Parent;

        public bool isActive(float sec)
        {
            return ActiveTime <= sec;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
