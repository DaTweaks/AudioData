using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.DataControllers
{
    public class QPSK : DataControl
    {
       
        public override string BinaryToString(bool[] binary)
        {
            throw new NotImplementedException();
        }

        public override bool[] DecodeAudioToData(string fileName) // Kind of weird way to do it. 
        {
            throw new NotImplementedException();
        }

        public override bool[] DecodeAudioToData(float[] audioData)
        {
            throw new NotImplementedException();
        }

        public override float[] EncodeDataToAudio(bool[] data, float noise)
        {
            throw new NotImplementedException();
        }

        public override bool[] StringToBinary(string text)
        {
            throw new NotImplementedException();
        }
    }
}
