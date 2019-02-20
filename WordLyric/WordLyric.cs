using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WordLyric
{
    /// <summary>
    /// WordLyric主类
    /// </summary>
    public class WordLyric :IEnumerable<LyricLine>
    {
        public string Title, Artist, Album, Author;
        public float Offset;

        private SortedList<float,LyricLine> lyricLines = new SortedList<float, LyricLine>();
        public string SplitMark, TranslateTextMark, TranslateTextSplitMark;
        public string StyleMark;
        public LyricLine this[int index]
        {
            get { return lyricLines.Values[index]; }
            set {
                lyricLines.RemoveAt(index);
                AddLyricLine(value);
            }
        }
        private readonly static Regex
            regComment = new Regex(@"(.*?)\[:\](.*)", RegexOptions.Compiled),
            regTimeTag = new Regex(@"\[(\d+):(\d+.?(\d*))\]", RegexOptions.Compiled),
            regIdTag = new Regex(@"\[(\D\w*):(.*?)\]", RegexOptions.Compiled),
            regClearTag = new Regex(@"\[.*:.*\]", RegexOptions.Compiled);

        internal static void DebugLog(string s)
        {
            //Console.WriteLine(s);
        }
        /// <summary>
        /// 从字节构建WordLyric对象，可根据头部的encoding标签识别编码
        /// </summary>
        /// <param name="bts">欲转换的字节数组</param>
        /// <returns>构建的WordLyric对象</returns>
        public static WordLyric FromBytes(byte[] bts)
        {
            Encoding encoding = Encoding.UTF8;
            byte[] enchead = Encoding.ASCII.GetBytes("[encoding:");
            int i = 0;
            for(; i < enchead.Length; i++)
            {
                if (bts[i] != enchead[i])
                    break;
            }
            if(i == enchead.Length)
            {
                byte[] splitbyte = { (byte)']',(byte)'\r',(byte)'\n' };
                int encnamelen = 0;
                while (i < bts.Length && Array.IndexOf(splitbyte,bts[i]) < 0)
                {
                    encnamelen++;
                    i++;
                }
                var mencoding = Encoding.ASCII.GetString(bts, enchead.Length, encnamelen);
                var enc = Encoding.GetEncoding(mencoding);
                if(enc != null)
                {
                    encoding = enc;
                }
                else
                {
                    DebugLog($"Can't find encoding of \"${mencoding}\"");
                }
            }
            return FromLrc(encoding.GetString(bts));
        }

        /// <summary>
        /// 从lrc字符串构建分字lrc对象
        /// </summary>
        /// <param name="lrctext">歌词文件内容</param>
        /// <returns>构建的分字lrc</returns>
        public static WordLyric FromLrc(string lrctext)
        {
            WordLyric wordLyric = new WordLyric();

            string translateText = null;
            MetaData translateMetadata = new MetaData();
            MetaData currentMetadata = new MetaData();
            string currentStyleName = "";
            //处理流程：HandleComment(HandleMetaData) -> HandleIdTag -> HandleTimeTag
            MetaData HandleMetaData(string metadataText,MetaData metadata)
            {
                metadata.FromMetaText(metadataText);
                return metadata;
            }
            string HandleComment(string comment)
            {
                var match = regComment.Match(comment);
                if (match.Success)
                {
                    string commentText = match.Result("$2");
                    if (!string.IsNullOrEmpty(wordLyric.SplitMark) && commentText.StartsWith(wordLyric.SplitMark))
                    {
                        currentMetadata =  HandleMetaData(commentText.Substring(wordLyric.SplitMark.Length), currentMetadata);
                    }
                    if(!string.IsNullOrEmpty(wordLyric.TranslateTextSplitMark) && commentText.StartsWith(wordLyric.TranslateTextSplitMark))
                    {
                        translateMetadata = HandleMetaData(commentText.Substring(wordLyric.TranslateTextSplitMark.Length), translateMetadata);
                    }
                    if(!string.IsNullOrEmpty(wordLyric.TranslateTextMark) && commentText.StartsWith(wordLyric.TranslateTextMark))
                    {
                        translateText = commentText.Substring(wordLyric.TranslateTextMark.Length);
                    }
                    if (!string.IsNullOrEmpty(wordLyric.StyleMark) && commentText.StartsWith(wordLyric.StyleMark))
                    {
                        currentStyleName = commentText.Substring(wordLyric.StyleMark.Length);
                    }

                    return match.Result("$1");
                }
                return comment;
            }
            string HandleIdTag(string text)
            {
                foreach(Match s in regIdTag.Matches(text))
                {
                    text = "";
                    var arg = s.Result("$2");
                    switch (s.Result("$1").Trim().ToLower())
                    {
                        case "ar":
                            wordLyric.Artist = arg;
                            break;
                        case "ti":
                            wordLyric.Title = arg;
                            break;
                        case "al":
                            wordLyric.Album = arg;
                            break;
                        case "by":
                            wordLyric.Author = arg;
                            break;
                        case "offset":
                            if(!float.TryParse(arg,out wordLyric.Offset))
                            {
                                wordLyric.Offset = 0;
                            }
                            break;
                        case "sm":
                            wordLyric.SplitMark = arg;
                            break;
                        case "ttm":
                            wordLyric.TranslateTextMark = arg;
                            break;
                        case "ttsm":
                            wordLyric.TranslateTextSplitMark = arg;
                            break;
                        case "st":
                            wordLyric.StyleMark = arg;
                            break;
                    }
                }
                return text;
            }

            void HandleTimeTag(string text)
            {
                var collect = regTimeTag.Matches(text);
                var clearText = regTimeTag.Replace(text, "");
                clearText = regClearTag.Replace(clearText, "");
                
                //foreach time,init a lyric line
                foreach (Match match in collect)
                {
                    LyricLine currentLine = new LyricLine();

                    int min = 0;
                    if (!int.TryParse(match.Result("$1"), out min))
                    {
                        DebugLog("failed to parse time tag[sec].");
                    }
                    float sec = 0;
                    if (!float.TryParse(match.Result("$2"), out sec))
                    {
                        DebugLog("failed to parse time tag[ms].");
                    }
                    sec += min * 60;
                    currentLine.ShowTime = sec;

                    //init style and other setthings
                    currentLine.Style = currentStyleName;
                    currentMetadata.ToWordGroupCollection(clearText, currentLine.LyricGroup);
                    if (translateText != null)
                    {
                        translateMetadata.ToWordGroupCollection(translateText, currentLine.TranslateGroup);
                    }

                    wordLyric.AddLyricLine(currentLine);

                    translateText = null;
                    translateMetadata = new MetaData();
                    currentMetadata = new MetaData();
                }
                
                
            }
           

            foreach (string textline in lrctext.Replace("\r\n", "\n").Replace("\r","\n").Split('\n'))
            {
                try
                {
                    var step1 = HandleComment(textline);
                    var step2 = HandleIdTag(step1);
                    HandleTimeTag(step2);
                }
                catch (Exception) { }
            }
            return wordLyric;
        }
        /// <summary>
        /// 在歌词中增加一行
        /// </summary>
        /// <param name="line">新的歌词行</param>
        public void AddLyricLine(LyricLine line)
        {
            line._Parent = this;
            lyricLines.Add(line.ShowTime, line);
        }
        public void RemoveLyricLine(LyricLine line)
        {
            line._Parent = null;
            int rm;
            while ((rm = lyricLines.IndexOfValue(line)) >= 0)
                lyricLines.RemoveAt(rm);
        }
        public void RemoveLyricLine(int line)
        {
            lyricLines.RemoveAt(line);
        }
        public int FindLineNumber(LyricLine line)
        {
            return lyricLines.IndexOfValue(line);
        }
        public int Length { get => lyricLines.Count; }

        /// <summary>
        /// 将歌词编码为人类可读格式
        /// </summary>
        /// <param name="LrcOnly">为true则只编码标准的LRC内容，不含分字元素，为false则完整导出</param>
        /// <returns>人类可读的歌词格式</returns>
        public string ToLyric(bool LrcOnly = false)
        {
            StringBuilder sb = new StringBuilder();

            void AddTag(string str,string tag)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    sb.AppendLine($"[{tag}:{str}]");
                }
            }
            AddTag(Title, "ti");
            AddTag(Artist, "ar");
            AddTag(Album, "al");
            AddTag(Author, "by");
            if (Offset != 0)
                sb.AppendLine($"[offset:{Offset.ToString("F3")}]");
            if (!LrcOnly)
            {
                AddTag(SplitMark, "sm");
                AddTag(TranslateTextMark, "ttm");
                AddTag(TranslateTextSplitMark, "ttsm");
                AddTag(StyleMark, "st");
            }

            //LinkedList<LyricTag> lrcs = new LinkedList<LyricTag>();
            string currentStyle = null;
            foreach(var line in lyricLines)
            {
                if (!string.Equals(currentStyle, line.Value.Style))
                {
                    currentStyle = line.Value.Style;
                    new LyricStyleTag() { StyleMark = StyleMark, StyleName = currentStyle, LrcOnly = LrcOnly }.AppendTo(sb);
                }
                new LyricLineTag() { line = line.Value, LrcOnly = LrcOnly }.AppendTo(sb);
            }
            return sb.ToString();
        }
        /// <summary>
        /// 按UTF-8编码歌词
        /// </summary>
        /// <returns>UTF-8编码的歌词</returns>
        public byte[] ToBytes()
        {
            return ToBytes(Encoding.UTF8);
        }

        /// <summary>
        /// 添加编码信息，并转换成字节数组以便再次使用
        /// </summary>
        /// <param name="encoding">存盘使用的编码</param>
        /// <returns>转换后的字节数组</returns>
        public byte[] ToBytes(Encoding encoding)
        {
            string lrc = ToLyric();
            string ret = $"[encoding:{encoding.WebName}]" + Environment.NewLine + lrc;
            return encoding.GetBytes(ret);
        }
        
        public IEnumerator<LyricLine> GetEnumerator()
        {
            return lyricLines.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return lyricLines.Values.GetEnumerator();
        }
    }

    
}
