
using System.Diagnostics;
namespace jp.nyatla.kokolink.utils
{
    /**
     * 正規化された浮動小数点値と固定小数点値を相互変換します。
     */
    public class FloatConverter
    {
        public static double ByteToDouble(byte b)
        {
            return (double)b / 255 - 0.5;
        }
        public static double Int16ToDouble(Int16 b)
        {
            if (b >= 0)
            {
                return ((double)b) / Int16.MaxValue;
            }
            else
            {
                return -(((double)b) / Int16.MinValue);
            }
        }

        public static byte DoubleToByte(double b)
        {
            return (byte)(b * 127 + 128);
        }
        public static Int16 DoubleToInt16(double b)
        {
            Debug.Assert(1 >= b && b >= -1);
            if (b >= 0)
            {
                return (Int16)(Int16.MaxValue * b);
            }
            else
            {
                return (Int16)(-Int16.MinValue * b);
            }

        }
    }


}




