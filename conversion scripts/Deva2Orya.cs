using System;
using System.Collections;
using System.IO;
using System.Text;

namespace VRI.CSCD.Conversion
{
    class Deva2Orya
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

                Deva2Orya d2 = new Deva2Orya();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Oriya (Odia)");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2orya input [output]");
        }
        // end static methods

        private Hashtable deva2Orya;

        // The letter choices follow the Odia transliteration of Abhiraj Bibhar
        // (VipassanaTech/tipitaka-xml#2707): va is WA (U+0B71) and ya is YYA (U+0B5F),
        // everywhere, including after a virama.
        public Deva2Orya()
        {
            deva2Orya = new Hashtable();

            deva2Orya['\x0901'] = '\x0B01'; // candrabindu
            deva2Orya['\x0902'] = '\x0B02'; // niggahita

            // independent vowels
            deva2Orya['\x0905'] = '\x0B05'; // a
            deva2Orya['\x0906'] = '\x0B06'; // aa
            deva2Orya['\x0907'] = '\x0B07'; // i
            deva2Orya['\x0908'] = '\x0B08'; // ii
            deva2Orya['\x0909'] = '\x0B09'; // u
            deva2Orya['\x090A'] = '\x0B0A'; // uu
            deva2Orya['\x090B'] = '\x0B0B'; // vocalic r
            deva2Orya['\x090C'] = '\x0B0C'; // vocalic l
            deva2Orya['\x090F'] = '\x0B0F'; // e
            deva2Orya['\x0910'] = '\x0B10'; // ai
            deva2Orya['\x0913'] = '\x0B13'; // o
            deva2Orya['\x0914'] = '\x0B14'; // au

            // velar stops
            deva2Orya['\x0915'] = '\x0B15'; // ka
            deva2Orya['\x0916'] = '\x0B16'; // kha
            deva2Orya['\x0917'] = '\x0B17'; // ga
            deva2Orya['\x0918'] = '\x0B18'; // gha
            deva2Orya['\x0919'] = '\x0B19'; // n overdot a

            // palatal stops
            deva2Orya['\x091A'] = '\x0B1A'; // ca
            deva2Orya['\x091B'] = '\x0B1B'; // cha
            deva2Orya['\x091C'] = '\x0B1C'; // ja
            deva2Orya['\x091D'] = '\x0B1D'; // jha
            deva2Orya['\x091E'] = '\x0B1E'; // n tilde a

            // retroflex stops
            deva2Orya['\x091F'] = '\x0B1F'; // t underdot a
            deva2Orya['\x0920'] = '\x0B20'; // t underdot ha
            deva2Orya['\x0921'] = '\x0B21'; // d underdot a
            deva2Orya['\x0922'] = '\x0B22'; // d underdot ha
            deva2Orya['\x0923'] = '\x0B23'; // n underdot a

            // dental stops
            deva2Orya['\x0924'] = '\x0B24'; // ta
            deva2Orya['\x0925'] = '\x0B25'; // tha
            deva2Orya['\x0926'] = '\x0B26'; // da
            deva2Orya['\x0927'] = '\x0B27'; // dha
            deva2Orya['\x0928'] = '\x0B28'; // na

            // labial stops
            deva2Orya['\x092A'] = '\x0B2A'; // pa
            deva2Orya['\x092B'] = '\x0B2B'; // pha
            deva2Orya['\x092C'] = '\x0B2C'; // ba
            deva2Orya['\x092D'] = '\x0B2D'; // bha
            deva2Orya['\x092E'] = '\x0B2E'; // ma

            // liquids, fricatives, etc.
            deva2Orya['\x092F'] = '\x0B5F'; // ya *** YYA, not U+0B2F, which is read as ja in Odia ***
            deva2Orya['\x0930'] = '\x0B30'; // ra
            deva2Orya['\x0932'] = '\x0B32'; // la
            deva2Orya['\x0933'] = '\x0B33'; // l underdot a
            deva2Orya['\x0935'] = '\x0B71'; // va *** WA, not U+0B35 ***
            deva2Orya['\x0936'] = '\x0B36'; // sha (palatal)
            deva2Orya['\x0937'] = '\x0B37'; // sha (retroflex)
            deva2Orya['\x0938'] = '\x0B38'; // sa
            deva2Orya['\x0939'] = '\x0B39'; // ha

            // dependent vowel signs
            deva2Orya['\x093E'] = '\x0B3E'; // aa
            deva2Orya['\x093F'] = '\x0B3F'; // i
            deva2Orya['\x0940'] = '\x0B40'; // ii
            deva2Orya['\x0941'] = '\x0B41'; // u
            deva2Orya['\x0942'] = '\x0B42'; // uu
            deva2Orya['\x0943'] = '\x0B43'; // vocalic r
            deva2Orya['\x0947'] = '\x0B47'; // e
            deva2Orya['\x0948'] = '\x0B48'; // ai
            deva2Orya['\x094B'] = '\x0B4B'; // o
            deva2Orya['\x094C'] = '\x0B4C'; // au

            // virama. Odia shows it when no consonant follows (a visible halant),
            // so it needs no special case.
            deva2Orya['\x094D'] = '\x0B4D';

            // Sanskrit letters, as in CST Reader (fsnow/cst #1049)
            deva2Orya['\x0903'] = '\x0B03'; // visarga

            // numerals
            deva2Orya['\x0966'] = '\x0B66';
            deva2Orya['\x0967'] = '\x0B67';
            deva2Orya['\x0968'] = '\x0B68';
            deva2Orya['\x0969'] = '\x0B69';
            deva2Orya['\x096A'] = '\x0B6A';
            deva2Orya['\x096B'] = '\x0B6B';
            deva2Orya['\x096C'] = '\x0B6C';
            deva2Orya['\x096D'] = '\x0B6D';
            deva2Orya['\x096E'] = '\x0B6E';
            deva2Orya['\x096F'] = '\x0B6F';

            // The ZWJ in some conjuncts is for Devanagari presentation only
            deva2Orya['\x200C'] = ""; // ZWNJ (remove)
            deva2Orya['\x200D'] = ""; // ZWJ (remove)

            // The dandas and the abbreviation sign stay Devanagari: Odia has none of its own.
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

            // change name of stylesheet for Odia
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-orya.xsl");

            string str = Convert(devStr);

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.Unicode);
            sw.Write(str);
            sw.Flush();
            sw.Close();
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications or capitalization
        public string Convert(string devStr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Orya.ContainsKey(c))
                    sb.Append(deva2Orya[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
