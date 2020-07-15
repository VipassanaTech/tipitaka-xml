using System;
using System.Collections;
using System.IO;
using System.Text;

namespace VRI.CSCD.Conversion
{
    class Dev2Kannada
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

                Dev2Kannada d2 = new Dev2Kannada();
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


        private Hashtable dev2Kannada;

        public Dev2Kannada()
        {
            dev2Kannada = new Hashtable();

            // various signs
            dev2Kannada['\x0902'] = '\x0C82'; // anusvara
            dev2Kannada['\x0903'] = '\x0C83'; // visarga

            // independent vowels
            dev2Kannada['\x0905'] = '\x0C85'; // a
            dev2Kannada['\x0906'] = '\x0C86'; // aa
            dev2Kannada['\x0907'] = '\x0C87'; // i
            dev2Kannada['\x0908'] = '\x0C88'; // ii
            dev2Kannada['\x0909'] = '\x0C89'; // u
            dev2Kannada['\x090A'] = '\x0C8A'; // uu
            dev2Kannada['\x090B'] = '\x0C8B'; // vocalic r
            dev2Kannada['\x090C'] = '\x0C8C'; // vocalic l
            dev2Kannada['\x090F'] = '\x0C8F'; // e
            dev2Kannada['\x0910'] = '\x0C90'; // ai
            dev2Kannada['\x0913'] = '\x0C93'; // o
            dev2Kannada['\x0914'] = '\x0C94'; // au

            // velar stops
            dev2Kannada['\x0915'] = '\x0C95'; // ka
            dev2Kannada['\x0916'] = '\x0C96'; // kha
            dev2Kannada['\x0917'] = '\x0C97'; // ga
            dev2Kannada['\x0918'] = '\x0C98'; // gha
            dev2Kannada['\x0919'] = '\x0C99'; // n overdot a
            
            // palatal stops
            dev2Kannada['\x091A'] = '\x0C9A'; // ca
            dev2Kannada['\x091B'] = '\x0C9B'; // cha
            dev2Kannada['\x091C'] = '\x0C9C'; // ja
            dev2Kannada['\x091D'] = '\x0C9D'; // jha
            dev2Kannada['\x091E'] = '\x0C9E'; // ña

            // retroflex stops
            dev2Kannada['\x091F'] = '\x0C9F'; // t underdot a
            dev2Kannada['\x0920'] = '\x0CA0'; // t underdot ha
            dev2Kannada['\x0921'] = '\x0CA1'; // d underdot a
            dev2Kannada['\x0922'] = '\x0CA2'; // d underdot ha
            dev2Kannada['\x0923'] = '\x0CA3'; // n underdot a

            // dental stops
            dev2Kannada['\x0924'] = '\x0CA4'; // ta
            dev2Kannada['\x0925'] = '\x0CA5'; // tha
            dev2Kannada['\x0926'] = '\x0CA6'; // da
            dev2Kannada['\x0927'] = '\x0CA7'; // dha
            dev2Kannada['\x0928'] = '\x0CA8'; // na

            // labial stops
            dev2Kannada['\x092A'] = '\x0CAA'; // pa
            dev2Kannada['\x092B'] = '\x0CAB'; // pha
            dev2Kannada['\x092C'] = '\x0CAC'; // ba
            dev2Kannada['\x092D'] = '\x0CAD'; // bha
            dev2Kannada['\x092E'] = '\x0CAE'; // ma

            // liquids, fricatives, etc.
            dev2Kannada['\x092F'] = '\x0CAF'; // ya
            dev2Kannada['\x0930'] = '\x0CB0'; // ra
            dev2Kannada['\x0931'] = '\x0CB1'; // rra (Dravidian-specific)
            dev2Kannada['\x0932'] = '\x0CB2'; // la
            dev2Kannada['\x0933'] = '\x0CB3'; // l underdot a
            dev2Kannada['\x0935'] = '\x0CB5'; // va
            dev2Kannada['\x0936'] = '\x0CB6'; // sha (palatal)
            dev2Kannada['\x0937'] = '\x0CB7'; // sha (retroflex)
            dev2Kannada['\x0938'] = '\x0CB8'; // sa
            dev2Kannada['\x0939'] = '\x0CB9'; // ha

            // various signs
            dev2Kannada['\x093C'] = '\x0CBC'; // nukta
            dev2Kannada['\x093D'] = '\x0CBD'; // avagraha

            // dependent vowel signs
            dev2Kannada['\x093E'] = '\x0CBE'; // aa
            dev2Kannada['\x093F'] = '\x0CBF'; // i
            dev2Kannada['\x0940'] = '\x0CC0'; // ii
            dev2Kannada['\x0941'] = '\x0CC1'; // u
            dev2Kannada['\x0942'] = '\x0CC2'; // uu
            dev2Kannada['\x0943'] = '\x0CC3'; // vocalic r
            dev2Kannada['\x0944'] = '\x0CC4'; // vocalic rr
            dev2Kannada['\x0947'] = '\x0CC7'; // e
            dev2Kannada['\x0948'] = '\x0CC8'; // ai
            dev2Kannada['\x094B'] = '\x0CCB'; // o
            dev2Kannada['\x094C'] = '\x0CCC'; // au

            // various signs
            dev2Kannada['\x094D'] = '\x0CCD'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            dev2Kannada['\x0966'] = '\x0CE6';
            dev2Kannada['\x0967'] = '\x0CE7';
            dev2Kannada['\x0968'] = '\x0CE8';
            dev2Kannada['\x0969'] = '\x0CE9';
            dev2Kannada['\x096A'] = '\x0CEA';
            dev2Kannada['\x096B'] = '\x0CEB';
            dev2Kannada['\x096C'] = '\x0CEC';
            dev2Kannada['\x096D'] = '\x0CED';
            dev2Kannada['\x096E'] = '\x0CEE';
            dev2Kannada['\x096F'] = '\x0CEF';

            // zero-width joiners
            dev2Kannada['\x200C'] = ""; // ZWNJ (remove)
            dev2Kannada['\x200D'] = ""; // ZWJ (remove)
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
                if (dev2Kannada.ContainsKey(c))
                    sb.Append(dev2Kannada[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
