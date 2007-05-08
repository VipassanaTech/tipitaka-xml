using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dev2Malayalam
{
    class Dev2Malayalam
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

                Dev2Malayalam d2 = new Dev2Malayalam();
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
            Console.WriteLine("Unicode Devanagari to Unicode Malayalam, Pali characters conversion");
            Console.WriteLine("syntax:");
            Console.WriteLine("dev2mal input [output]");
        }
        // end static methods


        private Hashtable dev2Malayalam;

        public Dev2Malayalam()
        {
            dev2Malayalam = new Hashtable();

            // various signs
            dev2Malayalam['\x0902'] = '\x0D02'; // anusvara
            dev2Malayalam['\x0903'] = '\x0D03'; // visarga

            // independent vowels
            dev2Malayalam['\x0905'] = '\x0D05'; // a
            dev2Malayalam['\x0906'] = '\x0D06'; // aa
            dev2Malayalam['\x0907'] = '\x0D07'; // i
            dev2Malayalam['\x0908'] = '\x0D08'; // ii
            dev2Malayalam['\x0909'] = '\x0D09'; // u
            dev2Malayalam['\x090A'] = '\x0D0A'; // uu
            dev2Malayalam['\x090B'] = '\x0D0B'; // vocalic r
            dev2Malayalam['\x090C'] = '\x0D0C'; // vocalic l
            dev2Malayalam['\x090F'] = '\x0D0F'; // e
            dev2Malayalam['\x0910'] = '\x0D10'; // ai
            dev2Malayalam['\x0913'] = '\x0D13'; // o
            dev2Malayalam['\x0914'] = '\x0D14'; // au

            // velar stops
            dev2Malayalam['\x0915'] = '\x0D15'; // ka
            dev2Malayalam['\x0916'] = '\x0D16'; // kha
            dev2Malayalam['\x0917'] = '\x0D17'; // ga
            dev2Malayalam['\x0918'] = '\x0D18'; // gha
            dev2Malayalam['\x0919'] = '\x0D19'; // n overdot a
 
            // palatal stops
            dev2Malayalam['\x091A'] = '\x0D1A'; // ca
            dev2Malayalam['\x091B'] = '\x0D1B'; // cha
            dev2Malayalam['\x091C'] = '\x0D1C'; // ja
            dev2Malayalam['\x091D'] = '\x0D1D'; // jha
            dev2Malayalam['\x091E'] = '\x0D1E'; // ña

            // retroflex stops
            dev2Malayalam['\x091F'] = '\x0D1F'; // t underdot a
            dev2Malayalam['\x0920'] = '\x0D20'; // t underdot ha
            dev2Malayalam['\x0921'] = '\x0D21'; // d underdot a
            dev2Malayalam['\x0922'] = '\x0D22'; // d underdot ha
            dev2Malayalam['\x0923'] = '\x0D23'; // n underdot a

            // dental stops
            dev2Malayalam['\x0924'] = '\x0D24'; // ta
            dev2Malayalam['\x0925'] = '\x0D25'; // tha
            dev2Malayalam['\x0926'] = '\x0D26'; // da
            dev2Malayalam['\x0927'] = '\x0D27'; // dha
            dev2Malayalam['\x0928'] = '\x0D28'; // na

            // labial stops
            dev2Malayalam['\x092A'] = '\x0D2A'; // pa
            dev2Malayalam['\x092B'] = '\x0D2B'; // pha
            dev2Malayalam['\x092C'] = '\x0D2C'; // ba
            dev2Malayalam['\x092D'] = '\x0D2D'; // bha
            dev2Malayalam['\x092E'] = '\x0D2E'; // ma

            // liquids, fricatives, etc.
            dev2Malayalam['\x092F'] = '\x0D2F'; // ya
            dev2Malayalam['\x0930'] = '\x0D30'; // ra
            dev2Malayalam['\x0931'] = '\x0D31'; // rra (Dravidian-specific)
            dev2Malayalam['\x0932'] = '\x0D32'; // la
            dev2Malayalam['\x0933'] = '\x0D33'; // l underdot a
            dev2Malayalam['\x0935'] = '\x0D35'; // va
            dev2Malayalam['\x0936'] = '\x0D36'; // sha (palatal)
            dev2Malayalam['\x0937'] = '\x0D37'; // sha (retroflex)
            dev2Malayalam['\x0938'] = '\x0D38'; // sa
            dev2Malayalam['\x0939'] = '\x0D39'; // ha

            // dependent vowel signs
            dev2Malayalam['\x093E'] = '\x0D3E'; // aa
            dev2Malayalam['\x093F'] = '\x0D3F'; // i
            dev2Malayalam['\x0940'] = '\x0D40'; // ii
            dev2Malayalam['\x0941'] = '\x0D41'; // u
            dev2Malayalam['\x0942'] = '\x0D42'; // uu
            dev2Malayalam['\x0943'] = '\x0D43'; // vocalic r
            dev2Malayalam['\x0947'] = '\x0D47'; // e
            dev2Malayalam['\x0948'] = '\x0D48'; // ai
            dev2Malayalam['\x094B'] = '\x0D4B'; // o
            dev2Malayalam['\x094C'] = '\x0D4C'; // au

            // various signs
            dev2Malayalam['\x094D'] = '\x0D4D'; // virama

            // additional vowels for Sanskrit
            dev2Malayalam['\x0960'] = '\x0D60'; // vocalic rr
            dev2Malayalam['\x0961'] = '\x0D61'; // vocalic ll

            // digits
            dev2Malayalam['\x0966'] = '\x0D66';
            dev2Malayalam['\x0967'] = '\x0D67';
            dev2Malayalam['\x0968'] = '\x0D68';
            dev2Malayalam['\x0969'] = '\x0D69';
            dev2Malayalam['\x096A'] = '\x0D6A';
            dev2Malayalam['\x096B'] = '\x0D6B';
            dev2Malayalam['\x096C'] = '\x0D6C';
            dev2Malayalam['\x096D'] = '\x0D6D';
            dev2Malayalam['\x096E'] = '\x0D6E';
            dev2Malayalam['\x096F'] = '\x0D6F';

            // other
            dev2Malayalam['\x0964'] = '.'; // danda -> period
            
            dev2Malayalam['\x200C'] = ""; // ZWNJ (ignore)
            dev2Malayalam['\x200D'] = ""; // ZWJ (ignore)
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

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.BigEndianUnicode);

            // zap the double danda around "namo tassa..."
            devStr = devStr.Replace("\x0964\x0964", "");

            // change name of stylesheet for Gurmukhi
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-mlym.xsl");

            char[] dev = devStr.ToCharArray();

            foreach (char c in dev)
            {
                if (dev2Malayalam.ContainsKey(c))
                    sw.Write(dev2Malayalam[c]);
                else
                    sw.Write(c);
            }

            sw.Flush();
            sw.Close();
            sr.Close();
        }
    }
}
