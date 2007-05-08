using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Dev2Latin
{
    class Dev2Latin
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

                Dev2Latin d2 = new Dev2Latin();
                d2.InputFilePath = args[0];
                d2.OutputFilePath = di.FullName + "\\" + fi.Name;
                d2.ParagraphElements = ConfigurationManager.AppSettings["ParagraphElements"].Split(',');
                d2.CapitalMarker = System.Convert.ToChar(
                        System.Convert.ToInt32(
                        ConfigurationManager.AppSettings["CapitalMarker"])
                        ).ToString();
                d2.Convert();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Unicode Devanagari to Unicode Latin, Pali characters conversion");
            Console.WriteLine("syntax:");
            Console.WriteLine("dev2lat input [output]");
        }
        // end static methods


        private Hashtable dev2Latin;

        public Dev2Latin()
        {
            dev2Latin = new Hashtable();

            // velar stops
            dev2Latin['\x0915'] = 'k'; // ka
            dev2Latin['\x0916'] = "kh"; // kha
            dev2Latin['\x0917'] = 'g'; // ga
            dev2Latin['\x0918'] = "gh"; // gha
            dev2Latin['\x0919'] = "\x1E45"; // n overdot a
            
            // palatal stops
            dev2Latin['\x091A'] = 'c'; // ca
            dev2Latin['\x091B'] = "ch"; // cha
            dev2Latin['\x091C'] = 'j'; // ja
            dev2Latin['\x091D'] = "jh"; // jha
            dev2Latin['\x091E'] = 'ñ'; // ña

            // retroflex stops
            dev2Latin['\x091F'] = '\x1E6D'; // t underdot a
            dev2Latin['\x0920'] = "\x1E6Dh"; // t underdot ha
            dev2Latin['\x0921'] = '\x1E0D'; // d underdot a
            dev2Latin['\x0922'] = "\x1E0Dh"; // d underdot ha
            dev2Latin['\x0923'] = '\x1E47'; // n underdot a

            // dental stops
            dev2Latin['\x0924'] = 't'; // ta
            dev2Latin['\x0925'] = "th"; // tha
            dev2Latin['\x0926'] = 'd'; // da
            dev2Latin['\x0927'] = "dh"; // dha
            dev2Latin['\x0928'] = 'n'; // na

            // labial stops
            dev2Latin['\x092A'] = 'p'; // pa
            dev2Latin['\x092B'] = "ph"; // pha
            dev2Latin['\x092C'] = 'b'; // ba
            dev2Latin['\x092D'] = "bh"; // bha
            dev2Latin['\x092E'] = 'm'; // ma

            // liquids, fricatives, etc.
            dev2Latin['\x092F'] = 'y'; // ya
            dev2Latin['\x0930'] = 'r'; // ra
            dev2Latin['\x0932'] = 'l'; // la
            dev2Latin['\x0935'] = 'v'; // va
            dev2Latin['\x0938'] = 's'; // sa
            dev2Latin['\x0939'] = 'h'; // ha
            dev2Latin['\x0933'] = "\x1E37"; // l underdot a

            // independent vowels
            dev2Latin['\x0905'] = 'a'; // a
            dev2Latin['\x0906'] = '\x0101'; // aa
            dev2Latin['\x0907'] = 'i'; // i
            dev2Latin['\x0908'] = '\x012B'; // ii
            dev2Latin['\x0909'] = 'u'; // u
            dev2Latin['\x090A'] = '\x016B'; // uu
            dev2Latin['\x090F'] = 'e'; // e
            dev2Latin['\x0913'] = 'o'; // o

            // dependent vowel signs
            dev2Latin['\x093E'] = '\x0101'; // aa
            dev2Latin['\x093F'] = 'i'; // i
            dev2Latin['\x0940'] = "\x012B"; // ii
            dev2Latin['\x0941'] = 'u'; // u
            dev2Latin['\x0942'] = '\x016B'; // uu
            dev2Latin['\x0947'] = 'e'; // e
            dev2Latin['\x094B'] = 'o'; // o

            // numerals
            dev2Latin['\x0966'] = '0';
            dev2Latin['\x0967'] = '1';
            dev2Latin['\x0968'] = '2';
            dev2Latin['\x0969'] = '3';
            dev2Latin['\x096A'] = '4';
            dev2Latin['\x096B'] = '5';
            dev2Latin['\x096C'] = '6';
            dev2Latin['\x096D'] = '7';
            dev2Latin['\x096E'] = '8';
            dev2Latin['\x096F'] = '9';

            // other
            dev2Latin['\x0964'] = '.'; // danda
            dev2Latin['\x0902'] = '\x1E43'; // niggahita
            dev2Latin['\x094D'] = ""; // virama
            dev2Latin['\x200C'] = ""; // ZWNJ (ignore)
            dev2Latin['\x200D'] = ""; // ZWJ (ignore)
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

        public string[] ParagraphElements
        {
            get { return paragraphElements; }
            set { paragraphElements = value; }
        }
        private string[] paragraphElements;

        public string CapitalMarker
        {
            get { return capitalMarker; }
            set { capitalMarker = value; }
        }
        private string capitalMarker;

        public void Convert()
        {
            StreamReader sr = new StreamReader(InputFilePath);
            string devStr = sr.ReadToEnd();

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.BigEndianUnicode);

            // zap the double danda around "namo tassa..."
            devStr = devStr.Replace("\x0964\x0964", "");

            // change name of stylesheet for Latin
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-latn.xsl");

            // mark the Devanagari text for programmatic capitalization
            Capitalizer capitalizer = new Capitalizer(this.ParagraphElements, this.CapitalMarker);
            devStr = capitalizer.MarkCapitals(devStr);

            // insert 'a' after all consonants that are not followed by virama, dependent vowel or 'a'
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])([^\x093E-\x094Da])", "$1a$2");
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])([^\x093E-\x094Da])", "$1a$2");
            // TODO: figure out how to backtrack so this replace doesn't have to be done twice

            // replace Devanagari zero in abbreviations with period
            // (any Dev zero following a Dev letter (or the short a from above) is assumed to be an 
            // abbreviation marker)
            devStr = Regex.Replace(devStr, "([a\x0901-\x0963])\x0966", "$1.");

            char[] dev = devStr.ToCharArray();
            StringBuilder sb = new StringBuilder();
            foreach (char c in dev)
            {
                if (dev2Latin.ContainsKey(c))
                    sb.Append(dev2Latin[c]);
                else
                    sb.Append(c);
            }

            string latin = sb.ToString();
            latin = capitalizer.Capitalize(latin);

            sw.Write(latin);

            sw.Flush();
            sw.Close();
            sr.Close();
        }
    }
}
