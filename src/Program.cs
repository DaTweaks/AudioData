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
using Spectrogram;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Do you want to test FSK or QPSK?");

        string input = Console.ReadLine().Trim().ToUpper();

        switch (input)
        {
            case "FSK":
                UnitTestModulation(new FSK(), 0f, 100, 75);
                break;
            case "QPSK":
                UnitTestModulation(new QPSK(), 0f, 100, 75);
                break;
        }
    }

    #region HammingEncoder

    static void TestHamming() // EXAMPLE USAGE OF HAMMING ENCODER!
    {
        string input = "10110110"; // NOTE: This only works with multiples of 4 and 8. otherwise it doesnt work.
        Console.WriteLine(input);

        var encoded = MessageEncoder.GroupDecode(Helpers.PrettyStringToBoolArray(input), 8);

        Console.WriteLine(Helpers.BoolArrayToPrettyString(encoded));
        MessageEncoder.MixinRandomError(encoded, 1);

        Console.WriteLine(Helpers.BoolArrayToPrettyString(MessageEncoder.GroupDecode(encoded, 8)));
    }

    #endregion

    public static Dictionary<float, float> totalTries = new Dictionary<float, float>();

    public static List<bool> tries = new List<bool>();


    // Mass tests for when i want to test the capabilites of it.
    public static void UnitTestModulation(DataControl dataControl, float startingNoise, int tryCount, int totalTryCount)
    {
        totalTryCount++;
        int SampleRate = 192000;
        Thread thread = new Thread(UpdateTries);
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
                Console.Write($"Complete with noise level: {startingNoise} Percent Calculated: {TryPercent()}%                ");
                totalTries.Add(startingNoise, (float)TryPercent());
                tries.Clear();
                startingNoise += 0.1f;
            }

            binary = MessageEncoder.GroupEncode(binary, 8);

            var audioData = dataControl.EncodeDataToAudio(binary, SampleRate, startingNoise);

            // Induce a offset error.
            audioData = audioData.Skip(1000).ToArray();

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
    public static void SingleTestByte(DataControl controller, float noise)
    {
        CreateFolder(controller.GetName());
        int SampleRate = 192000;

        var message = new Message(1000, "YES! WOW!");
      
        var binary = controller.SerializeToBinary(message); // First convert the string into binary.

        binary = MessageEncoder.GroupEncode(binary, 8); // Add hamming code correction to the bits

        var audioData = controller.EncodeDataToAudio(binary, SampleRate, noise); // Encode the bits to audio

        var filespace = controller.SaveAudioToFile(audioData, controller.GetName() + "/Audio.wav", SampleRate);

        GenerateSpectrogram(filespace, SampleRate, controller.GetName()+ "/Spectrogram.png");

        controller.PlayAudio(filespace);

        var editedBinary = controller.DecodeAudioToData(audioData, SampleRate); // Decode the audio to bits again.
        
        Console.WriteLine("ModulatedBits:   "+ Helpers.BoolArrayToPrettyString(binary));

        var dataConvertedData = controller.DeserializeFromBinary<Message>(MessageEncoder.GroupDecode(editedBinary, 8)); // Decode it from hamming and then decode back into a string.

        Console.WriteLine($"Original Message: Name: {message.Name} id: {message.Number}");
        Console.WriteLine($"Demodulated Message: Name: {dataConvertedData.Name} id: {dataConvertedData.Number}");

        Console.WriteLine("Duration: "+controller.GetWavFileDuration(filespace));

        tries.Add(dataConvertedData == message);
    }

    // For single tests, when you dont want to loop it a 1000 times.
    public static void SingleTestString(DataControl controller, float noise, string data = "SINGLE TEST! DOES THIS WORK???")
    {
        CreateFolder(controller.GetName());
        int SampleRate = 192000;
        string encryptionKey = "HemligtLösenord";

        var encryptedData = AESEncryption.EncryptString(data, encryptionKey);

        var binary = controller.StringToBinary(encryptedData);

        binary = MessageEncoder.GroupEncode(binary, 8);

        var audioData = controller.EncodeDataToAudio(binary, SampleRate, noise);

        var filespace = controller.SaveAudioToFile(audioData, controller.GetName() + "/Audio.wav", SampleRate);

        GenerateSpectrogram(filespace, SampleRate, controller.GetName()+ "/Spectrogram.png");

        controller.PlayAudio(filespace);

        var editedBinary = controller.DecodeAudioToData(audioData, SampleRate);

        Console.WriteLine("ModulatedBits:   " + Helpers.BoolArrayToPrettyString(binary));

        var dataConvertedData = AESEncryption.DecryptString(controller.BinaryToString(MessageEncoder.GroupDecode(editedBinary, 8)), encryptionKey);

        Console.WriteLine("Original Message: " + data);
        Console.WriteLine("Demodulated Message: " + dataConvertedData);

        Console.WriteLine("Duration: " + controller.GetWavFileDuration(filespace));

        tries.Add(dataConvertedData == data);
    }

    public static void UpdateTries()
    {
        while (true)
        {
            Thread.Sleep(10);
            Console.SetCursorPosition(0, 0);
            Console.Write($"Tried this many times: {tries.Count} succeeded this many times: {GetSuccess()} SuccessPercent: {TryPercent()}%                  ");
        }
    }

    public static double TryPercent()
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
        var sg = new SpectrogramGenerator(sampleRate, fftSize: 4096, stepSize: 250, maxFreq: 10000);
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

    public class Message
    {
        public int Number { get; set; }
        public string Name { get; set; }

        public Message(int number, string name)
        {
            Number = number;
            Name = name;
        }
    }
    #endregion
}
