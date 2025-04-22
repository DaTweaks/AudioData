using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.recoverable;

namespace jp.nyatla.kokolink.compatibility
{
    public interface IBinaryReader
    {
        public byte[] ReadBytes(int size);

        sealed public int ReadInt32LE()
        {
            byte[] b = this.ReadBytes(4);
            return (int)(((0xff & b[3]) << 24) | ((0xff & b[2]) << 16) | ((0xff & b[1]) << 8) | (0xff & b[0]));
        }

        sealed public int ReadInt16LE()
        {
            byte[] b = this.ReadBytes(2);
            return (int)((0xff & b[1]) << 8) | (0xff & b[0]);
        }
    }


    public interface IBinaryWriter
    {
        public int WriteBytes(List<byte> buf);
        public int WriteBytes(byte[] buf);
    }
    public class MemBuffer : List<byte>
    {

        public int WriteBytes(IBinaryReader s, int size)
        {
            return this.WriteBytes(s.ReadBytes(size));

        }

        private int _WriteBytes(byte[] s)
        {
            foreach (var i in s)
            {
                this.Add(i);
            }
            return this.Count - (s.Length);
        }



        public int WriteBytes(String s, int size)
        {
            var a = System.Text.Encoding.GetEncoding("us-ascii").GetBytes(s);
            if (a.Length != size)
            {
                throw new InvalidCastException();
            }
            return this._WriteBytes(a);
        }
        public int WriteBytes(Byte[] s)
        {
            return this.WriteBytes(s, 0);
        }
        public int WriteBytes(Byte[] s, int padding)
        {
            var r = this._WriteBytes(s);
            var t = new Byte[] { 0 };
            for (var i = 0; i < padding; i++)
            {
                this._WriteBytes(t);
            }
            return r;
        }
        public int WriteInt16LE(int v)
        {
            var w = new Byte[]{
            (byte)((v >> 0) & 0xff),
            (byte)((v >> 8) & 0xff)
            };
            return this.WriteBytes(w);
        }
        public int WriteInt32LE(int v)
        {
            var w = new Byte[]{
            (byte)((v >> 0) & 0xff),
            (byte)((v >> 8) & 0xff),
            (byte)((v >> 16) & 0xff),
            (byte)((v >> 24) & 0xff)
            };
            return this.WriteBytes(w);
        }
        public byte[] AsBytes(int idx, int size)
        {
            var r = new byte[size];
            for (var i = 0; i < size; i++)
            {
                r[i] = this[i + idx];
            }
            return r;
        }
        public String AsStr(int idx, int size)
        {
            var s = this.GetRange(idx, size);
            byte[] b = new byte[s.Count];
            for (int i = 0; i < s.Count; i++)
            {
                b[i] = s[i];
            }
            return System.Text.Encoding.GetEncoding("us-ascii").GetString(b);
        }
        public int AsInt32LE(int idx)
        {
            var w = this.GetRange(idx, 4).ToArray();
            return ((int)(0xff & w[3]) << 24) | ((int)(0xff & w[2]) << 16) | ((int)(0xff & w[1]) << 8) | (0xff & w[0]);
        }
        public int AsInt16LE(int idx)
        {
            var w = this.GetRange(idx, 2).ToArray();
            return ((int)((0xff & w[1]) << 8) | (0xff & w[0]));
        }
        public int Dump(IBinaryWriter dest)
        {
            return dest.WriteBytes(this);
        }
    }













    //  GetEnumeratorで読取位置をリセットされないIEnumerableです。
    //  新たに取得した場合は、現在の読取位置を起点としたインスタンスを返す実装をします。
    //
    public interface ISequentialEnumerable<T> : IEnumerable<T> { }



    //  IPyEnumeratorをソースにしたIEnumerator
    //  MoveToはIpyIteratorの仕様を引き継いでRecoverableStopIterationをスローすることがあります。
    public class PyIterSuorceIEnumerator<T> : IEnumerator<T>
    {
        private readonly IPyIterator<T> _src;
        private T? _current;
        public PyIterSuorceIEnumerator(IPyIterator<T> src)
        {
            Debug.Assert(src is not IEnumerator<T>); //Enumulableを持たないこと
            this._src = src;
            //this._current;
        }
        T IEnumerator<T>.Current
        {
            get
            {
                if (this._src != null && this._current != null)
                {
                    return this._current;
                }
                throw new InvalidOperationException();
            }
        }

        object IEnumerator.Current
        {
            get
            {
                if (this._src != null && this._current != null)
                {
                    return this._current;
                }
                throw new InvalidOperationException();
            }
        }



        bool IEnumerator.MoveNext()
        {
            try
            {
                var c = this._src.Next();
                if (c == null)
                {
                    throw new NullReferenceException();
                }
                this._current = c;
                return true;
            }
            catch (RecoverableStopIteration e)
            {
                throw e;
            }
            catch (PyStopIteration)
            {
                return false;
            }
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }
        void IDisposable.Dispose()
        {
            //nothing to do
            //throw new NotImplementedException();
        }
    }
    // このEnumerableは常に同じEnumerableを返します。

    public class SequentialEnumerable<T> : ISequentialEnumerable<T>
    {
        readonly private IEnumerator<T> _src;
        private SequentialEnumerable(IEnumerator<T> src)
        {
            this._src = src;
        }
        public static SequentialEnumerable<T> CreateInstance(IEnumerator<T> src)
        {
            return new SequentialEnumerable<T>(src);
        }
        public static SequentialEnumerable<T> CreateInstance(IPyIterator<T> src)
        {
            Debug.Assert(src is not IEnumerable<T>); //Enumulableを持たないこと
            return new SequentialEnumerable<T>(new PyIterSuorceIEnumerator<T>(src));
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this._src;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._src;
        }
    }






    public class Functions
    {
        static public IPyIterator<T> ToPyIter<T>(IEnumerable<T> s)
        {
            Debug.Assert(s is not ISequentialEnumerable<T>); //設計ミスのトラップ
            if (s is IPyIterator<T> iterator)
            {
                return iterator;
            }
            return new PyIterator<T>(s);
        }
        static public IPyIterator<T> ToPyIter<T>(T[] s)
        {
            return new PyIterator<T>(s);
        }



        static public List<T> ToList<T>(IPyIterator<T> src)
        {
            var tmp = new List<T>();
            try
            {
                while (true)
                {
                    tmp.Add(src.Next());
                }

            }
            catch (PyStopIteration)
            {
                //OK
            }
            return tmp;
        }
        static public T[] ToArray<T>(IPyIterator<T> src)
        {
            return ToList<T>(src).ToArray();
        }

        static public T[] Flatten<T>(params IEnumerable<T>[] s)
        {
            List<T> d = new List<T>();
            foreach (var i in s)
            {
                d.AddRange(i);
            }
            return d.ToArray();
        }
        static public T[] Repeat<T>(int n, T pad)
        {
            var r = new T[n];
            Array.Fill<T>(r, pad);
            return r;
        }

    }




}