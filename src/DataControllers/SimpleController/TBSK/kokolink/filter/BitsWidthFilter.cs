using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.types;
namespace jp.nyatla.kokolink.filter
{



    // """ 任意ビット幅のintストリームを任意ビット幅のint値にエンコードします。
    // """
    class BitsWidthFilter:BasicRoStream<int>,IFilter<BitsWidthFilter,IRoStream<int>,int>
    {
        
        private Int64 _pos;
        private int _input_bits;
        private int _output_bits;
        private BitsWidthConvertIterator? _iter;
        public BitsWidthFilter(int input_bits=8,int output_bits=1):base(){
            this._input_bits=input_bits;
            this._output_bits=output_bits;
            this._iter=null;
        }

        public BitsWidthFilter SetInput(IRoStream<int> src)
        {
            this._pos=0;
            this._iter=src==null?null:new BitsWidthConvertIterator(src,this._input_bits,this._output_bits);
            return this;
        }

        override public int Next()
        {
            if(this._iter==null){
                throw new PyStopIteration();
            }
            var r=this._iter.Next();
            this._pos=this._pos+1;
            return r;
        }
        // @property
        override public Int64 Pos
        {
            get=>this._pos;
        }
    }
}

