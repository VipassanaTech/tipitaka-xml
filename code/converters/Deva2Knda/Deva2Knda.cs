using System;
using System.Collections;
using System.IO;
using System.Text;

namespace CST.Conversion
{
    class Deva2Knda
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

                Deva2Knda d2 = new Deva2Knda();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Kannada script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2knda input [output]");
        }
        // end static methods


        private Hashtable deva2Knda;

        public Deva2Knda()
        {
            deva2Knda = new Hashtable();

            // various signs
            deva2Knda['\x0902'] = '\x0C82'; // anusvara
            deva2Knda['\x0903'] = '\x0C83'; // visarga

            // independent vowels
            deva2Knda['\x0905'] = '\x0C85'; // a
            deva2Knda['\x0906'] = '\x0C86'; // aa
            deva2Knda['\x0907'] = '\x0C87'; // i
            deva2Knda['\x0908'] = '\x0C88'; // ii
            deva2Knda['\x0909'] = '\x0C89'; // u
            deva2Knda['\x090A'] = '\x0C8A'; // uu
            deva2Knda['\x090B'] = '\x0C8B'; // vocalic r
            deva2Knda['\x090C'] = '\x0C8C'; // vocalic l
            deva2Knda['\x090F'] = '\x0C8F'; // e
            deva2Knda['\x0910'] = '\x0C90'; // ai
            deva2Knda['\x0913'] = '\x0C93'; // o
            deva2Knda['\x0914'] = '\x0C94'; // au

            // velar stops
            deva2Knda['\x0915'] = '\x0C95'; // ka
            deva2Knda['\x0916'] = '\x0C96'; // kha
            deva2Knda['\x0917'] = '\x0C97'; // ga
            deva2Knda['\x0918'] = '\x0C98'; // gha
            deva2Knda['\x0919'] = '\x0C99'; // n overdot a
            
            // palatal stops
            deva2Knda['\x091A'] = '\x0C9A'; // ca
            deva2Knda['\x091B'] = '\x0C9B'; // cha
            deva2Knda['\x091C'] = '\x0C9C'; // ja
            deva2Knda['\x091D'] = '\x0C9D'; // jha
            deva2Knda['\x091E'] = '\x0C9E'; // ña

            // retroflex stops
            deva2Knda['\x091F'] = '\x0C9F'; // t underdot a
            deva2Knda['\x0920'] = '\x0CA0'; // t underdot ha
            deva2Knda['\x0921'] = '\x0CA1'; // d underdot a
            deva2Knda['\x0922'] = '\x0CA2'; // d underdot ha
            deva2Knda['\x0923'] = '\x0CA3'; // n underdot a

            // dental stops
            deva2Knda['\x0924'] = '\x0CA4'; // ta
            deva2Knda['\x0925'] = '\x0CA5'; // tha
            deva2Knda['\x0926'] = '\x0CA6'; // da
            deva2Knda['\x0927'] = '\x0CA7'; // dha
            deva2Knda['\x0928'] = '\x0CA8'; // na

            // labial stops
            deva2Knda['\x092A'] = '\x0CAA'; // pa
            deva2Knda['\x092B'] = '\x0CAB'; // pha
            deva2Knda['\x092C'] = '\x0CAC'; // ba
            deva2Knda['\x092D'] = '\x0CAD'; // bha
            deva2Knda['\x092E'] = '\x0CAE'; // ma

            // liquids, fricatives, etc.
            deva2Knda['\x092F'] = '\x0CAF'; // ya
            deva2Knda['\x0930'] = '\x0CB0'; // ra
            deva2Knda['\x0931'] = '\x0CB1'; // rra (Dravidian-specific)
            deva2Knda['\x0932'] = '\x0CB2'; // la
            deva2Knda['\x0933'] = '\x0CB3'; // l underdot a
            deva2Knda['\x0935'] = '\x0CB5'; // va
            deva2Knda['\x0936'] = '\x0CB6'; // sha (palatal)
            deva2Knda['\x0937'] = '\x0CB7'; // sha (retroflex)
            deva2Knda['\x0938'] = '\x0CB8'; // sa
            deva2Knda['\x0939'] = '\x0CB9'; // ha

            // various signs
            deva2Knda['\x093C'] = '\x0CBC'; // nukta
            deva2Knda['\x093D'] = '\x0CBD'; // avagraha

            // dependent vowel signs
            deva2Knda['\x093E'] = '\x0CBE'; // aa
            deva2Knda['\x093F'] = '\x0CBF'; // i
            deva2Knda['\x0940'] = '\x0CC0'; // ii
            deva2Knda['\x0941'] = '\x0CC1'; // u
            deva2Knda['\x0942'] = '\x0CC2'; // uu
            deva2Knda['\x0943'] = '\x0CC3'; // vocalic r
            deva2Knda['\x0944'] = '\x0CC4'; // vocalic rr
            deva2Knda['\x0947'] = '\x0CC7'; // e
            deva2Knda['\x0948'] = '\x0CC8'; // ai
            deva2Knda['\x094B'] = '\x0CCB'; // o
            deva2Knda['\x094C'] = '\x0CCC'; // au

            // various signs
            deva2Knda['\x094D'] = '\x0CCD'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            deva2Knda['\x0966'] = '\x0CE6';
            deva2Knda['\x0967'] = '\x0CE7';
            deva2Knda['\x0968'] = '\x0CE8';
            deva2Knda['\x0969'] = '\x0CE9';
            deva2Knda['\x096A'] = '\x0CEA';
            deva2Knda['\x096B'] = '\x0CEB';
            deva2Knda['\x096C'] = '\x0CEC';
            deva2Knda['\x096D'] = '\x0CED';
            deva2Knda['\x096E'] = '\x0CEE';
            deva2Knda['\x096F'] = '\x0CEF';

            // zero-width joiners
            deva2Knda['\x200C'] = ""; // ZWNJ (remove)
            deva2Knda['\x200D'] = ""; // ZWJ (remove)
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

            // change name of stylesheet for Kannada
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-knda.xsl");

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
                if (deva2Knda.ContainsKey(c))
                    sb.Append(deva2Knda[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
