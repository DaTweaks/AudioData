using AudioData.Encoding.RS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.Encoders.Encoding.RS
{
    public class RSEncoder : Encoder
    {
        ReedSolomonDecoder decoder;
        ReedSolomonEncoder encoder;

        public override string EncoderName => "Reed-Solomon Encoder";

        public RSEncoder()
        {
            GenericGF generic = GenericGF.DATA_MATRIX_FIELD_256;
            decoder = new ReedSolomonDecoder(generic);
            encoder = new ReedSolomonEncoder(generic);
        }

        public override bool[] Decode(bool[] binary)
        {
            binary = MakeLengthMultipleOf(binary, 8);
            byte[] allBytes = BoolsToBytes(binary);

            int eccLength = 8;

            if (allBytes.Length < eccLength + 2)
                return binary;

            // Split message and ECC
            byte[] messageWithLength = allBytes.Take(allBytes.Length - eccLength).ToArray();
            byte[] ecc = allBytes.Skip(allBytes.Length - eccLength).ToArray();

            // Decode message (includes length bytes)
            byte[] corrected = decoder.DecodeEx(messageWithLength, ecc);

            if(corrected == null)
            {
                return binary;
            }

            // Extract length
            byte[] lengthBytes = corrected.Take(2).ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            ushort messageLength = BitConverter.ToUInt16(lengthBytes, 0);

            // Extract actual message
            byte[] message = corrected.Skip(2).Take(messageLength).ToArray();

            return BytesToBools(message);
        }

        public override bool[] Encode(bool[] binary)
        {
            binary = MakeLengthMultipleOf(binary, 8);
            byte[] message = BoolsToBytes(binary);
            ushort messageLength = (ushort)message.Length;

            // Prepend length
            byte[] lengthBytes = BitConverter.GetBytes(messageLength);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes); // big-endian

            byte[] messageWithLength = lengthBytes.Concat(message).ToArray();

            int eccLength = 8;
            byte[] ecc = encoder.EncodeEx(messageWithLength, eccLength);

            byte[] finalData = messageWithLength.Concat(ecc).ToArray();
            return BytesToBools(finalData);
        }
    }
}


