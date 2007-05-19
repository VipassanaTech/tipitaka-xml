using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VRI.CSCD.Conversion
{
    class Deva2Mymr
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

                Deva2Mymr d2t = new Deva2Mymr();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Myanmar script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2mymr input [output]");
        }
        // end static methods


        private Hashtable deva2Mymr;

        public Deva2Mymr()
        {
            deva2Mymr = new Hashtable();

            // velar stops
            deva2Mymr['\x0915'] = '\x1000'; // ka
            deva2Mymr['\x0916'] = '\x1001'; // kha
            deva2Mymr['\x0917'] = '\x1002'; // ga
            deva2Mymr['\x0918'] = '\x1003'; // gha
            deva2Mymr['\x0919'] = '\x1004'; // n overdot a
            
            // palatal stops
            deva2Mymr['\x091A'] = '\x1005'; // ca
            deva2Mymr['\x091B'] = '\x1006'; // cha
            deva2Mymr['\x091C'] = '\x1007'; // ja
            deva2Mymr['\x091D'] = '\x1008'; // jha
            deva2Mymr['\x091E'] = '\x1009'; // ña

            // retroflex stops
            deva2Mymr['\x091F'] = '\x100B'; // t underdot a
            deva2Mymr['\x0920'] = '\x100C'; // t underdot ha
            deva2Mymr['\x0921'] = '\x100D'; // d underdot a
            deva2Mymr['\x0922'] = '\x100E'; // d underdot ha
            deva2Mymr['\x0923'] = '\x100F'; // n underdot a

            // dental stops
            deva2Mymr['\x0924'] = '\x1010'; // ta
            deva2Mymr['\x0925'] = '\x1011'; // tha
            deva2Mymr['\x0926'] = '\x1012'; // da
            deva2Mymr['\x0927'] = '\x1013'; // dha
            deva2Mymr['\x0928'] = '\x1014'; // na

            // labial stops
            deva2Mymr['\x092A'] = '\x1015'; // pa
            deva2Mymr['\x092B'] = '\x1016'; // pha
            deva2Mymr['\x092C'] = '\x1017'; // ba
            deva2Mymr['\x092D'] = '\x1018'; // bha
            deva2Mymr['\x092E'] = '\x1019'; // ma

            // liquids, fricatives, etc.
            deva2Mymr['\x092F'] = '\x101A'; // ya
            deva2Mymr['\x0930'] = '\x101B'; // ra
            deva2Mymr['\x0932'] = '\x101C'; // la
            deva2Mymr['\x0935'] = '\x101D'; // va
            deva2Mymr['\x0938'] = '\x101E'; // sa
            deva2Mymr['\x0939'] = '\x101F'; // ha
            deva2Mymr['\x0933'] = '\x1020'; // l underdot a

            // independent vowels
            deva2Mymr['\x0905'] = '\x1021'; // a
            deva2Mymr['\x0906'] = "\x1021\x102C"; // aa
            deva2Mymr['\x0907'] = '\x1023'; // i
            deva2Mymr['\x0908'] = '\x1024'; // ii
            deva2Mymr['\x0909'] = '\x1025'; // u
            deva2Mymr['\x090A'] = '\x1026'; // uu
            deva2Mymr['\x090F'] = '\x1027'; // e
            deva2Mymr['\x0913'] = '\x1029'; // o

            // dependent vowel signs
            deva2Mymr['\x093E'] = '\x102C'; // aa
            deva2Mymr['\x093F'] = '\x102D'; // i
            deva2Mymr['\x0940'] = '\x102E'; // ii
            deva2Mymr['\x0941'] = '\x102F'; // u
            deva2Mymr['\x0942'] = '\x1030'; // uu
            deva2Mymr['\x0947'] = '\x1031'; // e
            deva2Mymr['\x094B'] = "\x1031\x102C"; // o

            // numerals
            deva2Mymr['\x0966'] = '\x1040';
            deva2Mymr['\x0967'] = '\x1041';
            deva2Mymr['\x0968'] = '\x1042';
            deva2Mymr['\x0969'] = '\x1043';
            deva2Mymr['\x096A'] = '\x1044';
            deva2Mymr['\x096B'] = '\x1045';
            deva2Mymr['\x096C'] = '\x1046';
            deva2Mymr['\x096D'] = '\x1047';
            deva2Mymr['\x096E'] = '\x1048';
            deva2Mymr['\x096F'] = '\x1049';

            // other
            deva2Mymr['\x0902'] = '\x1036'; // niggahita
            deva2Mymr['\x094D'] = '\x1039'; // virama
            // we let dandas and double dandas pass through and handle
            // them in ConvertDandas()
            //deva2Mymr['\x0964'] = '\x104B';
            deva2Mymr['\x0970'] = '.'; // Devanagari abbreviation sign
            deva2Mymr['\x200C'] = ""; // ZWNJ (ignore)
            deva2Mymr['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Myanmar
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-mymr.xsl");

            // convert to Myanmar style of peyyala: double line + pe + double line
            // (we do this here to remove the Dev. abbreviation sign, which would otherwise
            // be converted to period in Convert())
            devStr = devStr.Replace("\x2026\x092A\x094B\x0970\x2026", "\x104B\x1015\x1031\x104B");

            string str = Convert(devStr);

            str = ConvertDandas(str);
            str = CleanupPunctuation(str);

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.BigEndianUnicode);
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
                if (deva2Mymr.ContainsKey(c))
                    sb.Append(deva2Mymr[c]);
                else
                    sb.Append(c);
            }

            string mya = sb.ToString();

            // Replace ña + virama + ña with ñña character
            mya = mya.Replace("\x1009\x1039\x1009", "\x100A");

            return mya;
        }

        public string ConvertDandas(string str)
        {
            // in gathas, single dandas convert to semicolon, double to period
            str = Regex.Replace(str, "<gatha[a-z0-9]*>.+</gatha[a-z0-9]*>",
                new MatchEvaluator(this.ConvertGathaDandas));

            // convert all others to double line
            str = str.Replace("\x0964", "\x104B");
            str = str.Replace("\x0965", "\x104B");

            // convert ellipsis to double line
            str = str.Replace("\x2026", "\x104B");

            return str;
        }

        public string ConvertGathaDandas(Match m)
        {
            string str = m.Value;
            str = str.Replace(",", "\x104A"); // comma -> single line
            str = str.Replace("\x0964", "\x104B"); // danda -> double line
            str = str.Replace("\x0965", "\x104B"); // double danda -> double line
            return str;
        }

        public string RemoveNamoTassaDandas(Match m)
        {
            string str = m.Value;
            return str.Replace("\x0965", "");
        }

        // There should be no spaces before these
        // punctuation marks. 
        public string CleanupPunctuation(string str)
        {
            str = str.Replace(" ,", ",");
            str = str.Replace(" ?", "?");
            str = str.Replace(" !", "!");
            str = str.Replace(" ;", ";");
            // does not affect peyyalas because they have ellipses now
            str = str.Replace(" .", ".");
            return str;
        }
    }
}
