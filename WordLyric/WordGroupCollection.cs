using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WordLyric
{
    public class WordGroupCollection :IEnumerable<WordGroup>,IList<WordGroup>
    {
        private LyricLine parent;
        private List<WordGroup> groupList = new List<WordGroup>();

        public WordGroupCollection(LyricLine wordGroup)
        {
            parent = wordGroup;
        }

        public WordGroup this[int index] { get => ((IList<WordGroup>)groupList)[index];
            set
            {
                groupList[index].Parent = null;
                value.Parent = parent;
                ((IList<WordGroup>)groupList)[index] = value;
            }
        }

        public int Count => ((IList<WordGroup>)groupList).Count;

        public bool IsReadOnly => ((IList<WordGroup>)groupList).IsReadOnly;

        public void Add(WordGroup item)
        {
            item.Parent = parent;
            ((IList<WordGroup>)groupList).Add(item);
        }

        public void Clear()
        {
            foreach (var x in groupList)
                x.Parent = null;
            ((IList<WordGroup>)groupList).Clear();
        }

        public bool Contains(WordGroup item)
        {
            return ((IList<WordGroup>)groupList).Contains(item);
        }

        public void CopyTo(WordGroup[] array, int arrayIndex)
        {
            ((IList<WordGroup>)groupList).CopyTo(array, arrayIndex);
        }

        public IEnumerator<WordGroup> GetEnumerator()
        {
            return ((IEnumerable<WordGroup>)groupList).GetEnumerator();
        }

        public int IndexOf(WordGroup item)
        {
            return ((IList<WordGroup>)groupList).IndexOf(item);
        }

        public void Insert(int index, WordGroup item)
        {
            item.Parent = parent;
            ((IList<WordGroup>)groupList).Insert(index, item);
        }

        public bool Remove(WordGroup item)
        {
            item.Parent = null;
            return ((IList<WordGroup>)groupList).Remove(item);
        }

        public void RemoveAt(int index)
        {
            groupList[index].Parent = null;
            ((IList<WordGroup>)groupList).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<WordGroup>)groupList).GetEnumerator();
        }
    }
}
