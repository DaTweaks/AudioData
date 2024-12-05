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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AudioData.DataControllers
{
    public class QPSK : DataControl
    {
        const double BPS = 80;
        const double BitDuration = 1.0 / BPS; // seconds
        const double CarrierFrequency = 1500;

        public override string GetName() => "QPSK";

        public override string GetDescription() => "Quadrature Shift Keying";

        public QPSK()
        {
            modulators.Add("00", Math.PI / 4);       
            modulators.Add("01", 3 * Math.PI / 4);   
            modulators.Add("10", 5 * Math.PI / 4);   
            modulators.Add("11", 7 * Math.PI / 4);   
        }

        private Dictionary<string, double> modulators = new Dictionary<string, double>();

        #region Audio

        public override float[] EncodeDataToAudio(bool[] data, int SampleRate, float noise = 0f)
        {
            data = GenerateStartHandshakeEncoded()
           .Concat(data)
           .Concat(GenerateEndHandshakeEncoded())
           .ToArray();

            int samplesPerBit = (int)(SampleRate * BitDuration);

            float[] audioData = new float[(data.Length / 2 * samplesPerBit)];

            List<double> frequecies = new List<double>();

            for (int i = 0; i < data.Length; i += 2)
            {
                string bitpair = (data[i] == true ? "1" : "0") + (data[i + 1] == true ? "1" : "0"); // Could make it a bit better than using strings here.

                frequecies.Add(CarrierFrequency * modulators[bitpair]);

                //Console.WriteLine("Modulating Frequency! it is at: " + bitpair);
            }

            for (int i = 0; i < frequecies.Count; i++) // This is really flawed, between frequencies it 
            {
                for (int j = 0; j < samplesPerBit; j++)
                {
                    double t = (double)j / SampleRate;
                    audioData[i * samplesPerBit + j] = (float)Math.Sin(2 * Math.PI * frequecies[i] * t);
                }
            }

            var list = PadArrayWithZeros(audioData, 50000);

            return AddNoise(list, noise); // Should give like a 50% chance of it coming through completely fine. Will rework encoder.
        }

        public override bool[] DecodeAudioToData(float[] audioData, int SampleRate)
        {
            // Calculate where the bits will be placed.
            int samplesPerBit = (int)(SampleRate * BitDuration);

            string decodedStringData = "";
            for (int i = 0; i < audioData.Length; i += samplesPerBit)
            {
                float[] bitData = audioData.Skip(i).Take(samplesPerBit).ToArray();
                decodedStringData += DetectFrequency(bitData, SampleRate);
            }
            //Console.WriteLine("DeModulatedBits: " + decodedStringData);
            return RemoveBeforeHandShake(RemoveAfterHandShake(Helpers.prettyStringToBoolArray(decodedStringData)));
        }

        private string DetectFrequency(float[] samples, int SampleRate)
        {
            string bestGuess = "00";
            double power = 0.0;

            //Console.WriteLine("Detecting Frequency!");

            foreach(var kvp in modulators)
            {
                var tempPower = Goertzel(samples, kvp.Value*CarrierFrequency, SampleRate);
                //Console.WriteLine(kvp.Key+" Power: "+tempPower);
                if (tempPower > power)
                {
                    bestGuess = kvp.Key;
                    power = tempPower;
                }
            }
            //Console.WriteLine(bestGuess+" has won!");
            return bestGuess;
        }

        #endregion
    }
}
