using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    class Deva2Sinh
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

                Deva2Sinh d2 = new Deva2Sinh();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Sinhala script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2sinh input [output]");
        }
        // end static methods


        private Hashtable deva2Sinh;

        public Deva2Sinh()
        {
            deva2Sinh = new Hashtable();

            deva2Sinh['\x0902'] = '\x0D82'; // niggahita

            // independent vowels
            deva2Sinh['\x0905'] = '\x0D85'; // a
            deva2Sinh['\x0906'] = '\x0D86'; // aa
            deva2Sinh['\x0907'] = '\x0D89'; // i
            deva2Sinh['\x0908'] = '\x0D8A'; // ii
            deva2Sinh['\x0909'] = '\x0D8B'; // u
            deva2Sinh['\x090A'] = '\x0D8C'; // uu
            deva2Sinh['\x090F'] = '\x0D91'; // e
            deva2Sinh['\x0913'] = '\x0D94'; // o

            // velar stops
            deva2Sinh['\x0915'] = '\x0D9A'; // ka
            deva2Sinh['\x0916'] = '\x0D9B'; // kha
            deva2Sinh['\x0917'] = '\x0D9C'; // ga
            deva2Sinh['\x0918'] = '\x0D9D'; // gha
            deva2Sinh['\x0919'] = '\x0D9E'; // n overdot a
            
            // palatal stops
            deva2Sinh['\x091A'] = '\x0DA0'; // ca
            deva2Sinh['\x091B'] = '\x0DA1'; // cha
            deva2Sinh['\x091C'] = '\x0DA2'; // ja
            deva2Sinh['\x091D'] = '\x0DA3'; // jha
            deva2Sinh['\x091E'] = '\x0DA4'; // ña

            // retroflex stops
            deva2Sinh['\x091F'] = '\x0DA7'; // t underdot a
            deva2Sinh['\x0920'] = '\x0DA8'; // t underdot ha
            deva2Sinh['\x0921'] = '\x0DA9'; // d underdot a
            deva2Sinh['\x0922'] = '\x0DAA'; // d underdot ha
            deva2Sinh['\x0923'] = '\x0DAB'; // n underdot a

            // dental stops
            deva2Sinh['\x0924'] = '\x0DAD'; // ta
            deva2Sinh['\x0925'] = '\x0DAE'; // tha
            deva2Sinh['\x0926'] = '\x0DAF'; // da
            deva2Sinh['\x0927'] = '\x0DB0'; // dha
            deva2Sinh['\x0928'] = '\x0DB1'; // na

            // labial stops
            deva2Sinh['\x092A'] = '\x0DB4'; // pa
            deva2Sinh['\x092B'] = '\x0DB5'; // pha
            deva2Sinh['\x092C'] = '\x0DB6'; // ba
            deva2Sinh['\x092D'] = '\x0DB7'; // bha
            deva2Sinh['\x092E'] = '\x0DB8'; // ma

            // liquids, fricatives, etc.
            deva2Sinh['\x092F'] = '\x0DBA'; // ya
            deva2Sinh['\x0930'] = '\x0DBB'; // ra
            deva2Sinh['\x0932'] = '\x0DBD'; // la
            deva2Sinh['\x0935'] = '\x0DC0'; // va
            deva2Sinh['\x0938'] = '\x0DC3'; // sa
            deva2Sinh['\x0939'] = '\x0DC4'; // ha
            deva2Sinh['\x0933'] = '\x0DC5'; // l underdot a

            // dependent vowel signs
            deva2Sinh['\x093E'] = '\x0DCF'; // aa
            deva2Sinh['\x093F'] = '\x0DD2'; // i
            deva2Sinh['\x0940'] = '\x0DD3'; // ii
            deva2Sinh['\x0941'] = '\x0DD4'; // u
            deva2Sinh['\x0942'] = '\x0DD6'; // uu
            deva2Sinh['\x0947'] = '\x0DD9'; // e
            deva2Sinh['\x094B'] = '\x0DDC'; // o

            // various signs
            deva2Sinh['\x094D'] = "\x0DCA\x200C"; // virama -> Sinhala virama + ZWNJ

            // we let dandas (U+0964) and double dandas (U+0965) pass through 
            // and handle them in ConvertDandas()

            // numerals
            deva2Sinh['\x0966'] = '0';
            deva2Sinh['\x0967'] = '1';
            deva2Sinh['\x0968'] = '2';
            deva2Sinh['\x0969'] = '3';
            deva2Sinh['\x096A'] = '4';
            deva2Sinh['\x096B'] = '5';
            deva2Sinh['\x096C'] = '6';
            deva2Sinh['\x096D'] = '7';
            deva2Sinh['\x096E'] = '8';
            deva2Sinh['\x096F'] = '9';

            // other
            deva2Sinh['\x0970'] = '.'; // Dev. abbreviation sign

            // zero-width joiners
            deva2Sinh['\x200C'] = ""; // ZWNJ (ignore)
            deva2Sinh['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Sinhala
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-sinh.xsl");

            string str = Convert(devStr);

            str = ConvertDandas(str);
            str = CleanupPunctuation(str);

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
                if (deva2Sinh.ContainsKey(c))
                    sb.Append(deva2Sinh[c]);
                else
                    sb.Append(c);
            }

            string str = sb.ToString();

            // a few special cases in Sinhala, per Vincent's email

            // change joiners before U+0DBA Yayanna to Virama + ZWJ
            str = str.Replace("\x0DCA\x200C\x0DBA", "\x0DCA\x200D\x0DBA");

            // change joiners before U+0DBB Rayanna to Virama + ZWJ
            str = str.Replace("\x0DCA\x200C\x0DBB", "\x0DCA\x200D\x0DBB");

            return str;
        }

        public string ConvertDandas(string str)
        {
            // in gathas, single dandas convert to semicolon, double to period
            // Regex note: the +? is the lazy quantifier which finds the shortest match
            str = Regex.Replace(str, "<p rend=\"gatha[a-z0-9]*\".+?</p>",
                new MatchEvaluator(this.ConvertGathaDandas));

            // remove double dandas around namo tassa
            str = Regex.Replace(str, "<p rend=\"centre\".+?</p>",
                new MatchEvaluator(this.RemoveNamoTassaDandas));

            // convert all others to period
            str = str.Replace("\x0964", ".");
            str = str.Replace("\x0965", ".");
            return str;
        }

        public string ConvertGathaDandas(Match m)
        {
            string str = m.Value;
            str = str.Replace("\x0964", ";");
            str = str.Replace("\x0965", ".");
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
			// two spaces to one
			str = str.Replace("  ", " ");

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
