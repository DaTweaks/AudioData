// from typing import TypeVar
// from ..types import Iterator
// from .RingBuffer import RingBuffer
// from .recoverable import RecoverableIterator, RecoverableStopIteration
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.recoverable;
// T=TypeVar("T")

namespace jp.nyatla.kokolink.utils.math
{
    // """ ストリームの読み出し位置から過去N個の合計を返すイテレータです。
    //     このイテレータはRecoverableStopInterationを利用できます。
    // """
    class SumIterator:BasicRecoverableIterator<double>
    {
        readonly private IPyIterator<double> _src;
        private double _sum;
        private RingBuffer<double> _buf;

        // def __init__(self,src:Iterator[T],length:int):
        //     self._src=src
        //     self._buf=RingBuffer[T](length,0)
        //     self._sum=0
        //     # self._length=length
        //     # self._num_of_input=0
        //     self._gen=None
        //     return
        public SumIterator(IPyIterator<double> src,int length){
            this._src=src;
            this._buf=new RingBuffer<double>(length,0);
            this._sum=0;
            // # self._length=length
            // # self._num_of_input=0
            // self._gen=None
            return;

        }

        // def __next__(self) -> T:
        //     try:
        //         s=next(self._src)
        //     except RecoverableStopIteration as e:
        //         raise RecoverableStopIteration(e)
        //     d=self._buf.append(s)
        //     self._sum=self._sum+s-d
        //     # self._num_of_input=self._num_of_input+1
        //     return self._sum
        override public double Next()
        {
            var s=this._src.Next();
            var d=this._buf.Append(s);
            this._sum=this._sum+s-d;
            // # self._num_of_input=self._num_of_input+1
            return this._sum;
        }
        // @property
        // def buf(self)->RingBuffer[T]:
        //     return self._buf
        public RingBuffer<double> Buf{
            get=>this._buf;
        }
    }

}




