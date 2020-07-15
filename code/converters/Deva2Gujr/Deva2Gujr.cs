using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace VRI.CSCD.Conversion
{
    class Deva2Gujr
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

                Deva2Gujr d2 = new Deva2Gujr();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Gujarati script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2gujr input [output]");
        }
        // end static methods


        private Hashtable deva2Gujarati;

        public Deva2Gujr()
        {
            deva2Gujarati = new Hashtable();

            // various signs
            deva2Gujarati['\x0902'] = '\x0A82'; // niggahita

            // independent vowels
            deva2Gujarati['\x0905'] = '\x0A85'; // a
            deva2Gujarati['\x0906'] = '\x0A86'; // aa
            deva2Gujarati['\x0907'] = '\x0A87'; // i
            deva2Gujarati['\x0908'] = '\x0A88'; // ii
            deva2Gujarati['\x0909'] = '\x0A89'; // u
            deva2Gujarati['\x090A'] = '\x0A8A'; // uu
            deva2Gujarati['\x090F'] = '\x0A8F'; // e
            deva2Gujarati['\x0913'] = '\x0A93'; // o

            // velar stops
            deva2Gujarati['\x0915'] = '\x0A95'; // ka
            deva2Gujarati['\x0916'] = '\x0A96'; // kha
            deva2Gujarati['\x0917'] = '\x0A97'; // ga
            deva2Gujarati['\x0918'] = '\x0A98'; // gha
            deva2Gujarati['\x0919'] = '\x0A99'; // n overdot a
            
            // palatal stops
            deva2Gujarati['\x091A'] = '\x0A9A'; // ca
            deva2Gujarati['\x091B'] = '\x0A9B'; // cha
            deva2Gujarati['\x091C'] = '\x0A9C'; // ja
            deva2Gujarati['\x091D'] = '\x0A9D'; // jha
            deva2Gujarati['\x091E'] = '\x0A9E'; // ña

            // retroflex stops
            deva2Gujarati['\x091F'] = '\x0A9F'; // t underdot a
            deva2Gujarati['\x0920'] = '\x0AA0'; // t underdot ha
            deva2Gujarati['\x0921'] = '\x0AA1'; // d underdot a
            deva2Gujarati['\x0922'] = '\x0AA2'; // d underdot ha
            deva2Gujarati['\x0923'] = '\x0AA3'; // n underdot a

            // dental stops
            deva2Gujarati['\x0924'] = '\x0AA4'; // ta
            deva2Gujarati['\x0925'] = '\x0AA5'; // tha
            deva2Gujarati['\x0926'] = '\x0AA6'; // da
            deva2Gujarati['\x0927'] = '\x0AA7'; // dha
            deva2Gujarati['\x0928'] = '\x0AA8'; // na

            // labial stops
            deva2Gujarati['\x092A'] = '\x0AAA'; // pa
            deva2Gujarati['\x092B'] = '\x0AAB'; // pha
            deva2Gujarati['\x092C'] = '\x0AAC'; // ba
            deva2Gujarati['\x092D'] = '\x0AAD'; // bha
            deva2Gujarati['\x092E'] = '\x0AAE'; // ma

            // liquids, fricatives, etc.
            deva2Gujarati['\x092F'] = '\x0AAF'; // ya
            deva2Gujarati['\x0930'] = '\x0AB0'; // ra
            deva2Gujarati['\x0932'] = '\x0AB2'; // la
            deva2Gujarati['\x0933'] = '\x0AB3'; // l underdot a
            deva2Gujarati['\x0935'] = '\x0AB5'; // va
            deva2Gujarati['\x0936'] = '\x0AB6'; // sha (palatal)
            deva2Gujarati['\x0937'] = '\x0AB7'; // sha (retroflex)
            deva2Gujarati['\x0938'] = '\x0AB8'; // sa
            deva2Gujarati['\x0939'] = '\x0AB9'; // ha

            // dependent vowel signs
            deva2Gujarati['\x093E'] = '\x0ABE'; // aa
            deva2Gujarati['\x093F'] = '\x0ABF'; // i
            deva2Gujarati['\x0940'] = '\x0AC0'; // ii
            deva2Gujarati['\x0941'] = '\x0AC1'; // u
            deva2Gujarati['\x0942'] = '\x0AC2'; // uu
            deva2Gujarati['\x0947'] = '\x0AC7'; // e
            deva2Gujarati['\x094B'] = '\x0ACB'; // o

            // various signs
            deva2Gujarati['\x094D'] = '\x0ACD'; // virama

            // we let dandas (U+0964) and double dandas (U+0965) pass through 
            // and handle them in ConvertDandas()

            // numerals
            deva2Gujarati['\x0966'] = '\x0AE6';
            deva2Gujarati['\x0967'] = '\x0AE7';
            deva2Gujarati['\x0968'] = '\x0AE8';
            deva2Gujarati['\x0969'] = '\x0AE9';
            deva2Gujarati['\x096A'] = '\x0AEA';
            deva2Gujarati['\x096B'] = '\x0AEB';
            deva2Gujarati['\x096C'] = '\x0AEC';
            deva2Gujarati['\x096D'] = '\x0AED';
            deva2Gujarati['\x096E'] = '\x0AEE';
            deva2Gujarati['\x096F'] = '\x0AEF';

            // zero-width joiners
            deva2Gujarati['\x200C'] = ""; // ZWNJ (remove)
            deva2Gujarati['\x200D'] = ""; // ZWJ (remove)
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

            // change name of stylesheet for Gujarati
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-gujr.xsl");

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
                if (deva2Gujarati.ContainsKey(c))
                    sb.Append(deva2Gujarati[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public string ConvertDandas(string str)
        {
            // in gathas, single dandas convert to semicolon, double to period
            // Regex note: the +? is the lazy quantifier which finds the shortest match
            str = Regex.Replace(str, "<p rend=\"gatha[a-z0-9]*\">.+?</p>",
                new MatchEvaluator(this.ConvertGathaDandas));

            // remove double dandas around namo tassa
            str = Regex.Replace(str, "<p rend=\"centre\">.+?</p>",
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
