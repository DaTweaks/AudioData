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

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using AudioData;
using NAudio;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using System.IO;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    const int SampleRate = 44100; // Hz
    const double BPS = 80;
    const double BitDuration = 1.0 / BPS; // seconds
    const double Frequency0 = 1000; // Frequency for binary 0
    const double Frequency1 = 3000; // Frequency for binary 1

    //static void Main(string[] args) // EXAMPLE USAGE OF HAMMING ENCODER!
    //{
    //    string input = "10110110";
    //    Console.WriteLine(input);

    //    var encoded = MessageEncoder.GroupDecode(Helpers.prettyStringToBoolArray(input), input.Length);

    //    Console.WriteLine(Helpers.boolArrayToPrettyString(encoded));
    //    MessageEncoder.MixinRandomError(encoded, 1);

    //    Console.WriteLine(Helpers.boolArrayToPrettyString(MessageEncoder.GroupDecode(encoded, input.Length)));
    //}

    public static List<bool> tries = new List<bool>();

    public static void updateTries()
    {
        string lastoutput = "";
        while (true)
        {
            Thread.Sleep(10);
            Console.SetCursorPosition(0, 0);

            int succeeded = 0;

            for(int i = 0; i < tries.Count; i++)
            {
                if (tries[i])
                {
                    succeeded++;
                }
            }

            string thisMessage = $"Tried this many times: {tries.Count} succeeded this many times: {succeeded} SuccessPercent: {((double)succeeded / (double)tries.Count) * 100}%";

            if(thisMessage.Length < lastoutput.Length)
            {
                Console.Clear();
            }

            lastoutput = thisMessage;

            Console.Write(thisMessage);
        }
    }



    static void Main(string[] args)
    {
        Thread thread = new Thread(updateTries);
        var datacontrol = new DataController();
        thread.Start(); 
        while (true)
        {
            string encryptionKey = "hemligtLösenord";

            string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

            var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

            var binary = datacontrol.StringToBinary(encryptedData);

            binary = MessageEncoder.GroupEncode(binary, 8);

            var audioData = datacontrol.EncodeDataToAudio(binary);

            var editedBinary = datacontrol.DecodeAudioToData(audioData);

            var dataConvertedData = AESEncryption.DecryptString(datacontrol.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

            tries.Add(dataConvertedData == data);
        }

    }

 
}
