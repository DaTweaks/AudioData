using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.utils.math;

namespace jp.nyatla.tbaskmodem
{
    public class TbskTone
    {
        public static SinTone CreateSin()
        {
            return CreateSin(10, 10);
        }
        public static SinTone CreateSin(int points)
        {
            return CreateSin(points, 1);
        }
        public static SinTone CreateSin(int points, int cycle)
        {
            return new SinTone(points, cycle);
        }

        public static XPskSinTone CreateXPskSin()
        {
            return CreateXPskSin(10, 10);
        }
        public static XPskSinTone CreateXPskSin(int points)
        {
            return CreateXPskSin(points, 1);
        }
        public static XPskSinTone CreateXPskSin(int points, int cycle)
        {
            return new XPskSinTone(points, cycle, 8, null);
        }

        public static PnTone CreatePn(uint seed, int interval, TraitTone? base_tone)
        {
            return new PnTone(seed, interval, base_tone);
        }
        public static PnTone CreatePn(uint seed)
        {
            return CreatePn(seed, 2, null);
        }


        public static MSeqTone CreateMSeq(MSequence mseq)
        {
            return new MSeqTone(mseq, null);
        }
        public static MSeqTone CreateMSeq(int bits, int tap)
        {
            return CreateMSeq(new MSequence(bits, tap));
        }
        public static MSeqTone CreateMSeq(int bits, int tap, TraitTone base_tone)
        {
            return new MSeqTone(new MSequence(bits, tap), base_tone);
        }

        public static TraitTone CreateCostom(IEnumerable<double> patt)
        {
            return new TraitTone(patt);
        }

    }


}
