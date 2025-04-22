// """ BitStreamクラスを宣言します。
    
// """
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.utils;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.compatibility;

namespace jp.nyatla.kokolink.streams{
    class BitStream:BasicRoStream<int>,IBitStream
    {
        private Int64 _pos;
        readonly private BitsWidthConvertIterator _bw;
        // """ 任意ビット幅のintストリームを1ビット単位のビットストリームに展開します。
        // """
        public BitStream(IEnumerable<int> src, int bitwidth = 8):this(new PyIterator<int>(src),bitwidth:bitwidth)
        {
        }

        public BitStream(IPyIterator<int> src,int bitwidth=8){
            this._bw=new BitsWidthConvertIterator(src,bitwidth,1);
            this._pos=0;
        }
        override public int Next(){
            int r;
            try{
                r=this._bw.Next();
            }catch(RecoverableStopIteration e){
                throw e;
            }
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



