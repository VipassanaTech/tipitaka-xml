using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Deva2Cyrillic
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
                d2.Convert();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Converts Unicode Devanagari to Unicode Cyrillic");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2cyrl input [output]");
        }
        // end static methods


        private Hashtable deva2Cyrillic;

        public Deva2Cyrl()
        {
            deva2Cyrillic = new Hashtable();

            // velar stops
            deva2Cyrillic['\x0915'] = '\x0433'; // ka
            deva2Cyrillic['\x0916'] = '\x043A'; // kha
            deva2Cyrillic['\x0917'] = "\x0433\x0307"; // ga
            deva2Cyrillic['\x0918'] = "\x0433\x0445"; // gha
            deva2Cyrillic['\x0919'] = "\x043D\x0307"; // n overdot a
            
            // palatal stops
            deva2Cyrillic['\x091A'] = '\x0436'; // ca
            deva2Cyrillic['\x091B'] = '\x0447'; // cha
            deva2Cyrillic['\x091C'] = "\x0436\x0307"; // ja
            deva2Cyrillic['\x091D'] = "\x0436\x0445"; // jha
            deva2Cyrillic['\x091E'] = "\x043D\x0303"; // ña

            // retroflex stops
            deva2Cyrillic['\x091F'] = '\x0434'; // t underdot a
            deva2Cyrillic['\x0920'] = '\x0442'; // t underdot ha
            deva2Cyrillic['\x0921'] = "\x0434\x0323"; // d underdot a
            deva2Cyrillic['\x0922'] = "\x0434\x0445"; // d underdot ha
            deva2Cyrillic['\x0923'] = "\x043D\x0323"; // n underdot a

            // dental stops
            deva2Cyrillic['\x0924'] = "\x0434\x0307"; // ta
            deva2Cyrillic['\x0925'] = "\x0442\x0307"; // tha
            deva2Cyrillic['\x0926'] = "\x0434\x0307\x0323"; // da
            deva2Cyrillic['\x0927'] = "\x0434\x0307\x0445"; // dha
            deva2Cyrillic['\x0928'] = '\x043D'; // na

            // labial stops
            deva2Cyrillic['\x092A'] = '\x0431'; // pa
            deva2Cyrillic['\x092B'] = '\x043F'; // pha
            deva2Cyrillic['\x092C'] = "\x0431\x0323"; // ba
            deva2Cyrillic['\x092D'] = "\x0431\x0445"; // bha
            deva2Cyrillic['\x092E'] = '\x043C'; // ma

            // liquids, fricatives, etc.
            deva2Cyrillic['\x092F'] = '\x044F'; // ya
            deva2Cyrillic['\x0930'] = '\x0440'; // ra
            deva2Cyrillic['\x0932'] = '\x043B'; // la
            deva2Cyrillic['\x0935'] = '\x0432'; // va
            deva2Cyrillic['\x0938'] = '\x0441'; // sa
            deva2Cyrillic['\x0939'] = '\x0445'; // ha
            deva2Cyrillic['\x0933'] = "\x043B\x0323"; // l underdot a

            // independent vowels
            deva2Cyrillic['\x0905'] = '\x0430'; // a
            deva2Cyrillic['\x0906'] = "\x0430\x0430"; // aa
            deva2Cyrillic['\x0907'] = '\x0438'; // i
            deva2Cyrillic['\x0908'] = "\x0438\x0439"; // ii
            deva2Cyrillic['\x0909'] = '\x0443'; // u
            deva2Cyrillic['\x090A'] = "\x0443\x0443"; // uu
            deva2Cyrillic['\x090F'] = '\x0437'; // e
            deva2Cyrillic['\x0913'] = '\x043E'; // o

            // dependent vowel signs
            deva2Cyrillic['\x093E'] = "\x0430\x0430"; // aa
            deva2Cyrillic['\x093F'] = '\x0438'; // i
            deva2Cyrillic['\x0940'] = "\x0438\x0439"; // ii
            deva2Cyrillic['\x0941'] = '\x0443'; // u
            deva2Cyrillic['\x0942'] = "\x0443\x0443"; // uu
            deva2Cyrillic['\x0947'] = '\x0437'; // e
            deva2Cyrillic['\x094B'] = '\x043E'; // o

            // numerals
            deva2Cyrillic['\x0966'] = '0';
            deva2Cyrillic['\x0967'] = '1';
            deva2Cyrillic['\x0968'] = '2';
            deva2Cyrillic['\x0969'] = '3';
            deva2Cyrillic['\x096A'] = '4';
            deva2Cyrillic['\x096B'] = '5';
            deva2Cyrillic['\x096C'] = '6';
            deva2Cyrillic['\x096D'] = '7';
            deva2Cyrillic['\x096E'] = '8';
            deva2Cyrillic['\x096F'] = '9';

            // other
            deva2Cyrillic['\x0964'] = '.'; // danda -> period
            deva2Cyrillic['\x0902'] = "\x043C\x0323"; // niggahita
            deva2Cyrillic['\x094D'] = ""; // virama
            deva2Cyrillic['\x200C'] = ""; // ZWNJ (ignore)
            deva2Cyrillic['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Cyrillic
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-cyrl.xsl");

            // insert Cyrillic 'a' after all consonants that are not followed by virama, dependent vowel or cyrillic a
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])([^\x093E-\x094D\x0430])", "$1\x0430$2");
            devStr = Regex.Replace(devStr, "([\x0915-\x0939])([^\x093E-\x094D\x0430])", "$1\x0430$2");
            // TODO: figure out how to backtrack so this replace doesn't have to be done twice

            // replace Devanagari zero in abbreviations with period
            // (any Dev zero following a Dev letter (or the Cyrillic a from above) is assumed to be an 
            // abbreviation marker)
            devStr = Regex.Replace(devStr, "([\x0430\x0901-\x0963])\x0966", "$1.");

            char[] dev = devStr.ToCharArray();

            foreach (char c in dev)
            {
                if (deva2Cyrillic.ContainsKey(c))
                    sw.Write(deva2Cyrillic[c]);
                else
                    sw.Write(c);
            }

            sw.Flush();
            sw.Close();
            sr.Close();
        }
    }
}
