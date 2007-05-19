using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VRI.CSCD.Conversion
{
    class Deva2Khmr
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

                Deva2Khmr d2 = new Deva2Khmr();
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
            Console.WriteLine("Transliterates Devanagari to Unicode Khmer script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2khmr input [output]");
        }
        // end static methods


        private Hashtable dev2Khmer;

        public Deva2Khmr()
        {
            dev2Khmer = new Hashtable();

            // velar stops
            dev2Khmer['\x0915'] = '\x1780'; // ka
            dev2Khmer['\x0916'] = '\x1781'; // kha
            dev2Khmer['\x0917'] = '\x1782'; // ga
            dev2Khmer['\x0918'] = '\x1783'; // gha
            dev2Khmer['\x0919'] = '\x1784'; // n overdot a
            
            // palatal stops
            dev2Khmer['\x091A'] = '\x1785'; // ca
            dev2Khmer['\x091B'] = '\x1786'; // cha
            dev2Khmer['\x091C'] = '\x1787'; // ja
            dev2Khmer['\x091D'] = '\x1788'; // jha
            dev2Khmer['\x091E'] = '\x1789'; // ña

            // retroflex stops
            dev2Khmer['\x091F'] = '\x178A'; // t underdot a
            dev2Khmer['\x0920'] = '\x178B'; // t underdot ha
            dev2Khmer['\x0921'] = '\x178C'; // d underdot a
            dev2Khmer['\x0922'] = '\x178D'; // d underdot ha
            dev2Khmer['\x0923'] = '\x178E'; // n underdot a

            // dental stops
            dev2Khmer['\x0924'] = '\x178F'; // ta
            dev2Khmer['\x0925'] = '\x1790'; // tha
            dev2Khmer['\x0926'] = '\x1791'; // da
            dev2Khmer['\x0927'] = '\x1792'; // dha
            dev2Khmer['\x0928'] = '\x1793'; // na

            // labial stops
            dev2Khmer['\x092A'] = '\x1794'; // pa
            dev2Khmer['\x092B'] = '\x1795'; // pha
            dev2Khmer['\x092C'] = '\x1796'; // ba
            dev2Khmer['\x092D'] = '\x1797'; // bha
            dev2Khmer['\x092E'] = '\x1798'; // ma

            // liquids, fricatives, etc.
            dev2Khmer['\x092F'] = '\x1799'; // ya
            dev2Khmer['\x0930'] = '\x179A'; // ra
            dev2Khmer['\x0932'] = '\x179B'; // la
            dev2Khmer['\x0935'] = '\x179C'; // va
            dev2Khmer['\x0938'] = '\x179F'; // sa
            dev2Khmer['\x0939'] = '\x17A0'; // ha
            dev2Khmer['\x0933'] = '\x17A1'; // l underdot a

            // independent vowels
            dev2Khmer['\x0905'] = '\x17A2'; // a
            dev2Khmer['\x0906'] = "\x17A2\x17B6"; // aa
            dev2Khmer['\x0907'] = '\x17A5'; // i
            dev2Khmer['\x0908'] = '\x17A6'; // ii
            dev2Khmer['\x0909'] = '\x17A7'; // u
            dev2Khmer['\x090A'] = '\x17A9'; // uu
            dev2Khmer['\x090F'] = '\x17AF'; // e
            dev2Khmer['\x0913'] = '\x17B1'; // o

            // dependent vowel signs
            dev2Khmer['\x093E'] = '\x17B6'; // aa
            dev2Khmer['\x093F'] = '\x17B7'; // i
            dev2Khmer['\x0940'] = '\x17B8'; // ii
            dev2Khmer['\x0941'] = '\x17BB'; // u
            dev2Khmer['\x0942'] = '\x17BC'; // uu
            dev2Khmer['\x0947'] = '\x17C1'; // e
            dev2Khmer['\x094B'] = '\x17C4'; // o

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            dev2Khmer['\x0966'] = '\x17E0';
            dev2Khmer['\x0967'] = '\x17E1';
            dev2Khmer['\x0968'] = '\x17E2';
            dev2Khmer['\x0969'] = '\x17E3';
            dev2Khmer['\x096A'] = '\x17E4';
            dev2Khmer['\x096B'] = '\x17E5';
            dev2Khmer['\x096C'] = '\x17E6';
            dev2Khmer['\x096D'] = '\x17E7';
            dev2Khmer['\x096E'] = '\x17E8';
            dev2Khmer['\x096F'] = '\x17E9';

            // other
            dev2Khmer['\x0902'] = '\x17C6'; // niggahita
            dev2Khmer['\x094D'] = '\x17D2'; // virama
            dev2Khmer['\x0970'] = "."; // Dev abbreviation sign
            dev2Khmer['\x200C'] = ""; // ZWNJ (ignore)
            dev2Khmer['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Khmer
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-khmr.xsl");

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
                if (dev2Khmer.ContainsKey(c))
                    sb.Append(dev2Khmer[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public string ConvertDandas(string str)
        {
            // in gathas, single dandas convert to semicolon, double to period
            str = Regex.Replace(str, "<gatha[a-z0-9]*>.+</gatha[a-z0-9]*>",
                new MatchEvaluator(this.ConvertGathaDandas));

            // remove double dandas around namo tassa
            str = Regex.Replace(str, "<centre>.+</centre>",
                new MatchEvaluator(this.ConvertNamoTassaDandas));

            // convert all others to KHAN
            str = str.Replace("\x0964", "\x17D4");
            str = str.Replace("\x0965", "\x17D4");
            return str;
        }

        public string ConvertGathaDandas(Match m)
        {
            string str = m.Value;
            str = str.Replace("\x0964", ";");
            str = str.Replace("\x0965", "\x17D4");
            return str;
        }

        public string ConvertNamoTassaDandas(Match m)
        {
            string str = m.Value;
            return str.Replace("\x0965", "\x17D4");
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
