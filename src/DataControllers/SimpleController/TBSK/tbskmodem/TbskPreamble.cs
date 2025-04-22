using jp.nyatla.kokolink.protocol.tbsk.preamble;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;

namespace jp.nyatla.tbaskmodem
{
    public class TbskPreamble
    {
        public static Preamble createCoff(TraitTone tone)
        {
            return createCoff(tone, CoffPreamble.DEFAULT_TH, CoffPreamble.DEFAULT_CYCLE);
        }
        public static Preamble createCoff(TraitTone tone, double threshold, int cycle)
        {
            return new CoffPreamble(tone, threshold, cycle);
        }
    }





}
