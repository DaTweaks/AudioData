using jp.nyatla.kokolink.utils.math.corrcoef;
using jp.nyatla.kokolink.types;



namespace jp.nyatla.kokolink.protocol.tbsk
{
    /**
     * 使用する実装を切り替えるスイッチ
     */
    public class AlgorithmSwitch
    {
        public static ISelfCorrcoefIterator CreateSelfCorrcoefIterator(int window, IPyIterator<double> src, int shift = 0)
        {
            return new SelfCorrcoefIterator2(window, src, shift:shift);
        }
    }
}


