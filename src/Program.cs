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

using AudioData;
using AudioData.DataControllers;
using NAudio.Wave;
using Spectrogram;
using System.Drawing.Imaging;
using System.Drawing;

class Program
{
    static void Main(string[] args)
    {
        SingleTest(new QPSK(), 7f);
    }

    #region HammingEncoder

    static void TestHamming() // EXAMPLE USAGE OF HAMMING ENCODER!
    {
        string input = "10110110"; // NOTE: This only works with multiples of 4 and 8. otherwise it doesnt work.
        Console.WriteLine(input);

        var encoded = MessageEncoder.GroupDecode(Helpers.prettyStringToBoolArray(input), 8);

        Console.WriteLine(Helpers.boolArrayToPrettyString(encoded));
        MessageEncoder.MixinRandomError(encoded, 1);

        Console.WriteLine(Helpers.boolArrayToPrettyString(MessageEncoder.GroupDecode(encoded, 8)));
    }

    #endregion

    public static Dictionary<float, float> totalTries = new Dictionary<float, float>();

    public static List<bool> tries = new List<bool>();


    // Mass tests for when i want to test the capabilites of it.
    public static void UnitTestModulation(DataControl dataControl, float startingNoise, int tryCount, int totalTryCount)
    {
        totalTryCount++;
        int SampleRate = 192000;
        Thread thread = new Thread(updateTries);
        CreateFolder(dataControl.GetName());
        thread.Start();
        while (totalTries.Count <= totalTryCount)
        {
            string encryptionKey = "hemligtLösenord";

            string data = "TESTING TESTING, WHAT IS UP? HOW ARE YOU? YES YES YES";

            var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

            var binary = dataControl.StringToBinary(encryptedData);

            if (tries.Count >= tryCount)
            {
                Console.SetCursorPosition(0, 1);
                Console.Write($"Complete with noise level: {startingNoise} Percent Calculated: {trypercent()}%                ");
                totalTries.Add(startingNoise, (float)trypercent());
                tries.Clear();
                startingNoise += 0.1f;
            }

            binary = MessageEncoder.GroupEncode(binary, 8);

            var audioData = dataControl.EncodeDataToAudio(binary, SampleRate, startingNoise);

            var editedBinary = dataControl.DecodeAudioToData(audioData, SampleRate);

            var dataConvertedData = AESEncryption.DecryptString(dataControl.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

            tries.Add(dataConvertedData == data);
        }

        string filePath = dataControl.GetName()+"/Data.txt";


        // Write it into the data file.
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var kvp in totalTries)
            {
                writer.WriteLine($"NOISE: {kvp.Key}     -   {kvp.Value}%");
            }
        }

        Console.WriteLine("DONE!");
    }


    // For single tests, when you dont want to loop it a 1000 times.
    public static void SingleTest(DataControl controller, float noise)
    {
        CreateFolder(controller.GetName());
        int SampleRate = 192000;
        string encryptionKey = "HemligtLösenord";

        string data = "SINGLE TEST! DOES THIS WORK???";

        var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

        var binary = controller.StringToBinary(data);

        binary = MessageEncoder.GroupEncode(binary, 8);

        var audioData = controller.EncodeDataToAudio(binary, SampleRate, noise);

        var filespace = controller.SaveAudioToFile(audioData, controller.GetName() + "/Audio.wav", SampleRate);

        GenerateSpectrogram(filespace, SampleRate, controller.GetName()+ "/Spectrogram.png");

        //controller.PlayAudio(filespace);

        var editedBinary = controller.DecodeAudioToData(audioData, SampleRate);
        
        Console.WriteLine("ModulatedBits:   "+ Helpers.boolArrayToPrettyString(binary));

        var dataConvertedData = AESEncryption.DecryptString(controller.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

        Console.WriteLine("Original Message: "+data);
        Console.WriteLine("Demodulated Message: "+dataConvertedData);

        Console.WriteLine("Duration: "+controller.GetWavFileDuration(filespace));

        tries.Add(dataConvertedData == data);
    }

    public static void updateTries()
    {
        while (true)
        {
            Thread.Sleep(10);
            Console.SetCursorPosition(0, 0);
            Console.Write($"Tried this many times: {tries.Count} succeeded this many times: {GetSuccess()} SuccessPercent: {trypercent()}%                  ");
        }
    }

    public static double trypercent()
    {
        int success = GetSuccess();
        if (success != 0 || tries.Count != 0)
            return Math.Round(((double)success / (double)tries.Count) * 100, 4);

        return 0;
    }

    public static int GetSuccess()
    {
        int succeeded = 0;

        for (int i = 0; i < tries.Count; i++)
        {
            if (tries[i])
            {
                succeeded++;
            }
        }

        return succeeded;
    }

    #region Utils

    public static void CreateFolder(string path)
    {
        Directory.CreateDirectory(path);
    }

    public static void GenerateSpectrogram(string audioFile, int SampleRate, string name) 
    {
        (double[] audio, int sampleRate) = ReadMono(audioFile);
        var sg = new SpectrogramGenerator(sampleRate, fftSize: 4096, stepSize: 250, maxFreq: 8000);
        sg.Add(audio);
        sg.SaveImage(name);
    }

    static (double[] audio, int sampleRate) ReadMono(string filePath, double multiplier = 16_000)
    {
        using var afr = new NAudio.Wave.AudioFileReader(filePath);
        int sampleRate = afr.WaveFormat.SampleRate;
        int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
        int sampleCount = (int)(afr.Length / bytesPerSample);
        int channelCount = afr.WaveFormat.Channels;
        var audio = new List<double>(sampleCount);
        var buffer = new float[sampleRate * channelCount];
        int samplesRead = 0;
        while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
            audio.AddRange(buffer.Take(samplesRead).Select(x => x * multiplier));
        return (audio.ToArray(), sampleRate);
    }

    #endregion
}
