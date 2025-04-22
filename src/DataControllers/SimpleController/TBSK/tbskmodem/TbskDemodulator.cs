using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.filter;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.protocol.tbsk.preamble;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.protocol.tbsk.traitblockcoder;
using jp.nyatla.kokolink.protocol.tbsk.tbaskmodem;

namespace jp.nyatla.tbaskmodem
{












    public class TbskDemodulator: TbskDemodulator_impl
    {
        // """ nBit intイテレータから1バイト単位のhex stringを返すフィルタです。
        // """
        private class Bits2HexStrFilter : BasicRoStream<string>, IFilter<Bits2HexStrFilter, IRoStream<int>, string>
        {
            readonly private BitsWidthFilter _src;
            public Bits2HexStrFilter(int input_bits = 1)
            {
                this._src = new BitsWidthFilter(input_bits: input_bits, output_bits: 8);
            }

            override public string Next()
            {
                while (true)
                {
                    int d;
                    try
                    {
                        d = this._src.Next();
                    }
                    catch (RecoverableStopIteration)
                    {
                        throw;
                    }
                    Debug.Assert(0 < d && d < 256);
                    return Convert.ToString(d, 16);

                }
            }
            public Bits2HexStrFilter SetInput(IRoStream<int> src)
            {
                this._src.SetInput(src);
                return this;
            }
            override public Int64 Pos
            {
                get => this._src.Pos;
            }
        }





        public TbskDemodulator(TraitTone tone):base(tone,null)
        {
        }
        public TbskDemodulator(TraitTone tone, double preamble_th, int preamble_cycle):base(tone,new CoffPreamble(tone,threshold:preamble_th,cycle:preamble_cycle))
        {
        }
        public TbskDemodulator(TraitTone tone, Preamble preamble) : base(tone, preamble)
        {
        }



        public class DemodulateAsIntAS : AsyncDemodulateX<int>
        {
            public DemodulateAsIntAS(TbskDemodulator parent, IPyIterator<double> src, int bitwidth) :
                base(parent, src, (TraitBlockDecoder src) => new BitsWidthFilter(1, bitwidth).SetInput(src))
            { }
        }




        public ISequentialEnumerable<int>? DemodulateAsBit(IEnumerable<double> src)
        {
            var w = this.DemodulateAsBit(Functions.ToPyIter(src));
            if (w == null) { return null; 
            } else{
                return SequentialEnumerable<int>.CreateInstance(w);
            }
        }

        //    """ TBSK信号からnビットのint値配列を復元します。
        //        関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。
        //    """
        public ISequentialEnumerable<int>? DemodulateAsInt(IPyIterator<double> src, int bitwidth = 8)
        {
            Debug.Assert(!this._asmethod_lock);
            DemodulateAsIntAS asmethod = new DemodulateAsIntAS(this, src, bitwidth);
            if (asmethod.Run())
            {
                Debug.Assert(asmethod.Result != null);
                return SequentialEnumerable<int>.CreateInstance(asmethod.Result);
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new RecoverableException<DemodulateAsIntAS, IPyIterator<int>?>(asmethod);
            }
        }
        public ISequentialEnumerable<int>? DemodulateAsInt(IEnumerable<double> src, int bitwidth = 8)
        {
            return this.DemodulateAsInt(Functions.ToPyIter(src), bitwidth: bitwidth);
        }
        public class DemodulateAsByteAS : AsyncDemodulateX<byte>
        {
            public DemodulateAsByteAS(TbskDemodulator parent, IPyIterator<double> src) :
                base(parent, src, (TraitBlockDecoder src) => new Bits2BytesFilter(input_bits: 1).SetInput(src))
            { }
        }
        public class DemodulateAsStrAS : AsyncDemodulateX<char>
        {
            public DemodulateAsStrAS(TbskDemodulator parent, IPyIterator<double> src, string encoding = "utf-8") :
                base(parent, src, (TraitBlockDecoder src) => new Bits2StrFilter(input_bits: 1, encoding: encoding).SetInput(src))
            { }
        }
        public class DemodulateAsHexStrAS : AsyncDemodulateX<string>
        {
            public DemodulateAsHexStrAS(TbskDemodulator parent, IPyIterator<double> src) :
                base(parent, src, (TraitBlockDecoder src) => new Bits2HexStrFilter(input_bits: 1).SetInput(src))
            { }
        }

        //    """ TBSK信号からバイト単位でbytesを返します。
        //        途中でストリームが終端した場合、既に読みだしたビットは破棄されます。
        //        関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。   
        //    """
        public ISequentialEnumerable<byte>? DemodulateAsBytes(IPyIterator<double> src)
        {
            Debug.Assert(!this._asmethod_lock);
            DemodulateAsByteAS asmethod = new DemodulateAsByteAS(this, src);
            if (asmethod.Run())
            {                
                return asmethod.Result == null?null: SequentialEnumerable<byte>.CreateInstance(asmethod.Result);
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new RecoverableException<DemodulateAsByteAS, IPyIterator<byte>?>(asmethod);
            }
        }
        public ISequentialEnumerable<byte>? DemodulateAsBytes(IEnumerable<double> src)
        {
            return this.DemodulateAsBytes(Functions.ToPyIter(src));
        }
        public ISequentialEnumerable<char>? DemodulateAsStr(IEnumerable<double> src, string encoding = "utf-8")
        {
            return this.DemodulateAsStr(Functions.ToPyIter(src), encoding: encoding);
        }

        //    """ TBSK信号からsize文字単位でstrを返します。
        //        途中でストリームが終端した場合、既に読みだしたビットは破棄されます。
        //        関数は信号を検知する迄制御を返しません。信号を検知せずにストリームが終了した場合はNoneを返します。
        //    """
        public ISequentialEnumerable<char>? DemodulateAsStr(IPyIterator<double> src,string encoding="utf-8")
        {
            Debug.Assert(!this._asmethod_lock);

            DemodulateAsStrAS asmethod = new DemodulateAsStrAS(this, src, encoding);
            if (asmethod.Run())
            {
                return asmethod.Result == null ? null : SequentialEnumerable<char>.CreateInstance(asmethod.Result);
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new RecoverableException<DemodulateAsStrAS, IPyIterator<char>?>(asmethod);
            }
        }


        public ISequentialEnumerable<string>? DemodulateAsHexStr(IPyIterator<double> src)
        {
            Debug.Assert(!this._asmethod_lock);
            DemodulateAsHexStrAS asmethod = new DemodulateAsHexStrAS(this, src);
            if (asmethod.Run())
            {
                return asmethod.Result == null ? null : SequentialEnumerable<string>.CreateInstance(asmethod.Result);
            }
            else
            {
                this._asmethod_lock = true;// #解放はAsyncDemodulateXのcloseで
                throw new RecoverableException<DemodulateAsHexStrAS, IPyIterator<string>?>(asmethod);
            }
        }
        public ISequentialEnumerable<string>? DemodulateAsHexStr(IEnumerable<double> src)
        {
            return this.DemodulateAsHexStr(Functions.ToPyIter(src));
        }

    }


}
