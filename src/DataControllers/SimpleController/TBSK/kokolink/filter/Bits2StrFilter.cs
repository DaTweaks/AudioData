using System.Diagnostics;
using System.Text;
using System;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils;
using jp.nyatla.kokolink.utils.recoverable;
using System.Runtime.InteropServices;

namespace jp.nyatla.kokolink.filter
{


    // """ nBit intイテレータから1バイト単位のbytesを返すフィルタです。
    // """
    class Bits2StrFilter : BasicRoStream<char>, IFilter<Bits2StrFilter, IRoStream<int>, char>
    {
        private Int64 _pos;
        private int _input_bits;
        private IPyIterator<int>? _iter;
        private BrokenTextStreamDecoder _decoder;

        public Bits2StrFilter(int input_bits = 1, string encoding = "utf-8"):base()
        {
            this._input_bits = input_bits;
            this._decoder = new BrokenTextStreamDecoder(encoding);
        }
        public Bits2StrFilter SetInput(IRoStream<int> src)
        {
            this._pos = 0;
            this._iter = src == null ? null : new BitsWidthConvertIterator(src!, this._input_bits, 8);
            return this;
        }
        override public char Next()
        {
            if (this._iter == null)
            {
                throw new PyStopIteration();
            }
            while (true)
            {
                try
                {
                    while (true)
                    {
                        var r = this._decoder.Update((byte)this._iter.Next());
                        if (r != null)
                        {
                            return (char)r;
                        }
                    }
                }
                catch (RecoverableStopIteration e)
                {
                    throw e;
                }catch(PyStopIteration e)
                {
                    var r = this._decoder.Update();
                    if (r == null)
                    {
                        throw e;
                    }
                    return (char)r;
                }

            }
        }
        override public Int64 Pos
        {
            get => this._pos;
        }
    }

}