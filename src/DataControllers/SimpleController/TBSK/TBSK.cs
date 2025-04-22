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

using AudioData.Encoders.Encoding;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.types;
using jp.nyatla.tbaskmodem;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Encoder = AudioData.Encoders.Encoding.Encoder;

namespace AudioData.DataControllers.SimpleController
{
    public class TBSK : SimpleDataController
    {
        private TraitTone tone; //    # SSFM DPSK
        private TbskDemodulator demod;
        private TbskModulator mod;

        private int speed = 10;

        public TBSK()
        {
            tone = TbskTone.CreateXPskSin(speed, speed).Mul(1);
            demod = new TbskDemodulator(tone);
            mod = new TbskModulator(tone);
        }

        public override float[] EncodeDataToAudio(bool[] data, int SampleRate, float noise = 0)
        {
            IList<double> src_pcm = mod.Modulate(Encoder.BoolsToBytes(data)).ToList();

            var audioData = PadArrayWithZeros(src_pcm.Select(x => (float)x).ToArray(), 5000);

            if (noise > 0f)
            {
                audioData = AddNoise(audioData, noise);
            }

            return audioData;
        }

        public override int GetBitsPerSecond()
        {
            return 8820;
        }

        public override string GetDescription()
        {
            return "Tone-Based Shift Keying";
        }

        public override string GetName()
        {
            return "TBSK";
        }

        public override int GetOptimalDetectionTone()
        {
            return 100;
        }

        protected override bool[] DecodeAudio(float[] audioData, int sampleRate)
        {
            IList<double> pcm = audioData.Select(f => (double)f).ToList();
            var ret = demod.DemodulateAsBytes(pcm);

            if(ret == null)
            {
                return new bool[0];
            }

            return Encoder.BytesToBools(ret.ToArray());
        }
    }
}
