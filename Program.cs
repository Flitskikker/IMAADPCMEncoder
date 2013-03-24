using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using WAV;
using System.Collections;

namespace ADPCMEncoder
{
    class Program
    {
        static void Main(string[] args)
        {
            //Debug test
            //args = (@"E:\Mijn documenten\Visual Studio 2010\ADPCMEncoder\~24000HZTEST.wav|test").Split(Convert.ToChar("|"));
            //args = (@"E:\Mijn documenten\Visual Studio 2010\ADPCMEncoder\TestFiles\LoadingSNOW.wav|test").Split(Convert.ToChar("|"));
            
            //Print header
            Console.WriteLine("=================");
            Console.WriteLine("IMA ADPCM Encoder");
            Console.WriteLine(" by Flitskikker");
            Console.WriteLine("=================");
            Console.WriteLine("");

            //Check args
            if(args.Length == 0){
                //No args
                Console.WriteLine("ERROR: No command line arguments passed. Press any key to exit...");
                Console.ReadLine();
                Environment.Exit(1);               
            }

            //Check file
            if(!File.Exists(args[0])){
                //File not found
                Console.WriteLine("ERROR: File \"" + args[0] + "\" not found. Press any key to exit...");
                Console.ReadLine();
                Environment.Exit(2);
            }

            //Print file
            Console.WriteLine("File: " + args[0]);
            Console.WriteLine("");

            //Load file
            WAVFile wav = new WAVFile();
            wav.Open(args[0], WAVFile.WAVFileMode.READ);

            //Show WAV info
            Console.WriteLine("WAV Info:");
            Console.WriteLine("\tBits per sample:\t" + wav.BitsPerSample.ToString() + " bits");
            Console.WriteLine("\tBytes per sample:\t" + wav.BytesPerSample.ToString());
            Console.WriteLine("\tBytes per second:\t" + wav.BytesPerSec.ToString());
            Console.WriteLine("\tData size:\t\t" + wav.DataSizeBytes.ToString() + " bytes");
            Console.WriteLine("\tDuration:\t\t" + Math.Round(Convert.ToDecimal((double)wav.NumSamples / (double)wav.SampleRateHz), 2).ToString() + " seconds (" + Utils.getDurationString(wav.NumSamples, wav.SampleRateHz) + ")");
            Console.WriteLine("\tEncoding type:\t\t" + wav.EncodingType.ToString());
            Console.WriteLine("\tFile size:\t\t" + wav.FileSizeBytes.ToString() + " bytes");
            Console.WriteLine("\t# of channels:\t\t" + wav.NumChannels.ToString());
            Console.WriteLine("\t# of samples:\t\t" + wav.NumSamples.ToString());
            Console.WriteLine("\tRIFF type:\t\t" + wav.RIFFTypeString);
            Console.WriteLine("\tSample Rate:\t\t" + wav.SampleRateHz.ToString() + " Hz");
            Console.WriteLine("\tWAV header:\t\t" + wav.WAVHeaderString);
            Console.WriteLine("");

            //Check bits
            if(wav.BitsPerSample != 16){
                //Not 16 bits
                Console.WriteLine("ERROR: WAV should have 16 bits per sample. Press any key to exit...");
                Console.ReadLine();
                Environment.Exit(1);               
            }

            //Check bytes per sample
            if(wav.BytesPerSample / wav.NumChannels != 2){
                //Not 2 bytes
                Console.WriteLine("ERROR: WAV should have (wav.NumChannels * 2) bytes per sample. Press any key to exit...");
                Console.ReadLine();
                Environment.Exit(1);               
            }
                        
            //Print message
            Console.WriteLine("Ready to go! Press any key when ready...");

            //Wait for input
            Console.ReadLine();

            if(wav.NumChannels > 1){
                //Split channels
                Console.WriteLine("");
                Console.WriteLine("Saving separate channels...");
                Console.WriteLine("");

                List<WAVFile> wavs = new List<WAVFile>();

                for (int c = 0; c < wav.NumChannels; c++) {
                    wavs.Add(new WAVFile());
                    wavs[c].Create(Path.GetDirectoryName(args[0]) + "\\" + Path.GetFileNameWithoutExtension(args[0]) + "_" + (c + 1).ToString() + ".wav", false, wav.SampleRateHz, wav.BitsPerSample, true);
                }

                int numSamplesCorrected = ((wav.DataSizeBytes - 8) / wav.BytesPerSample);
                                               
                //for (long i = 0; i < wav.NumSamples / wav.NumChannels; i++) {
                for (long i = 0; i < numSamplesCorrected; i++) {
                    for (int c = 0; c < wav.NumChannels; c++) {
                        wavs[c].AddSample_16bit(wav.GetNextSampleAs16Bit());              
                    }                    
                }

                for (int c = 0; c < wav.NumChannels; c++) {
                    wavs[c].Close();
                }
            }

            //Encode
            Console.WriteLine("");
            Console.WriteLine("Encoding file(s)...");
            Console.WriteLine("");

            IMAADPCM.ADPCMState state = new IMAADPCM.ADPCMState();
            WAVFile cwav = new WAVFile();
            int nc = wav.NumChannels;
            
            for (int c = 0; c < wav.NumChannels; c++) {
                Console.WriteLine("Encoding file: " + Path.GetDirectoryName(args[0]) + "\\" + Path.GetFileNameWithoutExtension(args[0]) + "_" + (c + 1).ToString() + ".bin");

                MemoryStream ms = new MemoryStream();
                cwav = new WAVFile();
               
                // Open splitted WAV
                if(wav.NumChannels > 1){
                    cwav.Open(Path.GetDirectoryName(args[0]) + "\\" + Path.GetFileNameWithoutExtension(args[0]) + "_" + (c + 1).ToString() + ".wav", WAVFile.WAVFileMode.READ_WRITE);                    
                }else{
                    cwav = wav;
                    //wav.Close();
                    //cwav.Open(args[0], WAVFile.WAVFileMode.READ_WRITE); 
                }
                
                byte[] bytes = new byte[2];
                int loopValue = ((cwav.DataSizeBytes - 8) / cwav.BytesPerSample);

                //Actual encode
                for (long i = 0; i < loopValue / 2; i++) {
                    bytes[0] = IMAADPCM.encodeADPCM(cwav.GetNextSampleAs16Bit(), ref state);
                    bytes[1] = IMAADPCM.encodeADPCM(cwav.GetNextSampleAs16Bit(), ref state);
                    ms.Write(BitConverter.GetBytes(Convert.ToInt32(Utils.binaryString(bytes[1], 4) + Utils.binaryString(bytes[0], 4), 2)), 0, 1);
                }

                //Get WAV data
                byte[] dataWAV = new byte[ms.Length];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(dataWAV, 0, (int)ms.Length);
                ms.Close();

                //Create file
                FileStream fs = new FileStream(Path.GetDirectoryName(args[0]) + "\\" + Path.GetFileNameWithoutExtension(args[0]) + "_" + (c + 1).ToString() + ".bin", FileMode.Create, FileAccess.ReadWrite);

                //Write sample
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(dataWAV, 0, dataWAV.Length);                                

                //Close
                cwav.Close();
                ms.Close();
                fs.Close();

                if (nc == 1) break;
            }

            //Close WAV file
            wav.Close();

            //Print message
            Console.WriteLine("");
            Console.WriteLine("Done! Press any key to exit...");
            
            //Wait for input
            Console.ReadLine();

            //Exit
            Environment.Exit(0);
        }
    }
}
