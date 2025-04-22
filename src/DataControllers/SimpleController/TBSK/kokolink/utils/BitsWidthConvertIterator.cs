using System;
using System.Collections.Generic;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.types;
// T=TypeVar("T")

namespace jp.nyatla.kokolink.utils
{
    class StopIteration_BitsWidthConvertIterator_FractionalBitsLeft:PyStopIteration
    {
        public StopIteration_BitsWidthConvertIterator_FractionalBitsLeft() : base() { }
        public StopIteration_BitsWidthConvertIterator_FractionalBitsLeft(Exception innerException) : base(innerException) { }

    }

    // """ 任意ビット幅のintストリームを任意ビット幅のint値に変換するイテレータです。
    // """
    class BitsWidthConvertIterator:IRecoverableIterator<int>{
        readonly private IPyIterator<int> _src;
        private bool _is_eos;
        private int _input_bits;
        private int _output_bits;
        private UInt32 _bits;
        private int _n_bits;
        // def __init__(self,src:Iterator[int],input_bits:int=8,output_bits:int=1):
        //     """
        //     """
        //     super().__init__()
        //     self._src=src
        //     self._is_eos=False
        //     self._input_bits=input_bits
        //     self._output_bits=output_bits
        //     self._bits  =0#byte値
        //     self._n_bits=0 #読み出し可能ビット数
        public BitsWidthConvertIterator(IPyIterator<int> src,int input_bits=8,int output_bits=1)
        {
            this._src=src;
            this._is_eos=false;
            this._input_bits=input_bits;
            this._output_bits=output_bits;
            this._bits  =0;//#byte値
            this._n_bits=0;//#読み出し可能ビット数

        }
        // def __next__(self)->int:
        //     if self._is_eos:
        //         raise StopIteration()
        //     n_bits=self._n_bits
        //     bits  =self._bits
        //     while n_bits<self._output_bits:
        //         try:
        //             d=next(self._src)
        //         except RecoverableStopIteration as e:
        //             self._bits=bits
        //             self._n_bits=n_bits
        //             raise RecoverableStopIteration(e)
        //         except StopIteration as e:
        //             self._is_eos=True
        //             if n_bits!=0:
        //                 # print("Fraction")
        //                 raise StopIteration_BitsWidthConvertIterator_FractionalBitsLeft(e)
        //             raise StopIteration(e)
        //         bits=(bits<<self._input_bits) | d
        //         n_bits=n_bits+self._input_bits

        //     r:int=0
        //     for _ in range(self._output_bits):
        //         r=(r<<1) | ((bits>>(n_bits-1))&0x01)
        //         n_bits=n_bits-1

        //     self._n_bits=n_bits
        //     self._bits=bits
        //     return r
        public int Next()
        {
            if(this._is_eos){
                throw new PyStopIteration();
            }
            var n_bits=this._n_bits;
            var bits  =this._bits;
            while(n_bits<this._output_bits){
                uint d;
                try{
                    d=(uint)this._src.Next();
                }catch(RecoverableStopIteration e){
                    this._bits=bits;
                    this._n_bits=n_bits;
                    throw e;
                }catch(PyStopIteration e){
                    this._is_eos=true;
                    if(n_bits!=0){
                        // # print("Fraction")
                        throw new StopIteration_BitsWidthConvertIterator_FractionalBitsLeft(e);
                    }
                    throw new PyStopIteration(e);
                }
                bits=(bits<<this._input_bits) | d;
                n_bits=n_bits+ this._input_bits;
            }
            uint r=0;
            for(var i=0;i<this._output_bits;i++){
                r=(r<<1) | ((bits>>(n_bits-1))&0x01);
                n_bits=n_bits-1;
            }
            this._n_bits=n_bits;
            this._bits=bits;
            return (int)r;
        }
    }
}
