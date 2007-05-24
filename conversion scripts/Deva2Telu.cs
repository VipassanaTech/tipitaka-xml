using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VRI.CSCD.Conversion
{
    class Dev2Telugu
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

                Dev2Telugu d2 = new Dev2Telugu();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Telugu");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2telu input [output]");
        }
        // end static methods


        private Hashtable dev2Telugu;

        public Dev2Telugu()
        {
            dev2Telugu = new Hashtable();

            dev2Telugu['\x0902'] = '\x0C02'; // niggahita

            // independent vowels
            dev2Telugu['\x0905'] = '\x0C05'; // a
            dev2Telugu['\x0906'] = '\x0C06'; // aa
            dev2Telugu['\x0907'] = '\x0C07'; // i
            dev2Telugu['\x0908'] = '\x0C08'; // ii
            dev2Telugu['\x0909'] = '\x0C09'; // u
            dev2Telugu['\x090A'] = '\x0C0A'; // uu
            dev2Telugu['\x090F'] = '\x0C0E'; // e
            dev2Telugu['\x0913'] = '\x0C12'; // o

            // velar stops
            dev2Telugu['\x0915'] = '\x0C15'; // ka
            dev2Telugu['\x0916'] = '\x0C16'; // kha
            dev2Telugu['\x0917'] = '\x0C17'; // ga
            dev2Telugu['\x0918'] = '\x0C18'; // gha
            dev2Telugu['\x0919'] = '\x0C19'; // n overdot a
            
            // palatal stops
            dev2Telugu['\x091A'] = '\x0C1A'; // ca
            dev2Telugu['\x091B'] = '\x0C1B'; // cha
            dev2Telugu['\x091C'] = '\x0C1C'; // ja
            dev2Telugu['\x091D'] = '\x0C1D'; // jha
            dev2Telugu['\x091E'] = '\x0C1E'; // ña

            // retroflex stops
            dev2Telugu['\x091F'] = '\x0C1F'; // t underdot a
            dev2Telugu['\x0920'] = '\x0C20'; // t underdot ha
            dev2Telugu['\x0921'] = '\x0C21'; // d underdot a
            dev2Telugu['\x0922'] = '\x0C22'; // d underdot ha
            dev2Telugu['\x0923'] = '\x0C23'; // n underdot a

            // dental stops
            dev2Telugu['\x0924'] = '\x0C24'; // ta
            dev2Telugu['\x0925'] = '\x0C25'; // tha
            dev2Telugu['\x0926'] = '\x0C26'; // da
            dev2Telugu['\x0927'] = '\x0C27'; // dha
            dev2Telugu['\x0928'] = '\x0C28'; // na

            // labial stops
            dev2Telugu['\x092A'] = '\x0C2A'; // pa
            dev2Telugu['\x092B'] = '\x0C2B'; // pha
            dev2Telugu['\x092C'] = '\x0C2C'; // ba
            dev2Telugu['\x092D'] = '\x0C2D'; // bha
            dev2Telugu['\x092E'] = '\x0C2E'; // ma

            // liquids, fricatives, etc.
            dev2Telugu['\x092F'] = '\x0C2F'; // ya
            dev2Telugu['\x0930'] = '\x0C30'; // ra
            dev2Telugu['\x0932'] = '\x0C32'; // la
            dev2Telugu['\x0935'] = '\x0C35'; // va
            dev2Telugu['\x0938'] = '\x0C38'; // sa
            dev2Telugu['\x0939'] = '\x0C39'; // ha
            dev2Telugu['\x0933'] = '\x0C33'; // l underdot a

            // dependent vowel signs
            dev2Telugu['\x093E'] = '\x0C3E'; // aa
            dev2Telugu['\x093F'] = '\x0C3F'; // i
            dev2Telugu['\x0940'] = '\x0C40'; // ii
            dev2Telugu['\x0941'] = '\x0C41'; // u
            dev2Telugu['\x0942'] = '\x0C42'; // uu
            dev2Telugu['\x0947'] = '\x0C46'; // e
            dev2Telugu['\x094B'] = '\x0C4A'; // o

            dev2Telugu['\x094D'] = '\x0C4D'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // numerals
            dev2Telugu['\x0966'] = '\x0C66';
            dev2Telugu['\x0967'] = '\x0C67';
            dev2Telugu['\x0968'] = '\x0C68';
            dev2Telugu['\x0969'] = '\x0C69';
            dev2Telugu['\x096A'] = '\x0C6A';
            dev2Telugu['\x096B'] = '\x0C6B';
            dev2Telugu['\x096C'] = '\x0C6C';
            dev2Telugu['\x096D'] = '\x0C6D';
            dev2Telugu['\x096E'] = '\x0C6E';
            dev2Telugu['\x096F'] = '\x0C6F';

            // zero-width joiners
            dev2Telugu['\x200C'] = ""; // ZWNJ (ignore)
            dev2Telugu['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Telugu
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-telu.xsl");

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
                if (dev2Telugu.ContainsKey(c))
                    sb.Append(dev2Telugu[c]);
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
