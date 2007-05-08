using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dev2Myanmar
{
    class Dev2Myanmar
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

                Dev2Myanmar d2t = new Dev2Myanmar();
                d2t.InputFilePath = args[0];
                d2t.OutputFilePath = di.FullName + "\\" + fi.Name;
                d2t.Convert();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Unicode Devanagari to Unicode Myanmar, Pali characters conversion");
            Console.WriteLine("syntax:");
            Console.WriteLine("dev2mya input [output]");
        }
        // end static methods


        private Hashtable dev2Myanmar;

        public Dev2Myanmar()
        {
            dev2Myanmar = new Hashtable();

            // velar stops
            dev2Myanmar['\x0915'] = '\x1000'; // ka
            dev2Myanmar['\x0916'] = '\x1001'; // kha
            dev2Myanmar['\x0917'] = '\x1002'; // ga
            dev2Myanmar['\x0918'] = '\x1003'; // gha
            dev2Myanmar['\x0919'] = '\x1004'; // n overdot a
            
            // palatal stops
            dev2Myanmar['\x091A'] = '\x1005'; // ca
            dev2Myanmar['\x091B'] = '\x1006'; // cha
            dev2Myanmar['\x091C'] = '\x1007'; // ja
            dev2Myanmar['\x091D'] = '\x1008'; // jha
            dev2Myanmar['\x091E'] = '\x1009'; // ña

            // retroflex stops
            dev2Myanmar['\x091F'] = '\x100B'; // t underdot a
            dev2Myanmar['\x0920'] = '\x100C'; // t underdot ha
            dev2Myanmar['\x0921'] = '\x100D'; // d underdot a
            dev2Myanmar['\x0922'] = '\x100E'; // d underdot ha
            dev2Myanmar['\x0923'] = '\x100F'; // n underdot a

            // dental stops
            dev2Myanmar['\x0924'] = '\x1010'; // ta
            dev2Myanmar['\x0925'] = '\x1011'; // tha
            dev2Myanmar['\x0926'] = '\x1012'; // da
            dev2Myanmar['\x0927'] = '\x1013'; // dha
            dev2Myanmar['\x0928'] = '\x1014'; // na

            // labial stops
            dev2Myanmar['\x092A'] = '\x1015'; // pa
            dev2Myanmar['\x092B'] = '\x1016'; // pha
            dev2Myanmar['\x092C'] = '\x1017'; // ba
            dev2Myanmar['\x092D'] = '\x1018'; // bha
            dev2Myanmar['\x092E'] = '\x1019'; // ma

            // liquids, fricatives, etc.
            dev2Myanmar['\x092F'] = '\x101A'; // ya
            dev2Myanmar['\x0930'] = '\x101B'; // ra
            dev2Myanmar['\x0932'] = '\x101C'; // la
            dev2Myanmar['\x0935'] = '\x101D'; // va
            dev2Myanmar['\x0938'] = '\x101E'; // sa
            dev2Myanmar['\x0939'] = '\x101F'; // ha
            dev2Myanmar['\x0933'] = '\x1020'; // l underdot a

            // independent vowels
            dev2Myanmar['\x0905'] = '\x1021'; // a
            dev2Myanmar['\x0906'] = "\x1021\x102C"; // aa
            dev2Myanmar['\x0907'] = '\x1023'; // i
            dev2Myanmar['\x0908'] = '\x1024'; // ii
            dev2Myanmar['\x0909'] = '\x1025'; // u
            dev2Myanmar['\x090A'] = '\x1026'; // uu
            dev2Myanmar['\x090F'] = '\x1027'; // e
            dev2Myanmar['\x0913'] = '\x1029'; // o

            // dependent vowel signs
            dev2Myanmar['\x093E'] = '\x102C'; // aa
            dev2Myanmar['\x093F'] = '\x102D'; // i
            dev2Myanmar['\x0940'] = '\x102E'; // ii
            dev2Myanmar['\x0941'] = '\x102F'; // u
            dev2Myanmar['\x0942'] = '\x1030'; // uu
            dev2Myanmar['\x0947'] = '\x1031'; // e
            dev2Myanmar['\x094B'] = "\x1031\x102C"; // o

            // numerals
            dev2Myanmar['\x0966'] = '\x1040';
            dev2Myanmar['\x0967'] = '\x1041';
            dev2Myanmar['\x0968'] = '\x1042';
            dev2Myanmar['\x0969'] = '\x1043';
            dev2Myanmar['\x096A'] = '\x1044';
            dev2Myanmar['\x096B'] = '\x1045';
            dev2Myanmar['\x096C'] = '\x1046';
            dev2Myanmar['\x096D'] = '\x1047';
            dev2Myanmar['\x096E'] = '\x1048';
            dev2Myanmar['\x096F'] = '\x1049';

            // other
            dev2Myanmar['\x0964'] = '\x104A'; // danda
            dev2Myanmar['\x0902'] = '\x1036'; // niggahita
            dev2Myanmar['\x094D'] = '\x1039'; // virama
            dev2Myanmar['\x200C'] = ""; // ZWNJ (ignore)
            dev2Myanmar['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Myanmar
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-mymr.xsl");

            char[] dev = devStr.ToCharArray();
            StringBuilder sb = new StringBuilder();

            foreach (char c in dev)
            {
                if (dev2Myanmar.ContainsKey(c))
                    sb.Append(dev2Myanmar[c]);
                else
                    sb.Append(c);
            }

            string mya = sb.ToString();

    
            // Replace ña + virama + ña with ñña character
            mya = mya.Replace("\x1009\x1039\x1009", "\x100A");

            sw.Write(mya);

            sw.Flush();
            sw.Close();
            sr.Close();
        }
    }
}
