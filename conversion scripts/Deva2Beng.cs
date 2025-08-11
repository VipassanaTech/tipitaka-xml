using System;
using System.Collections;
using System.IO;
using System.Text;

namespace VRI.CSCD.Conversion
{
    class Deva2Beng
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

                Deva2Beng d2 = new Deva2Beng();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Bengali");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2beng input [output]");
        }
        // end static methods


        private Hashtable deva2Beng;

        public Deva2Beng()
        {
            deva2Beng = new Hashtable();

            deva2Beng['\x0902'] = '\x0982'; // niggahita

            // independent vowels
            deva2Beng['\x0905'] = '\x0985'; // a
            deva2Beng['\x0906'] = '\x0986'; // aa
            deva2Beng['\x0907'] = '\x0987'; // i
            deva2Beng['\x0908'] = '\x0988'; // ii
            deva2Beng['\x0909'] = '\x0989'; // u
            deva2Beng['\x090A'] = '\x098A'; // uu
            deva2Beng['\x090B'] = '\x098B'; // vocalic r
            deva2Beng['\x090C'] = '\x098C'; // vocalic l
            deva2Beng['\x090F'] = '\x098F'; // e
            deva2Beng['\x0910'] = '\x0990'; // ai
            deva2Beng['\x0913'] = '\x0993'; // o
            deva2Beng['\x0914'] = '\x0994'; // au

            // velar stops
            deva2Beng['\x0915'] = '\x0995'; // ka
            deva2Beng['\x0916'] = '\x0996'; // kha
            deva2Beng['\x0917'] = '\x0997'; // ga
            deva2Beng['\x0918'] = '\x0998'; // gha
            deva2Beng['\x0919'] = '\x0999'; // n overdot a

            // palatal stops
            deva2Beng['\x091A'] = '\x099A'; // ca
            deva2Beng['\x091B'] = '\x099B'; // cha
            deva2Beng['\x091C'] = '\x099C'; // ja
            deva2Beng['\x091D'] = '\x099D'; // jha
            deva2Beng['\x091E'] = '\x099E'; // ña

            // retroflex stops
            deva2Beng['\x091F'] = '\x099F'; // t underdot a
            deva2Beng['\x0920'] = '\x09A0'; // t underdot ha
            deva2Beng['\x0921'] = '\x09A1'; // d underdot a
            deva2Beng['\x0922'] = '\x09A2'; // d underdot ha
            deva2Beng['\x0923'] = '\x09A3'; // n underdot a

            // dental stops
            deva2Beng['\x0924'] = '\x09A4'; // ta
            deva2Beng['\x0925'] = '\x09A5'; // tha
            deva2Beng['\x0926'] = '\x09A6'; // da
            deva2Beng['\x0927'] = '\x09A7'; // dha
            deva2Beng['\x0928'] = '\x09A8'; // na

            // labial stops
            deva2Beng['\x092A'] = '\x09AA'; // pa
            deva2Beng['\x092B'] = '\x09AB'; // pha
            deva2Beng['\x092C'] = '\x09AC'; // ba
            deva2Beng['\x092D'] = '\x09AD'; // bha
            deva2Beng['\x092E'] = '\x09AE'; // ma

            // liquids, fricatives, etc.
            deva2Beng['\x092F'] = '\x09AF'; // ya
            deva2Beng['\x0930'] = '\x09B0'; // ra
            deva2Beng['\x0932'] = '\x09B2'; // la
            deva2Beng['\x0933'] = "\x09B2\x09BC"; // l underdot a *** la with dot, there's no l underdot in Bengali***
            deva2Beng['\x0935'] = "\x09F0"; // va *** Bengali ra with middle diagonal. Used for Assamese. ***
            deva2Beng['\x0936'] = '\x09B6'; // sha (palatal)
            deva2Beng['\x0937'] = '\x09B7'; // sha (retroflex)
            deva2Beng['\x0938'] = '\x09B8'; // sa
            deva2Beng['\x0939'] = '\x09B9'; // ha

            // dependent vowel signs
            deva2Beng['\x093E'] = '\x09BE'; // aa
            deva2Beng['\x093F'] = '\x09BF'; // i
            deva2Beng['\x0940'] = '\x09C0'; // ii
            deva2Beng['\x0941'] = '\x09C1'; // u
            deva2Beng['\x0942'] = '\x09C2'; // uu
            deva2Beng['\x0943'] = '\x09C3'; // vocalic r
            deva2Beng['\x0947'] = '\x09C7'; // e
            deva2Beng['\x0948'] = '\x09C8'; // ai
            deva2Beng['\x094B'] = '\x09CB'; // o
            deva2Beng['\x094C'] = '\x09CC'; // au

            deva2Beng['\x094D'] = '\x09CD'; // virama

            // numerals
            deva2Beng['\x0966'] = '\x09E6';
            deva2Beng['\x0967'] = '\x09E7';
            deva2Beng['\x0968'] = '\x09E8';
            deva2Beng['\x0969'] = '\x09E9';
            deva2Beng['\x096A'] = '\x09EA';
            deva2Beng['\x096B'] = '\x09EB';
            deva2Beng['\x096C'] = '\x09EC';
            deva2Beng['\x096D'] = '\x09ED';
            deva2Beng['\x096E'] = '\x09EE';
            deva2Beng['\x096F'] = '\x09EF';

            deva2Beng['\x200C'] = ""; // ZWNJ (remove)
            deva2Beng['\x200D'] = ""; // ZWJ (remove)
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

            // change name of stylesheet for Bengali
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-beng.xsl");

            string str = Convert(devStr);

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.Unicode);
            sw.Write(str);
            sw.Flush();
            sw.Close();
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications or capitalization
        public string Convert(string devStr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Beng.ContainsKey(c))
                    sb.Append(deva2Beng[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
