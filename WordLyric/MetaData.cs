using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WordLyric
{
    internal class MetaData
    {
        struct MetaPair
        {
            public float activeTime;
            public int count;
        }
        LinkedList<MetaPair> metalist = new LinkedList<MetaPair>();
        public void FromMetaText(string metatext)
        {
            MetaPair metaByString(string str)
            {
                var arr = str.Split(',');
                MetaPair ret;
                if (!float.TryParse(arr[0], out ret.activeTime))
                {
                    WordLyric.DebugLog("unable to parse metadata[ative time].");
                }
                if (arr.Length > 1)
                {
                    if (!int.TryParse(arr[1], out ret.count))
                    {
                        WordLyric.DebugLog("unable to parse metadata[count]");
                    }
                }
                else
                {
                    ret.count = 1;/* default value */
                }
                return ret;
            }
            foreach (string str in metatext.Split('|'))
            {
                metalist.AddLast(metaByString(str));
            }

        }
        //需要使用StringInfo来编码
        public void ToWordGroupCollection(string cleartext, WordGroupCollection collection)
        {
            var strEnum = StringInfo.GetTextElementEnumerator(cleartext);

            StringBuilder sb = new StringBuilder();
            foreach (MetaPair pair in metalist)
            {
                //get string
                for (int i = 0; i < pair.count && strEnum.MoveNext(); i++)
                {
                    sb.Append(strEnum.GetTextElement());
                }
                if (sb.Length == 0)
                    break;
                WordGroup group = new WordGroup();
                group.ActiveOffset = pair.activeTime;
                group.Text = sb.ToString();
                collection.Add(group);

                sb.Clear();
            }
            while (strEnum.MoveNext())
            {
                sb.Append(strEnum.GetTextElement());
            }
            if (sb.Length > 0)
            {
                WordGroup group = new WordGroup();
                group.ActiveOffset = 0;
                group.Text = sb.ToString();
                collection.Add(group);
            }
        }
    }
}
