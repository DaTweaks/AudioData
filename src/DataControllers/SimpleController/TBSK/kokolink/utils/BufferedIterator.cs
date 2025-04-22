using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.types;
// T=TypeVar("T")

namespace jp.nyatla.kokolink.utils
{
    // """ 任意範囲のログを取りながら返すイテレータ
    //     このイテレータはRecoverableStopInterationを利用できます。
    // """
    class BufferedIterator<T>:BasicRecoverableIterator<T>{
        readonly private RingBuffer<T> _buf;
        readonly private IPyIterator<T> _src;

        // def __init__(self,src:Iterator[T],size:int):
        //     self._src=src
        //     self._buf=RingBuffer(size,0)
        public BufferedIterator(IPyIterator<T> src,int size,T pad){
            this._src=src;
            this._buf=new RingBuffer<T>(size, pad);
        }
        override public T Next(){
            T d;
            try{
                d=this._src.Next();
            }catch(RecoverableStopIteration e){
                throw e;
            }
            this._buf.Append(d);
            return d;
        }
        // @property
        // def buf(self)->RingBuffer:
        //     return self._buf
        public RingBuffer<T> Buf{
            get=>this._buf;
        }
    }
}

