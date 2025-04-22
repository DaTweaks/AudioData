


namespace jp.nyatla.kokolink.types
{
    public class PyStopIteration : Exception
    {
        public PyStopIteration() : base() { }
        public PyStopIteration(Exception innerException) : base("", innerException) { }
    }

    sealed public class PyIterator<T> : IPyIterator<T>
    {

        readonly private IEnumerator<T> _src;
        public PyIterator(IEnumerable<T> src)
        {
            this._src = src.GetEnumerator();
        }

        public PyIterator(IEnumerator<T> src)
        {
            this._src = src;

        }
        public T Next()
        {
            if (!this._src.MoveNext())
            {
                throw new PyStopIteration();
            }
            return this._src.Current;
        }
    }




    // IPyIteratorを連結するイテレータ
    public class IterChain<T> : IPyIterator<T>
    {
        readonly private IPyIterator<IPyIterator<T>> _src;
        private IPyIterator<T>? _current;
        public IterChain(IPyIterator<T>[] src)
        {
            this._src = new PyIterator<IPyIterator<T>>(src);
            this._current = null;
        }
        public T Next()
        {
            while (true)
            {
                if (this._current == null)
                {
                    try
                    {
                        this._current = this._src.Next();
                    }
                    catch (PyStopIteration)
                    {   //イテレーション配列の終端なら終了。
                        throw;
                    }
                }
                try
                {
                    return this._current.Next();
                }
                catch (PyStopIteration)
                {   //値取得で失敗したらイテレーションの差し替え。
                    this._current = null;
                    continue;
                }

            }

        }

    }
    //  定数個の値を返すイテレータ
    public class Repeater<T> : IPyIterator<T>
    {
        private T _v;
        private int _count;
        public Repeater(T v,int count)
        {
            this._v = v;
            this._count = count;
        }
        public T Next()
        {
            if (this._count==0)
            {
                throw new PyStopIteration();
            }
            this._count--;
            return this._v;
        }
    }

    abstract public class BasicIterator<T>:IPyIterator<T>{
        abstract public T Next();
    }    
}