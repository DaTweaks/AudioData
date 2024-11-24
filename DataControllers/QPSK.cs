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
        const int SampleRate = 44100; // Hz
        const double BPS = 10;
        const double SymbolDuration = 1.0 / BPS; // seconds
        const double CarrierFrequency = 1000; // Carrier frequency in Hz

        private static readonly double[] Phases = { 0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 };

        public bool[] StringToBinary(string text)
        {
            List<bool> boolList = new List<bool>();
            foreach (char c in text)
            {
                string binaryString = Convert.ToString(c, 2).PadLeft(8, '0');
                foreach (char bit in binaryString)
                {
                    boolList.Add(bit == '1');
                }
            }
            return boolList.ToArray();
        }

        public string BinaryToString(bool[] binary)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < binary.Length; i += 8)
            {
                string byteString = new string(binary.Skip(i).Take(8).Select(b => b ? '1' : '0').ToArray());
                sb.Append((char)Convert.ToByte(byteString, 2));
            }
            return sb.ToString();
        }

        public float[] EncodeDataToAudio(bool[] data)
        {
            // Ensure data length is even (each QPSK symbol uses 2 bits)
            if (data.Length % 2 != 0)
            {
                data = data.Concat(new bool[] { false }).ToArray(); // Pad with 0 if odd
            }

            int samplesPerSymbol = SampleRate;
            float[] audioData = new float[data.Length / 2 * samplesPerSymbol];

            for (int i = 0; i < data.Length; i += 2)
            {
                // Determine the phase shift for this pair of bits
                int symbol = (data[i] ? 1 : 0) << 1 | (data[i + 1] ? 1 : 0); // Combine bits into symbol
                double phase = symbol switch
                {
                    0b00 => 0,
                    0b01 => Math.PI / 2,
                    0b10 => Math.PI,
                    0b11 => 3 * Math.PI / 2,
                    _ => 0 // Should never happen
                };

                // Generate the waveform for this symbol
                for (int j = 0; j < samplesPerSymbol; j++)
                {
                    double t = (double)j / SampleRate;
                    audioData[i / 2 * samplesPerSymbol + j] = (float)Math.Sin(2 * Math.PI * t + phase);
                }
            }

            return audioData;
        }


        public bool[] DecodeAudioToData(float[] audioData)
        {
            int samplesPerSymbol = (int)(SampleRate * SymbolDuration);
            List<bool> decodedBits = new List<bool>();

            for (int i = 0; i < audioData.Length; i += samplesPerSymbol)
            {
                float[] symbolData = audioData.Skip(i).Take(samplesPerSymbol).ToArray();
                double detectedPhase = DetectPhase(symbolData);

                int phaseIndex = Array.IndexOf(Phases, ClosestPhase(detectedPhase));
                decodedBits.Add((phaseIndex & 2) != 0); // First bit
                decodedBits.Add((phaseIndex & 1) != 0); // Second bit
            }

            return decodedBits.ToArray();
        }

        private double DetectPhase(float[] samples)
        {
            // Use in-phase (I) and quadrature (Q) components to detect phase
            double inPhase = 0, quadrature = 0;

            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / SampleRate;
                inPhase += samples[i] * Math.Cos(2 * Math.PI * CarrierFrequency * t);
                quadrature += samples[i] * Math.Sin(2 * Math.PI * CarrierFrequency * t);
            }

            return Math.Atan2(quadrature, inPhase);
        }

        private double ClosestPhase(double phase)
        {
            // Normalize phase to [0, 2π)
            phase = (phase + 2 * Math.PI) % (2 * Math.PI);
            return Phases.OrderBy(p => Math.Abs(p - phase)).First();
        }

        public string SaveAudioToFile(float[] audioData, string fileName)
        {
            using (var writer = new WaveFileWriter(fileName, new WaveFormat(SampleRate, 1)))
            {
                writer.WriteSamples(audioData, 0, audioData.Length);
            }
            return fileName;
        }

        public float[] LoadAudioFromFile(string fileName)
        {
            using (var reader = new AudioFileReader(fileName))
            {
                var audioData = new float[reader.Length / sizeof(float)];
                reader.Read(audioData, 0, audioData.Length);
                return audioData;
            }
        }
    }

}
