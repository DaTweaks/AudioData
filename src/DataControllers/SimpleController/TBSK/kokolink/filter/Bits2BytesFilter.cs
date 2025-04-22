using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.types;

namespace jp.nyatla.kokolink.filter
{

    // """ nBit intイテレータから1バイト単位のbytesを返すフィルタです。
    // """
    class Bits2BytesFilter : BasicRoStream<byte>, IFilter<Bits2BytesFilter, IRoStream<int>, byte>
    {
        private Int64 _pos;
        readonly private int _input_bits;
        private IPyIterator<int>? _iter;

        public Bits2BytesFilter(int input_bits = 1)
        {
            this._input_bits = input_bits;

        }
        public Bits2BytesFilter SetInput(IRoStream<int> src)
        {
            this._pos = 0;
            this._iter = src == null ? null : new BitsWidthConvertIterator(src, this._input_bits, 8);
            return this;
        }
        override public byte Next()
        {
            if (this._iter == null) {
                throw new PyStopIteration();
            }
            var r = this._iter.Next();
            this._pos = this._pos + 1;
            Debug.Assert(0 <= r && r <= 255);
            return (byte)r;

        }
        override public Int64 Pos
        {
            get=>this._pos;
        }
    }
}