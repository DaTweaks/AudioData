//	Copyright (c) 2024 David Hornemark
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================

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
        private const double BitDuration = 0.1; // Example value, adjust as needed
        private const double CarrierFrequency = 1000; // Example value, adjust as needed
        private static readonly double[] Phases = { 0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 };

        public override bool[] DecodeAudioToData(string fileName, int SampleRate)
        {
            // Load audio data from file
            float[] audioData = LoadAudioFromFile(fileName);
            return DecodeAudioToData(audioData, SampleRate);
        }

        public override bool[] DecodeAudioToData(float[] audioData, int SampleRate)
        {
            int samplesPerSymbol = (int)(SampleRate * BitDuration);
            List<bool> dataList = new List<bool>();

            for (int i = 0; i < audioData.Length; i += samplesPerSymbol)
            {
                float[] symbol = audioData.Skip(i).Take(samplesPerSymbol).ToArray();
                double phase = Math.Atan2(symbol.Sum(y => Math.Sin(2 * Math.PI * CarrierFrequency * (y / SampleRate))),
                                           symbol.Sum(y => Math.Cos(2 * Math.PI * CarrierFrequency * (y / SampleRate))));

                int phaseIndex = Array.IndexOf(Phases, Phases.OrderBy(p => Math.Abs(p - phase)).First());
                dataList.AddRange(Convert.ToString(phaseIndex, 2).PadLeft(2, '0').Select(bit => bit == '1'));
            }

            return dataList.ToArray();
        }

        public override float[] EncodeDataToAudio(bool[] data, float noise, int SampleRate)
        {
            int samplesPerSymbol = (int)(SampleRate * BitDuration);
            List<float> audioData = new List<float>();

            for (int i = 0; i < data.Length; i += 2)
            {
                string bitPair = new string(data.Skip(i).Take(2).Select(bit => bit ? '1' : '0').ToArray());
                int phaseIndex = Convert.ToInt32(bitPair, 2);
                double phase = Phases[phaseIndex];

                for (int j = 0; j < samplesPerSymbol; j++)
                {
                    double t = (double)j / SampleRate;
                    audioData.Add((float)(Math.Cos(2 * Math.PI * CarrierFrequency * t + phase)));
                }
            }

            // Convert to float array and add noise
            float[] audioArray = audioData.ToArray();
            return AddNoise(audioArray, noise);
        }
    }
}
