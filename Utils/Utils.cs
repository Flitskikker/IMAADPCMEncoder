using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ADPCMEncoder
{
    public class Utils
    {
        private static Dictionary<string, UInt32> sampleHashes = new Dictionary<string, UInt32>();
        private static Dictionary<UInt32, string> sampleNames = new Dictionary<UInt32, string>();
        public static string sessionID = "";

        public static void loadDictionaries()
        {
            StreamReader reader;
            string[] split;            
            string[] test = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
            reader = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("IVAUDToolBox.Data.NamesHashes.dat"));
            while (!reader.EndOfStream){                
                split = reader.ReadLine().Split(Convert.ToChar("="));
                sampleHashes.Add(split[1], Convert.ToUInt32(split[0]));
                sampleNames.Add(Convert.ToUInt32(split[0]), split[1]);
            }
            reader.Close();
        }

        public static void loadSessionID()
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            sessionID = encodeSHA1(rnd.Next(0, 100).ToString()).Substring(DateTime.Now.Hour, 4);
        }

        public static string binaryString(int input, int width)
        {
            string temp = Convert.ToString(input, 2);
            while (temp.Length < width) temp = "0" + temp;
            return temp;
        }

        public static int getPaddedSize(int input)
        {
            return (int)(Math.Ceiling(input / 2048f) * 2048f);
        }

        public static string getDurationString(int samples, int sampleRate)
        {
            try { 
                TimeSpan ts = new TimeSpan(0, 0, samples / sampleRate);
                return ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00"); 
            } catch { return "XX:XX"; }
        }

        public static UInt16 swapUInt16(UInt16 inValue)
        {
            byte[] byteArray = BitConverter.GetBytes(inValue);
            Array.Reverse(byteArray);
            return BitConverter.ToUInt16(byteArray, 0);
        }

        public static UInt32 swapUInt32(UInt32 inValue)
        {
            byte[] byteArray = BitConverter.GetBytes(inValue);
            Array.Reverse(byteArray);
            return BitConverter.ToUInt32(byteArray, 0);
        }

        public static string getSampleName(UInt32 val)
        {
            try{
                string temp = "0x" + val.ToString("X");
                sampleNames.TryGetValue(val, out temp);
                if (temp == null) return "0x" + val.ToString("X");
                return temp;
            }catch{
                return "0x" + val.ToString("X");
            }
        }

        public static UInt32 getSampleHash(string val)
        {
            try{
                UInt32 temp = 0;
                sampleHashes.TryGetValue(val, out temp);
                if (temp == null) return 0;
                return temp;
            }catch{
                return 0;
            }
        }

        public static string encodeSHA1(string data) { 
            return BitConverter.ToString(SHA1Managed.Create().ComputeHash(Encoding.Default.GetBytes(data))).Replace("-", ""); 
        }

        public static bool isNumeric(string val)
        {
            Double result;
            return Double.TryParse(val, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out result);
        }
    }
}
