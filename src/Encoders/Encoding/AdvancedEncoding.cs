using AudioData.Encoders.Encoding.RS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.Encoders.Encoding
{
    public class AdvancedEncoding : Encoder // Giga Chad Encoder
    {
        HammingEncoder hamming;
        RSEncoder RS;
        public AdvancedEncoding()
        {
            RS = new RSEncoder();
            hamming = new HammingEncoder();
        }

        public override string EncoderName => "S-DART Encoder";

        public override bool[] Decode(bool[] binary)
        {
            binary = hamming.Decode(binary);
            return RS.Decode(binary);
        }

        public override bool[] Encode(bool[] binary)
        {
            binary = RS.Encode(binary);
            return hamming.Encode(binary);
        }
    }
}
