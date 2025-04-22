using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.wavefile.riffio;
using jp.nyatla.kokolink.utils.recoverable;
using System.Diagnostics;
using System.Data.Common;

namespace jp.nyatla.kokolink.utils.wavefile
{

    class SReader : IBinaryReader
    {
        Stream _src;
        public SReader(Stream src)
        {
            this._src = src;
        }
        public byte[] ReadBytes(int size)
        {
            var ret = new byte[size];
            for (var i = 0; i < size; i++)
            {
                var w = this._src.ReadByte();
                if (w < 0)
                {
                    throw new EndOfStreamException();
                }
                ret[i] = (byte)w;
            }
            return ret;
        }
    }
    class SWriter : IBinaryWriter
    {
        Stream _src;
        public SWriter(Stream src)
        {
            this._src = src;
        }
        public int WriteBytes(List<byte> buf)
        {
            return this.WriteBytes(buf.ToArray());
        }
        public int WriteBytes(byte[] buf)
        {
            this._src.Write(buf);
            return buf.Length;
        }
    }



    public class PcmData
    {
        private readonly WaveFile _wavfile;
        public static PcmData Load(IBinaryReader fp)
        {
            return new PcmData(fp);
        }
        public static PcmData Load(Stream src)
        {


            var io = new SReader(src);
            return new PcmData(io);
        }


        public static void Dump(PcmData src, IBinaryWriter dest)
        {
            src._wavfile.Dump(dest);
        }
        public static void Dump(PcmData src, Stream dest)
        {
            var io = new SWriter(dest);
            PcmData.Dump(src, io);
        }


        public PcmData(IBinaryReader fp)
        {
            this._wavfile = new WaveFile(fp);
            if (this._wavfile.Fmt == null)
            {
                throw new Exception();
            }
            if (this._wavfile.Data == null)
            {
                throw new Exception();
            }
        }

        public PcmData(byte[] src, int sample_bits, int frame_rate, List<Chunk>? chunks)
        {
            this._wavfile = new WaveFile(frame_rate, sample_bits / 8, 1, src, chunks);
            Debug.Assert(this._wavfile.Fmt != null);
            Debug.Assert(this._wavfile.Data != null);
        }
        public PcmData(byte[] src, int sample_bits, int frame_rate) : this(src, sample_bits, frame_rate, null) { }
        public PcmData(IPyIterator<double> src, int sample_bits, int frame_rate) : this(src, sample_bits, frame_rate, null) { }
        public PcmData(IPyIterator<double> src, int sample_bits, int frame_rate, List<Chunk>? chunks) : this(Float2bytes(src, sample_bits), sample_bits, frame_rate, chunks) { }
        public PcmData(IList<double> src, int sample_bits, int frame_rate) : this(new PyIterator<double>(src), sample_bits, frame_rate, null) { }


        // """サンプリングビット数
        // """

        public int SampleBits
        {
            get
            {
                Debug.Assert(this._wavfile.Fmt != null);
                return this._wavfile.Fmt.Samplewidth;

            }
        }
        // @property
        // def frame_rate(self)->int:
        //     """サンプリングのフレームレート
        //     """
        //     return self._frame_rate
        public int Framerate
        {
            get
            {
                Debug.Assert(this._wavfile.Fmt != null);
                return this._wavfile.Fmt.Framerate;

            }
        }
        public List<byte> Data
        {
            get
            {
                Debug.Assert(this._wavfile.Data != null);
                return this._wavfile.Data.Data;

            }
        }




        public int Byteslen
        {
            get
            {
                Debug.Assert(this._wavfile.Data != null);
                return this._wavfile.Data.Size;
            }
        }









        public List<double> DataAsFloat()
        {
            Debug.Assert(this._wavfile.Data!= null);
            var src = this._wavfile.Data.Data;
            var num_of_sample = src.Count;
            Debug.Assert(num_of_sample % (this.SampleBits / 8) == 0);

            var ret=new List<double>();
            if (this.SampleBits == 8)
            {
                foreach(var i in src)
                {
                    ret.Add(FloatConverter.ByteToDouble(i));
                }

            }else if (this.SampleBits == 16)
            {
                for (var i=0;i<num_of_sample;i+=2)
                {
                    Int16 v = (Int16)((UInt16)src[i] | ((UInt16)src[i+1] << 8));
                    ret.Add(FloatConverter.Int16ToDouble(v));
                }
            }
            else
            {
                throw new ArgumentException();
            }
            return ret;
        }



        static private byte[] Float2bytes(IPyIterator<Double> fdata, int bits)
        {
            List<byte> ret = new List<byte>();

            try
            {
                if (bits == 8)
                {
                    while (true)
                    {
                        ret.Add(FloatConverter.DoubleToByte(fdata.Next()));
                    }

                }
                else if (bits == 16)
                {
                    while (true)
                    {
                        var v = FloatConverter.DoubleToInt16(fdata.Next());
                        ret.Add((byte)(((UInt16)v) & 0xff));
                        ret.Add((byte)((v >> 8) & 0xff));
                    }
                }
                else
                {
                    throw new ArgumentException();
                }

            }
            catch (PyStopIteration)
            {
                return ret.ToArray();
            }
        }


    }
}