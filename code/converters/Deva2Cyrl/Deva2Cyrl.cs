using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VRI.CSCD.Conversion
{
    class Deva2Cyrl
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

                Deva2Cyrl d2 = new Deva2Cyrl();
                d2.InputFilePath = args[0];
                d2.OutputFilePath = di.FullName + "\\" + fi.Name;
                /*d2.ParagraphElements = ConfigurationManager.AppSettings["ParagraphElements"].Split(',');
                d2.IgnoreElements = ConfigurationManager.AppSettings["IgnoreElements"].Split(',');
                d2.CapitalMarker = System.Convert.ToChar(
                        System.Convert.ToInt32(
                        ConfigurationManager.AppSettings["CapitalMarker"])
                        ).ToString();*/
                d2.Convert();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Cyrillic");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2cyrl input [output]");
        }
        // end static methods


        private Hashtable deva2Cyrl;

        public Deva2Cyrl()
        {
            deva2Cyrl = new Hashtable();

            deva2Cyrl['\x0902'] = "\x043C\x0323"; // niggahita

            // velar stops
            deva2Cyrl['\x0915'] = '\x0433'; // ka
            deva2Cyrl['\x0916'] = '\x043A'; // kha
            deva2Cyrl['\x0917'] = "\x0433\x0307"; // ga
            deva2Cyrl['\x0918'] = "\x0433\x0445"; // gha
            deva2Cyrl['\x0919'] = "\x043D\x0307"; // n overdot a
            
            // palatal stops
            deva2Cyrl['\x091A'] = '\x0436'; // ca
            deva2Cyrl['\x091B'] = '\x0447'; // cha
            deva2Cyrl['\x091C'] = "\x0436\x0307"; // ja
            deva2Cyrl['\x091D'] = "\x0436\x0445"; // jha
            deva2Cyrl['\x091E'] = "\x043D\x0303"; // ña

            // retroflex stops
            deva2Cyrl['\x091F'] = '\x0434'; // t underdot a
            deva2Cyrl['\x0920'] = '\x0442'; // t underdot ha
            deva2Cyrl['\x0921'] = "\x0434\x0323"; // d underdot a
            deva2Cyrl['\x0922'] = "\x0434\x0445"; // d underdot ha
            deva2Cyrl['\x0923'] = "\x043D\x0323"; // n underdot a

            // dental stops
            deva2Cyrl['\x0924'] = "\x0434\x0307"; // ta
            deva2Cyrl['\x0925'] = "\x0442\x0307"; // tha
            deva2Cyrl['\x0926'] = "\x0434\x0307\x0323"; // da
            deva2Cyrl['\x0927'] = "\x0434\x0307\x0445"; // dha
            deva2Cyrl['\x0928'] = '\x043D'; // na

            // labial stops
            deva2Cyrl['\x092A'] = '\x0431'; // pa
            deva2Cyrl['\x092B'] = '\x043F'; // pha
            deva2Cyrl['\x092C'] = "\x0431\x0323"; // ba
            deva2Cyrl['\x092D'] = "\x0431\x0445"; // bha
            deva2Cyrl['\x092E'] = '\x043C'; // ma

            // liquids, fricatives, etc.
            deva2Cyrl['\x092F'] = '\x044F'; // ya
            deva2Cyrl['\x0930'] = '\x0440'; // ra
            deva2Cyrl['\x0932'] = '\x043B'; // la
            deva2Cyrl['\x0935'] = '\x0432'; // va
            deva2Cyrl['\x0938'] = '\x0441'; // sa
            deva2Cyrl['\x0939'] = '\x0445'; // ha
            deva2Cyrl['\x0933'] = "\x043B\x0323"; // l underdot a

            // independent vowels
            deva2Cyrl['\x0905'] = '\x0430'; // a
            deva2Cyrl['\x0906'] = "\x0430\x0430"; // aa
            deva2Cyrl['\x0907'] = '\x0438'; // i
            deva2Cyrl['\x0908'] = "\x0438\x0439"; // ii
            deva2Cyrl['\x0909'] = '\x0443'; // u
            deva2Cyrl['\x090A'] = "\x0443\x0443"; // uu
            deva2Cyrl['\x090F'] = '\x0437'; // e
            deva2Cyrl['\x0913'] = '\x043E'; // o

            // dependent vowel signs
            deva2Cyrl['\x093E'] = "\x0430\x0430"; // aa
            deva2Cyrl['\x093F'] = '\x0438'; // i
            deva2Cyrl['\x0940'] = "\x0438\x0439"; // ii
            deva2Cyrl['\x0941'] = '\x0443'; // u
            deva2Cyrl['\x0942'] = "\x0443\x0443"; // uu
            deva2Cyrl['\x0947'] = '\x0437'; // e
            deva2Cyrl['\x094B'] = '\x043E'; // o

            // various signs
            deva2Cyrl['\x094D'] = ""; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // numerals
            deva2Cyrl['\x0966'] = '0';
            deva2Cyrl['\x0967'] = '1';
            deva2Cyrl['\x0968'] = '2';
            deva2Cyrl['\x0969'] = '3';
            deva2Cyrl['\x096A'] = '4';
            deva2Cyrl['\x096B'] = '5';
            deva2Cyrl['\x096C'] = '6';
            deva2Cyrl['\x096D'] = '7';
            deva2Cyrl['\x096E'] = '8';
            deva2Cyrl['\x096F'] = '9';

            deva2Cyrl['\x0970'] = "."; // Dev abbreviation sign
            deva2Cyrl['\x200C'] = ""; // ZWNJ (ignore)
            deva2Cyrl['\x200D'] = ""; // ZWJ (ignore)
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

        public string[] IgnoreElements
        {
            get { return ignoreElements; }
            set { ignoreElements = value; }
        }
        private string[] ignoreElements;

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
            sr.Close();

            // change name of stylesheet for Cyrillic
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-cyrl.xsl");

            // mark the Devanagari text for programmatic capitalization
            //Capitalizer capitalizer = new Capitalizer(ParagraphElements, IgnoreElements, CapitalMarker);
            //devStr = capitalizer.MarkCapitals(devStr);

            // remove Dev abbreviation sign before an ellipsis. We don't want a 4th dot after pe.
            devStr = devStr.Replace("\x0970\x2026", "\x2026");

            string str = Convert(devStr);

            //str = capitalizer.Capitalize(str);
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
            // Insert Cyrillic 'a' after all consonants that are not followed by virama, dependent vowel or cyrillic a
            // (This still works after we inserted ZWJ in the Devanagari. The ZWJ goes after virama.)
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])([^\x093E-\x094D\x0430])", "$1\x0430$2");
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])([^\x093E-\x094D\x0430])", "$1\x0430$2");
            // TODO: figure out how to backtrack so this replace doesn't have to be done twice

            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Cyrl.ContainsKey(c))
                    sb.Append(deva2Cyrl[c]);
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
