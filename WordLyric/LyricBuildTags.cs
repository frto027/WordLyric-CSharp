using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WordLyric
{
    internal interface LyricTag {
        void AppendTo(StringBuilder sb);
    }
    internal class LyricStyleTag : LyricTag
    {
        public string StyleName;
        public string StyleMark;
        public bool LrcOnly = false;//只导出标准歌词

        public void AppendTo(StringBuilder sb)
        {
            if (LrcOnly)
                return;
            sb.AppendLine($"[:]{StyleMark}{StyleName}");
        }
    }
    internal class LyricLineTag : LyricTag
    {
        public LyricLine line;
        public bool LrcOnly = false;//只导出标准歌词

        public void AppendTo(StringBuilder sb)
        {
            if (line.LyricGroup.Count == 0)
                return;

            if (!LrcOnly && line.hasTranslate)
            {
                if (line.TranslateGroup.Count > 1 || (line.TranslateGroup.Count == 1 && line.TranslateGroup[0].ActiveOffset != 0))
                {
                    sb.Append($"[:]{line.Parent.TranslateTextSplitMark}");
                    AppendMetadata(line.TranslateGroup, sb);
                    sb.AppendLine();
                }
                sb.Append($"[:]{line.Parent.TranslateTextMark}");
                AppendText(line.TranslateGroup, sb);
                sb.AppendLine();
            }
            if (!LrcOnly && (line.LyricGroup.Count > 1 || (line.LyricGroup.Count == 1 && line.LyricGroup[0].ActiveOffset != 0)))
            {
                sb.Append($"[:]{line.Parent.SplitMark}");
                AppendMetadata(line.LyricGroup, sb);
                sb.AppendLine();
            }

            int min = ((int)line.ShowTime) / 60;
            float sec = line.ShowTime - (min * 60);

            string secstr = sec.ToString("N3").TrimEnd('0');
            if (secstr.EndsWith("."))
                secstr = secstr + "0";//补一个0，规范一些
            if (sec < 10)
                secstr = "0" + secstr;

            sb.Append($"[{(min < 10 ? "0" : "")}{min}:{secstr}]");
            AppendText(line.LyricGroup, sb);
            sb.AppendLine();
        }

        private void AppendMetadata(WordGroupCollection collect,StringBuilder sb)
        {
            bool first = true;
            foreach(WordGroup group in collect)
            {
                int wordcount = StringInfo.ParseCombiningCharacters(group.Text).Length;
                if (first)
                    first = false;
                else
                    sb.Append("|");
                sb.Append(group.ActiveOffset);
                if(wordcount != 1)
                {
                    sb.Append(",");
                    sb.Append(wordcount);
                }
            }
        }
        private void AppendText(WordGroupCollection collect,StringBuilder sb)
        {
            foreach (WordGroup wg in collect)
                sb.Append(wg.Text);
        }
    }
}
