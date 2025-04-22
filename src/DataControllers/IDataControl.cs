using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioData.DataControllers
{
    public interface IDataControl
    {
        public int GetBitsPerSecond();
        public string GetName();
        public string GetDescription();
        public int GetOptimalDetectionTone();
        public bool[] DecodeAudioToData(float[] audioData, int sampleRate);
        public float[] EncodeDataToAudio(bool[] data, int SampleRate, float noise = 0f);
    }
}
