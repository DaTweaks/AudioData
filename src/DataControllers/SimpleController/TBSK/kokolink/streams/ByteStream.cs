using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.streams.bytestreams;

namespace jp.nyatla.kokolink.streams{


    class ByteStream:BasicByteStream{
        class ByteCastIter : IPyIterator<int>
        {
            readonly private IPyIterator<byte>  _src;
            public ByteCastIter(IPyIterator<byte> src)
            {
                this._src = src;
            }
            public ByteCastIter(string src, string encoding)
            {
                
                this._src =Functions.ToPyIter<byte>(System.Text.Encoding.GetEncoding(encoding).GetBytes(src));
            }

            public int Next()
            {
                return this._src.Next();
            }
        }

        private Int64 _pos;
        readonly private IPyIterator<int> _iter;
        // """ iterをラップするByteStreamストリームを生成します。
        //     bytesの場合は1バイトづつ返します。
        //     strの場合はbytesに変換してから1バイトづつ返します。
        // """
        public ByteStream(IPyIterator<int> src,int inital_pos=0){
            this._pos=inital_pos;// #現在の読み出し位置
            this._iter=src;
        }
        public ByteStream(IPyIterator<byte> src,int inital_pos=0):this(new ByteCastIter(src),inital_pos:inital_pos)
        {
        }
        public ByteStream(string src,int inital_pos=0,string encoding="utf-8") : this(new ByteCastIter(src, encoding), inital_pos: inital_pos)
        {
        }
        override public int Next(){
            var r=this._iter.Next();
            this._pos=this._pos+1;
            return r;
        }
        override public Int64 Pos
        {
            get=>this._pos;
        }
    }

}