using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dev2Sinhala
{
    class Dev2Sinhala
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2 || args.Length > 3)
                {
                    PrintUsage();
                    return;
                }

                // validate args
                FileInfo fi = new FileInfo(args[0]);
                if (fi.Exists == false)
                {
                    Console.WriteLine("Input file does not exist");
                    return;
                }

                DirectoryInfo di = new DirectoryInfo(args[1]);
                if (di.Exists == false)
                {
                    Console.Write("Output directory does not exist. Would you like to create it?[y/n] ");
                    if (Console.ReadLine().ToLower().Substring(0, 1) == "y")
                    {
                        di.Create();
                    }
                    else
                    {
                        return;
                    }
                }

                Dev2Sinhala d2 = new Dev2Sinhala();
                d2.InputFilePath = args[0];
                d2.OutputFilePath = di.FullName + "\\" + fi.Name;
                d2.Convert();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Unicode Devanagari to Unicode Sinhala, Pali characters conversion");
            Console.WriteLine("syntax:");
            Console.WriteLine("dev2sin input [output]");
        }
        // end static methods


        private Hashtable dev2Sinhala;

        public Dev2Sinhala()
        {
            dev2Sinhala = new Hashtable();

            // velar stops
            dev2Sinhala['\x0915'] = '\x0D9A'; // ka
            dev2Sinhala['\x0916'] = '\x0D9B'; // kha
            dev2Sinhala['\x0917'] = '\x0D9C'; // ga
            dev2Sinhala['\x0918'] = '\x0D9D'; // gha
            dev2Sinhala['\x0919'] = '\x0D9E'; // n overdot a
            
            // palatal stops
            dev2Sinhala['\x091A'] = '\x0DA0'; // ca
            dev2Sinhala['\x091B'] = '\x0DA1'; // cha
            dev2Sinhala['\x091C'] = '\x0DA2'; // ja
            dev2Sinhala['\x091D'] = '\x0DA3'; // jha
            dev2Sinhala['\x091E'] = '\x0DA4'; // ña

            // retroflex stops
            dev2Sinhala['\x091F'] = '\x0DA7'; // t underdot a
            dev2Sinhala['\x0920'] = '\x0DA8'; // t underdot ha
            dev2Sinhala['\x0921'] = '\x0DA9'; // d underdot a
            dev2Sinhala['\x0922'] = '\x0DAA'; // d underdot ha
            dev2Sinhala['\x0923'] = '\x0DAB'; // n underdot a

            // dental stops
            dev2Sinhala['\x0924'] = '\x0DAD'; // ta
            dev2Sinhala['\x0925'] = '\x0DAE'; // tha
            dev2Sinhala['\x0926'] = '\x0DAF'; // da
            dev2Sinhala['\x0927'] = '\x0DB0'; // dha
            dev2Sinhala['\x0928'] = '\x0DB1'; // na

            // labial stops
            dev2Sinhala['\x092A'] = '\x0DB4'; // pa
            dev2Sinhala['\x092B'] = '\x0DB5'; // pha
            dev2Sinhala['\x092C'] = '\x0DB6'; // ba
            dev2Sinhala['\x092D'] = '\x0DB7'; // bha
            dev2Sinhala['\x092E'] = '\x0DB8'; // ma

            // liquids, fricatives, etc.
            dev2Sinhala['\x092F'] = '\x0DBA'; // ya
            dev2Sinhala['\x0930'] = '\x0DBB'; // ra
            dev2Sinhala['\x0932'] = '\x0DBD'; // la
            dev2Sinhala['\x0935'] = '\x0DC0'; // va
            dev2Sinhala['\x0938'] = '\x0DC3'; // sa
            dev2Sinhala['\x0939'] = '\x0DC4'; // ha
            dev2Sinhala['\x0933'] = '\x0DC5'; // l underdot a

            // independent vowels
            dev2Sinhala['\x0905'] = '\x0D85'; // a
            dev2Sinhala['\x0906'] = '\x0D86'; // aa
            dev2Sinhala['\x0907'] = '\x0D89'; // i
            dev2Sinhala['\x0908'] = '\x0D8A'; // ii
            dev2Sinhala['\x0909'] = '\x0D8B'; // u
            dev2Sinhala['\x090A'] = '\x0D8C'; // uu
            dev2Sinhala['\x090F'] = '\x0D91'; // e
            dev2Sinhala['\x0913'] = '\x0D94'; // o

            // dependent vowel signs
            dev2Sinhala['\x093E'] = '\x0DCF'; // aa
            dev2Sinhala['\x093F'] = '\x0DD2'; // i
            dev2Sinhala['\x0940'] = '\x0DD3'; // ii
            dev2Sinhala['\x0941'] = '\x0DD4'; // u
            dev2Sinhala['\x0942'] = '\x0DD6'; // uu
            dev2Sinhala['\x0947'] = '\x0DD9'; // e
            dev2Sinhala['\x094B'] = '\x0DDC'; // o

            // numerals
            dev2Sinhala['\x0966'] = '0';
            dev2Sinhala['\x0967'] = '1';
            dev2Sinhala['\x0968'] = '2';
            dev2Sinhala['\x0969'] = '3';
            dev2Sinhala['\x096A'] = '4';
            dev2Sinhala['\x096B'] = '5';
            dev2Sinhala['\x096C'] = '6';
            dev2Sinhala['\x096D'] = '7';
            dev2Sinhala['\x096E'] = '8';
            dev2Sinhala['\x096F'] = '9';

            // other
            dev2Sinhala['\x0964'] = '.'; // danda -> period
            dev2Sinhala['\x0902'] = '\x0D82'; // niggahita
            dev2Sinhala['\x094D'] = "\x200D\x0DCA"; // virama -> ZWJ + Sinhala virama
            dev2Sinhala['\x200C'] = ""; // ZWNJ (ignore)
            dev2Sinhala['\x200D'] = ""; // ZWJ (ignore)
        }

        public string InputFilePath
        {
            get { return inputFilePath; }
            set { inputFilePath = value; }
        }
        private string inputFilePath;

        public string OutputFilePath
        {
            get { return outputFilePath; }
            set { outputFilePath = value; }
        }
        private string outputFilePath;


        public void Convert()
        {
            StreamReader sr = new StreamReader(InputFilePath);
            string devStr = sr.ReadToEnd();

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.BigEndianUnicode);

            // zap the double danda around "namo tassa..."
            devStr = devStr.Replace("\x0964\x0964", "");

            // change name of stylesheet for Sinhala
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-sinh.xsl");

            char[] dev = devStr.ToCharArray();

            foreach (char c in dev)
            {
                if (dev2Sinhala.ContainsKey(c))
                    sw.Write(dev2Sinhala[c]);
                else
                    sw.Write(c);
            }

            sw.Flush();
            sw.Close();
            sr.Close();
        }
    }
}
