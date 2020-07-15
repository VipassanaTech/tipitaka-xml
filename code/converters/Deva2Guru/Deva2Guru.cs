using System;
using System.Collections;
using System.IO;
using System.Text;

namespace CST.Conversion
{
    class Deva2Guru
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

                Deva2Guru d2 = new Deva2Guru();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Gurmukhi");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2guru input [output]");
        }
        // end static methods


        private Hashtable deva2Guru;

        public Deva2Guru()
        {
            deva2Guru = new Hashtable();

            // various signs
            deva2Guru['\x0901'] = '\x0A01'; // candrabindhu
            deva2Guru['\x0902'] = '\x0A02'; // anusvara
            deva2Guru['\x0903'] = '\x0A03'; // visarga

            // independent vowels
            deva2Guru['\x0905'] = '\x0A05'; // a
            deva2Guru['\x0906'] = '\x0A06'; // aa
            deva2Guru['\x0907'] = '\x0A07'; // i
            deva2Guru['\x0908'] = '\x0A08'; // ii
            deva2Guru['\x0909'] = '\x0A09'; // u
            deva2Guru['\x090A'] = '\x0A0A'; // uu
            deva2Guru['\x090F'] = '\x0A0F'; // e
            deva2Guru['\x0910'] = '\x0A10'; // ai
            deva2Guru['\x0913'] = '\x0A13'; // o
            deva2Guru['\x0914'] = '\x0A14'; // au

            // velar stops
            deva2Guru['\x0915'] = '\x0A15'; // ka
            deva2Guru['\x0916'] = '\x0A16'; // kha
            deva2Guru['\x0917'] = '\x0A17'; // ga
            deva2Guru['\x0918'] = '\x0A18'; // gha
            deva2Guru['\x0919'] = '\x0A19'; // n overdot a
            
            // palatal stops
            deva2Guru['\x091A'] = '\x0A1A'; // ca
            deva2Guru['\x091B'] = '\x0A1B'; // cha
            deva2Guru['\x091C'] = '\x0A1C'; // ja
            deva2Guru['\x091D'] = '\x0A1D'; // jha
            deva2Guru['\x091E'] = '\x0A1E'; // ña

            // retroflex stops
            deva2Guru['\x091F'] = '\x0A1F'; // t underdot a
            deva2Guru['\x0920'] = '\x0A20'; // t underdot ha
            deva2Guru['\x0921'] = '\x0A21'; // d underdot a
            deva2Guru['\x0922'] = '\x0A22'; // d underdot ha
            deva2Guru['\x0923'] = '\x0A23'; // n underdot a

            // dental stops
            deva2Guru['\x0924'] = '\x0A24'; // ta
            deva2Guru['\x0925'] = '\x0A25'; // tha
            deva2Guru['\x0926'] = '\x0A26'; // da
            deva2Guru['\x0927'] = '\x0A27'; // dha
            deva2Guru['\x0928'] = '\x0A28'; // na

            // labial stops
            deva2Guru['\x092A'] = '\x0A2A'; // pa
            deva2Guru['\x092B'] = '\x0A2B'; // pha
            deva2Guru['\x092C'] = '\x0A2C'; // ba
            deva2Guru['\x092D'] = '\x0A2D'; // bha
            deva2Guru['\x092E'] = '\x0A2E'; // ma

            // liquids, fricatives, etc.
            deva2Guru['\x092F'] = '\x0A2F'; // ya
            deva2Guru['\x0930'] = '\x0A30'; // ra
            deva2Guru['\x0932'] = '\x0A32'; // la
            deva2Guru['\x0933'] = '\x0A33'; // l underdot a
            deva2Guru['\x0935'] = '\x0AB5'; // va
            deva2Guru['\x0936'] = '\x0A36'; // sha (palatal)
            deva2Guru['\x0938'] = '\x0A38'; // sa
            deva2Guru['\x0939'] = '\x0A39'; // ha

            // dependent vowel signs
            deva2Guru['\x093E'] = '\x0A3E'; // aa
            deva2Guru['\x093F'] = '\x0A3F'; // i
            deva2Guru['\x0940'] = '\x0A40'; // ii
            deva2Guru['\x0941'] = '\x0A41'; // u
            deva2Guru['\x0942'] = '\x0A42'; // uu
            deva2Guru['\x0947'] = '\x0A47'; // e
            deva2Guru['\x0948'] = '\x0A48'; // ai
            deva2Guru['\x094B'] = '\x0A4B'; // o
            deva2Guru['\x094C'] = '\x0A4C'; // au

            // various signs
            deva2Guru['\x094D'] = '\x0A4D'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            deva2Guru['\x0966'] = '\x0A66';
            deva2Guru['\x0967'] = '\x0A67';
            deva2Guru['\x0968'] = '\x0A68';
            deva2Guru['\x0969'] = '\x0A69';
            deva2Guru['\x096A'] = '\x0A6A';
            deva2Guru['\x096B'] = '\x0A6B';
            deva2Guru['\x096C'] = '\x0A6C';
            deva2Guru['\x096D'] = '\x0A6D';
            deva2Guru['\x096E'] = '\x0A6E';
            deva2Guru['\x096F'] = '\x0A6F';

            // zero-width joiners
            deva2Guru['\x200C'] = ""; // ZWNJ (remove)
            deva2Guru['\x200D'] = ""; // ZWJ (remove)
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
            sr.Close();

            // change name of stylesheet for Gurmukhi
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-guru.xsl");

            string str = Convert(devStr);

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.Unicode);
            sw.Write(str);
            sw.Flush();
            sw.Close();
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public string Convert(string devStr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Guru.ContainsKey(c))
                    sb.Append(deva2Guru[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
