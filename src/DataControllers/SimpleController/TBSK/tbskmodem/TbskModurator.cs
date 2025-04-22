using System.Diagnostics;
using jp.nyatla.kokolink.streams;
using jp.nyatla.kokolink.filter;
using jp.nyatla.kokolink.protocol.tbsk.preamble;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.protocol.tbsk.tbaskmodem;
using jp.nyatla.kokolink.types;

namespace jp.nyatla.tbaskmodem
{

    /**
     * Preamble
     */
    public class TbskModulator : TbskModulator_impl
    {
        public TbskModulator(TraitTone tone):base(tone,null)
        {
        }
        public TbskModulator(TraitTone tone, int preamble_cycle) : base(tone, new CoffPreamble(tone,cycle:preamble_cycle))
        {
        }

        public TbskModulator(TraitTone tone, Preamble preamble) : base(tone, preamble)
        {
        }
        private class SuffixTone: IPyIterator<double>
        {
            private TraitTone _tone;
            private int _c = 0;
            public SuffixTone(TraitTone tone)
            {
                this._tone = tone;
                this._c = 0;
            }
            public double Next()
            {
                var c = this._c;
                if (c >= this._tone.Count)
                {
                    throw new PyStopIteration();
                }
                var tone = this._tone;
                var r = c % 2 == 0 ? tone[c] * 0.5 : -tone[c] * 0.5;
                this._c++;
                return r;
            }

        }
        private ISequentialEnumerable<double> _ModulateAsBit(IPyIterator<int> src, bool stopsymbol)
        {
            SuffixTone? suffix = null;
            if (stopsymbol)
            {
                suffix = new SuffixTone(this._tone);
            }
            //既にIPyIteratorを持っていたらそのまま使う。
            return SequentialEnumerable<double>.CreateInstance(
                base.ModulateAsBit(src, suffix: suffix)
            );
        }


        public IEnumerable<double> ModulateAsBit(IEnumerable<int> src, bool stopsymbol = true)
        {
            return this._ModulateAsBit(Functions.ToPyIter<int>(src),stopsymbol);
        }



        public ISequentialEnumerable<double> ModulateAsHexStr(string src, bool stopsymbol = true)
        {
            // """ hex stringを変調します。
            //     hex stringは(0x)?[0-9a-fA-F]{2}形式の文字列です。
            //     hex stringはbytesに変換されて送信されます。
            // """
            Debug.Assert(src.Length % 2 == 0);
            if (src.Substring(0, 2) == "0x")
            {
                src = src.Substring(2, src.Length - 2);
            }
            var d = new List<byte>();
            for (var i = 0; i < src.Length / 2; i++)
            {
                d.Add(Convert.ToByte(src[i * 2] + src[i * 2 + 1]));
            }
            return this.Modulate(d, stopsymbol);
        }



        public ISequentialEnumerable<double> Modulate(IEnumerable<int> src, int bitwidth = 8, bool stopsymbol= true)
        {
            //既にIPyIteratorを持っていたらそのまま使う。
            return this._ModulateAsBit(
                new BitsWidthFilter(bitwidth).SetInput(new RoStream<int>(Functions.ToPyIter<int>(src))),stopsymbol
            );
        }
        public ISequentialEnumerable<double> Modulate(IEnumerable<byte> src, bool stopsymbol = true)
        {
            //既にIPyIteratorを持っていたらそのまま使う。
            return this._ModulateAsBit(
                new BitsWidthFilter(8).SetInput(new ByteStream(Functions.ToPyIter<byte>(src))), stopsymbol
                );
        }
        public ISequentialEnumerable<double> Modulate(string src, string encoding = "utf-8", bool stopsymbol = true)
        {
            return this._ModulateAsBit(
                new BitsWidthFilter(8).SetInput(new ByteStream(src, encoding: encoding)), stopsymbol
                );
        }
    }
}
