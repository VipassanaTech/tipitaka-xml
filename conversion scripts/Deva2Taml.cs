using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public class Deva2Taml
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

                Deva2Taml d2 = new Deva2Taml();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Tamil script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2taml input [output]");
        }
        // end static methods


        private Hashtable deva2Taml;

        public Deva2Taml()
        {
            deva2Taml = new Hashtable();

            // various signs
            deva2Taml['\x0902'] = "\x0B99\x0BCD"; // anusvara -> Tamil nga + virama
            deva2Taml['\x0903'] = '\x0B83'; // visarga

            // independent vowels
            deva2Taml['\x0905'] = '\x0B85'; // a
            deva2Taml['\x0906'] = '\x0B86'; // aa
            deva2Taml['\x0907'] = '\x0B87'; // i
            deva2Taml['\x0908'] = '\x0B88'; // ii
            deva2Taml['\x0909'] = '\x0B89'; // u
            deva2Taml['\x090A'] = '\x0B8A'; // uu
            //deva2Taml['\x090B'] = '\x0B8B'; // vocalic r
            //deva2Taml['\x090C'] = '\x0B8C'; // vocalic l
			deva2Taml['\x1200'] = '\x0B8E'; // e (short, replaces the placeholder inserted by regex)
            deva2Taml['\x090F'] = '\x0B8F'; // e
            deva2Taml['\x0910'] = '\x0B90'; // ai
			deva2Taml['\x1201'] = '\x0B92'; // 0 (short, replaces the placeholder inserted by regex)
            deva2Taml['\x0913'] = '\x0B93'; // o
            deva2Taml['\x0914'] = '\x0B94'; // au

            // velar stops
            deva2Taml['\x0915'] = '\x0B95'; // ka
            deva2Taml['\x0916'] = "\x0B95\x00B2"; // kha
            deva2Taml['\x0917'] = "\x0B95\x00B3"; // ga
            deva2Taml['\x0918'] = "\x0B95\x2074"; // gha
            deva2Taml['\x0919'] = '\x0B99'; // n overdot a
 
            // palatal stops
            deva2Taml['\x091A'] = '\x0B9A'; // ca
            deva2Taml['\x091B'] = "\x0B9A\x00B2"; // cha
            deva2Taml['\x091C'] = "\x0B9C"; // ja
            deva2Taml['\x091D'] = "\x0B9C\x00B2"; // jha
            deva2Taml['\x091E'] = '\x0B9E'; // ña

            // retroflex stops
            deva2Taml['\x091F'] = '\x0B9F'; // t underdot a
            deva2Taml['\x0920'] = "\x0B9F\x00B2"; // t underdot ha
            deva2Taml['\x0921'] = "\x0B9F\x00B3"; // d underdot a
            deva2Taml['\x0922'] = "\x0B9F\x2074"; // d underdot ha
            deva2Taml['\x0923'] = '\x0BA3'; // n underdot a

            // dental stops
            deva2Taml['\x0924'] = '\x0BA4'; // ta
            deva2Taml['\x0925'] = "\x0BA4\x00B2"; // tha
            deva2Taml['\x0926'] = "\x0BA4\x00B3"; // da
            deva2Taml['\x0927'] = "\x0BA4\x2074"; // dha
            deva2Taml['\x0928'] = '\x0BA8'; // na

            // labial stops
            deva2Taml['\x092A'] = '\x0BAA'; // pa
            deva2Taml['\x092B'] = "\x0BAA\x00B2"; // pha
            deva2Taml['\x092C'] = "\x0BAA\x00B3"; // ba
            deva2Taml['\x092D'] = "\x0BAA\x2074"; // bha
            deva2Taml['\x092E'] = '\x0BAE'; // ma

            // liquids, fricatives, etc.
            deva2Taml['\x092F'] = '\x0BAF'; // ya
            deva2Taml['\x0930'] = '\x0BB0'; // ra
            deva2Taml['\x0931'] = '\x0BB1'; // rra (Dravidian-specific)
            deva2Taml['\x0932'] = '\x0BB2'; // la
            deva2Taml['\x0933'] = '\x0BB3'; // l underdot a
            deva2Taml['\x0935'] = '\x0BB5'; // va
            deva2Taml['\x0936'] = '\x0BB6'; // sha (palatal)
            deva2Taml['\x0937'] = '\x0BB7'; // sha (retroflex)
            deva2Taml['\x0938'] = '\x0BB8'; // sa
            deva2Taml['\x0939'] = '\x0BB9'; // ha

            // dependent vowel signs
            deva2Taml['\x093E'] = '\x0BBE'; // aa
            deva2Taml['\x093F'] = '\x0BBF'; // i
            deva2Taml['\x0940'] = '\x0BC0'; // ii
            deva2Taml['\x0941'] = '\x0BC1'; // u
            deva2Taml['\x0942'] = '\x0BC2'; // uu
            deva2Taml['\x0943'] = '\x0BC3'; // vocalic r
			deva2Taml['\x1202'] = '\x0BC6'; // e (short, replaces the placeholder inserted by regex)
            deva2Taml['\x0947'] = '\x0BC7'; // e
            deva2Taml['\x0948'] = '\x0BC8'; // ai
			deva2Taml['\x1203'] = '\x0BCA'; // o (short, replaces the placeholder inserted by regex)
            deva2Taml['\x094B'] = '\x0BCB'; // o
            deva2Taml['\x094C'] = '\x0BCC'; // au

            // various signs
            deva2Taml['\x094D'] = '\x0BCD'; // virama

            // we let dandas (U+0964) and double dandas (U+0965) pass through 
            // and handle them in ConvertDandas()

            // digits
            deva2Taml['\x0966'] = '\x0BE6';
            deva2Taml['\x0967'] = '\x0BE7';
            deva2Taml['\x0968'] = '\x0BE8';
            deva2Taml['\x0969'] = '\x0BE9';
            deva2Taml['\x096A'] = '\x0BEA';
            deva2Taml['\x096B'] = '\x0BEB';
            deva2Taml['\x096C'] = '\x0BEC';
            deva2Taml['\x096D'] = '\x0BED';
            deva2Taml['\x096E'] = '\x0BEE';
            deva2Taml['\x096F'] = '\x0BEF';

            // zero-width joiners
            deva2Taml['\x200C'] = ""; // ZWNJ (remove)
            deva2Taml['\x200D'] = ""; // ZWJ (remove)
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

            // change name of stylesheet for Gurmukhi
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-taml.xsl");

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
			// change independent e vowel to placeholder (\x1200) if followed by a double consonant
			devStr = Regex.Replace(devStr, "\x090F([\x0915-\x0939]\x094D[\x0915-\x0939])", "\x1200$1");

			// change independent o vowel to something else if followed by a double consonant
			devStr = Regex.Replace(devStr, "\x0913([\x0915-\x0939]\x094D[\x0915-\x0939])", "\x1201$1");

			// change dependent e vowel to something else if followed by a double consonant
			devStr = Regex.Replace(devStr, "\x0947([\x0915-\x0939]\x094D[\x0915-\x0939])", "\x1202$1");

			// change dependent o vowel to something else if followed by a double consonant
			devStr = Regex.Replace(devStr, "\x094B([\x0915-\x0939]\x094D[\x0915-\x0939])", "\x1203$1");

            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Taml.ContainsKey(c))
                    sb.Append(deva2Taml[c]);
                else
                    sb.Append(c);
            }

			// move superscript after following dependent vowel
			string str = sb.ToString();
			str = Regex.Replace(str, "\x00B2([\x0BBE-\x0BCC])", "$1\x00B2");
			str = Regex.Replace(str, "\x00B3([\x0BBE-\x0BCC])", "$1\x00B3");
			str = Regex.Replace(str, "\x2074([\x0BBE-\x0BCC])", "$1\x2074");

			// move superscript after following virama
			str = Regex.Replace(str, "\x00B2\x0BCD", "\x0BCD\x00B2");
			str = Regex.Replace(str, "\x00B3\x0BCD", "\x0BCD\x00B3");
			str = Regex.Replace(str, "\x2074\x0BCD", "\x0BCD\x2074");

			// if "n" (0x0BA8) is preceded by any word character, change it to the other "n" (0x0BA9 TAMIL NNNA)
			str = Regex.Replace(str, "([\x0B82-\x0BCD\x00B2\x00B3\x2074])\x0BA8", "$1\x0BA9");

			// except if it's before a dental stop
			str = Regex.Replace(str, "\x0BA9\x0BCD\x0BA4", "\x0BA8\x0BCD\x0BA4");

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
