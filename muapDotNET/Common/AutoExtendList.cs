using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace muapDotNET.Common
{
    public class AutoExtendList<T>
    {
        public T this[int index]
        {
            get
            {
                return Get(index);
            }
            set
            {
                Set(index, value);
            }
        }

        private List<T> buf;

        public int Count
        {
            get
            {
                return buf == null ? 0 : buf.Count;
            }
        }

        public AutoExtendList()
        {
            buf = new List<T>();
        }

        public AutoExtendList(List<T> iniLst)
        {
            buf = iniLst;
        }

        public void Set(int adr, T d)
        {
            if (adr >= buf.Count)
            {
                int size = adr + 1;
                for (int i = buf.Count; i < size; i++)
                    buf.Add(default(T));
            }
            buf[adr] = d;
        }

        public T Get(int adr)
        {
            if (adr >= buf.Count) return default(T);
            return buf[adr];
        }

        public void Clear()
        {
            buf = new List<T>();
        }

        public T[] ToArray()
        {
            return buf.ToArray();
        }

        public void RemoveAll(int index = 0)
        {
            if (index < 0 || index >= buf.Count) return;
            buf.RemoveRange(index, buf.Count - index);
        }
    }
}
