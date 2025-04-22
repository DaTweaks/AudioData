using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.Encoders.Encoding
{
    public class NoEncoding : Encoder
    {
        public override string EncoderName => "No Encoding";

        public override bool[] Decode(bool[] binary)
        {
            return binary;
        }

        public override bool[] Encode(bool[] binary)
        {
            return binary;
        }
    }
}
