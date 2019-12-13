using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFX
{
    public class CircularQueue<T> : IEnumerator<T>, IEnumerable<T>
    {
        private int mCurrentIndex = 0;
        private int mCount = 0;
        private int mReserveSize = 0;
        private int mPosition = -1;
        List<T> mList = new List<T>();

        public int Count { get { return mCount; } }

        public void Init(int _count)
        {
            mList.Clear();
            mList.AddRange(new T[_count]);
            mReserveSize = _count;
            mCurrentIndex = 0;
            mCount = 0;
            mPosition = -1;
        }

        public void Add(T _item)
        {
            mList[mCurrentIndex] = _item;
            mCurrentIndex = (mCurrentIndex + 1) % mReserveSize;
            mCount = Math.Min(mCount + 1, mReserveSize);
        }
        public void Clear()
        {
            mCurrentIndex = 0;
            mCount = 0;
            mPosition = -1;
        }
        public T[] ToArray()
        {
            T[] buf = new T[Count];
            int position = FirstIndex();
            for (int i = 0; i<Count; ++i)
            {
                int curIdx = (position + i) % mReserveSize;
                buf[i] = mList[curIdx];
            }
            return buf;
        }

        public T this[int _idx]
        {
            get
            {
                int position = FirstIndex();
                int curIdx = (position + _idx) % mReserveSize;
                return mList[curIdx];
            }
        }

        public T Current
        {
            get { return mList[mPosition]; }
        }

        object IEnumerator.Current
        {
            get { return mList[mPosition]; }
        }

        public void Dispose()
        {
            mPosition = -1;
        }

        public bool MoveNext()
        {
            if (mCount == 0)
                return false;

            if(mPosition < 0)
            {
                mPosition = FirstIndex();
                return true;
            }

            mPosition = (mPosition + 1) % mReserveSize;
            return (mPosition == mCurrentIndex) ? false : true;
        }

        public void Reset()
        {
            mPosition = FirstIndex();
        }

        private int FirstIndex()
        {
            return (mReserveSize + mCurrentIndex - mCount) % mReserveSize;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
