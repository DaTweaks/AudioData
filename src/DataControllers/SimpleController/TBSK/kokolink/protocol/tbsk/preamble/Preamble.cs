using jp.nyatla.kokolink.interfaces;
namespace jp.nyatla.kokolink.protocol.tbsk.preamble
{

    public interface Preamble{
        IRoStream<double> GetPreamble();
        int? WaitForSymbol(IRoStream<double> src);
    }
}