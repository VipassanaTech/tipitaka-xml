using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VRI.CSCD.Conversion
{
    class Deva2Thai
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

                Deva2Thai d2 = new Deva2Thai();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Thai script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2thai input [output]");
        }
        // end static methods


        private Hashtable dev2Thai;

        public Deva2Thai()
        {
            dev2Thai = new Hashtable();

            dev2Thai['\x0902'] = '\x0E4D'; // niggahita

            // independent vowels
            dev2Thai['\x0905'] = '\x0E2D'; // a
            dev2Thai['\x0906'] = "\x0E2D\x0E32"; // aa
            dev2Thai['\x0907'] = "\x0E2D\x0E34"; // i
            dev2Thai['\x0908'] = "\x0E2D\x0E35"; // ii
            dev2Thai['\x0909'] = "\x0E2D\x0E38"; // u
            dev2Thai['\x090A'] = "\x0E2D\x0E39"; // uu
            dev2Thai['\x090F'] = "\x0E40\x0E2D"; // e
            dev2Thai['\x0913'] = "\x0E42\x0E2D"; // o

            // velar stops
            dev2Thai['\x0915'] = '\x0E01'; // ka
            dev2Thai['\x0916'] = '\x0E02'; // kha
            dev2Thai['\x0917'] = '\x0E04'; // ga
            dev2Thai['\x0918'] = '\x0E06'; // gha
            dev2Thai['\x0919'] = '\x0E07'; // n overdot a
            
            // palatal stops
            dev2Thai['\x091A'] = '\x0E08'; // ca
            dev2Thai['\x091B'] = '\x0E09'; // cha
            dev2Thai['\x091C'] = '\x0E0A'; // ja
            dev2Thai['\x091D'] = '\x0E0C'; // jha
            dev2Thai['\x091E'] = '\x0E0D'; // ña

            // retroflex stops
            dev2Thai['\x091F'] = '\x0E0F'; // t underdot a
            dev2Thai['\x0920'] = '\x0E10'; // t underdot ha
            dev2Thai['\x0921'] = '\x0E11'; // d underdot a
            dev2Thai['\x0922'] = '\x0E12'; // d underdot ha
            dev2Thai['\x0923'] = '\x0E13'; // n underdot a

            // dental stops
            dev2Thai['\x0924'] = '\x0E15'; // ta
            dev2Thai['\x0925'] = '\x0E16'; // tha
            dev2Thai['\x0926'] = '\x0E17'; // da
            dev2Thai['\x0927'] = '\x0E18'; // dha
            dev2Thai['\x0928'] = '\x0E19'; // na

            // labial stops
            dev2Thai['\x092A'] = '\x0E1B'; // pa
            dev2Thai['\x092B'] = '\x0E1C'; // pha
            dev2Thai['\x092C'] = '\x0E1E'; // ba
            dev2Thai['\x092D'] = '\x0E20'; // bha
            dev2Thai['\x092E'] = '\x0E21'; // ma

            // liquids, fricatives, etc.
            dev2Thai['\x092F'] = '\x0E22'; // ya
            dev2Thai['\x0930'] = '\x0E23'; // ra
            dev2Thai['\x0932'] = '\x0E25'; // la
            dev2Thai['\x0935'] = '\x0E27'; // va
            dev2Thai['\x0938'] = '\x0E2A'; // sa
            dev2Thai['\x0939'] = '\x0E2B'; // ha
            dev2Thai['\x0933'] = '\x0E2C'; // l underdot a

            // dependent vowel signs
            dev2Thai['\x093E'] = '\x0E32'; // aa
            dev2Thai['\x093F'] = '\x0E34'; // i
            dev2Thai['\x0940'] = '\x0E35'; // ii
            dev2Thai['\x0941'] = '\x0E38'; // u
            dev2Thai['\x0942'] = '\x0E39'; // uu
            dev2Thai['\x0947'] = '\x0E40'; // e
            dev2Thai['\x094B'] = '\x0E42'; // o

            dev2Thai['\x094D'] = '\x0E3A'; // virama

            // numerals
            dev2Thai['\x0966'] = '\x0E50';
            dev2Thai['\x0967'] = '\x0E51';
            dev2Thai['\x0968'] = '\x0E52';
            dev2Thai['\x0969'] = '\x0E53';
            dev2Thai['\x096A'] = '\x0E54';
            dev2Thai['\x096B'] = '\x0E55';
            dev2Thai['\x096C'] = '\x0E56';
            dev2Thai['\x096D'] = '\x0E57';
            dev2Thai['\x096E'] = '\x0E58';
            dev2Thai['\x096F'] = '\x0E59';

            // other
            dev2Thai['\x0970'] = '.'; // Dev. abbreviation sign
            dev2Thai['\x200C'] = ""; // ZWNJ (remove)
            dev2Thai['\x200D'] = ""; // ZWJ (remove)
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

            // change name of stylesheet for Thai
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-thai.xsl");

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
            // first remove all the ZWJs
            devStr = devStr.Replace("\x200D", "");

            // pre-processing step for Thai: put the e vowel before its consonants
            //devStr = Regex.Replace(devStr, "([\x0915-\x0939]\x094D[\x0915-\x0939]\x094D[\x0915-\x0939]\x094D[\x0915-\x0939])\x0947", "\x0E40$1");
            //devStr = Regex.Replace(devStr, "([\x0915-\x0939]\x094D[\x0915-\x0939]\x094D[\x0915-\x0939])\x0947", "\x0E40$1");
            //devStr = Regex.Replace(devStr, "([\x0915-\x0939]\x094D[\x0915-\x0939])\x0947", "\x0E40$1");
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])\x0947", "\x0E40$1");

            // pre-processing step for Thai: put the o vowel before its consonants
            //devStr = Regex.Replace(devStr, "([\x0915-\x0939]\x094D[\x0915-\x0939]\x094D[\x0915-\x0939]\x094D[\x0915-\x0939])\x094B", "\x0E42$1");
            //devStr = Regex.Replace(devStr, "([\x0915-\x0939]\x094D[\x0915-\x0939]\x094D[\x0915-\x0939])\x094B", "\x0E42$1");
            //devStr = Regex.Replace(devStr, "([\x0915-\x0939]\x094D[\x0915-\x0939])\x094B", "\x0E42$1");
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])\x094B", "\x0E42$1");

            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (dev2Thai.ContainsKey(c))
                    sb.Append(dev2Thai[c]);
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
                new MatchEvaluator(this.ConvertNamoTassaDandas));

            // convert all others to Thai paiyannoi
            str = str.Replace("\x0964", "\x0E2F");
            str = str.Replace("\x0965", "\x0E2F");
            return str;
        }

        public string ConvertGathaDandas(Match m)
        {
            string str = m.Value;
            str = str.Replace("\x0964", ";");
            str = str.Replace("\x0965", "\x0E2F");
            return str;
        }

        public string ConvertNamoTassaDandas(Match m)
        {
            string str = m.Value;
            return str.Replace("\x0965", "\x0E2F");
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
