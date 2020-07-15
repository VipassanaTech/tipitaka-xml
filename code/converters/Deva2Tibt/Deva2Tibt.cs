using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VRI.CSCD.Conversion
{
    class Deva2Tibt
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

                Deva2Tibt d2 = new Deva2Tibt();
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
            Console.WriteLine("Transliterates Unicode Devanagari to Unicode Tibetan script");
            Console.WriteLine("syntax:");
            Console.WriteLine("deva2tibt input [output]");
        }
        // end static methods


        private Hashtable deva2Tibt;

        public Deva2Tibt()
        {
            deva2Tibt = new Hashtable();

            deva2Tibt['\x0902'] = '\x0F7E'; // niggahita

            // independent vowels
            deva2Tibt['\x0905'] = '\x0F68'; // a
            deva2Tibt['\x0906'] = "\x0F68\x0F71"; // aa
            deva2Tibt['\x0907'] = "\x0F68\x0F72"; // i
            deva2Tibt['\x0908'] = "\x0F68\x0F71\x0F72"; // ii
            deva2Tibt['\x0909'] = "\x0F68\x0F74"; // u
            deva2Tibt['\x090A'] = "\x0F68\x0F71\x0F74"; // uu
            deva2Tibt['\x090F'] = "\x0F68\x0F7A"; // e
            deva2Tibt['\x0913'] = "\x0F68\x0F7C"; // o

            // velar stops
            deva2Tibt['\x0915'] = '\x0F40'; // ka
            deva2Tibt['\x0916'] = '\x0F41'; // kha
            deva2Tibt['\x0917'] = '\x0F42'; // ga
            deva2Tibt['\x0918'] = '\x0F43'; // gha
            deva2Tibt['\x0919'] = '\x0F44'; // n overdot a
            
            // palatal stops
            // Note that these 4 stops are represented by the Tibetan
            // tsa, tsha, dza, dzha, not ca, cha, ... (per cfynn. see below)
            deva2Tibt['\x091A'] = '\x0F59'; // ca
            deva2Tibt['\x091B'] = '\x0F5A'; // cha 
            deva2Tibt['\x091C'] = '\x0F5B'; // ja
            deva2Tibt['\x091D'] = '\x0F5C'; // jha  
            deva2Tibt['\x091E'] = '\x0F49'; // ña

            // retroflex stops
            deva2Tibt['\x091F'] = '\x0F4A'; // t underdot a
            deva2Tibt['\x0920'] = '\x0F4B'; // t underdot ha
            deva2Tibt['\x0921'] = '\x0F4C'; // d underdot a
            deva2Tibt['\x0922'] = '\x0F4D'; // d underdot ha
            deva2Tibt['\x0923'] = '\x0F4E'; // n underdot a

            // dental stops
            deva2Tibt['\x0924'] = '\x0F4F'; // ta
            deva2Tibt['\x0925'] = '\x0F50'; // tha
            deva2Tibt['\x0926'] = '\x0F51'; // da
            deva2Tibt['\x0927'] = '\x0F52'; // dha
            deva2Tibt['\x0928'] = '\x0F53'; // na

            // labial stops
            deva2Tibt['\x092A'] = '\x0F54'; // pa
            deva2Tibt['\x092B'] = '\x0F55'; // pha
            deva2Tibt['\x092C'] = '\x0F56'; // ba
            deva2Tibt['\x092D'] = '\x0F57'; // bha
            deva2Tibt['\x092E'] = '\x0F58'; // ma

            // liquids, fricatives, etc.
            deva2Tibt['\x092F'] = '\x0F61'; // ya
            deva2Tibt['\x0930'] = '\x0F62'; // ra
            deva2Tibt['\x0932'] = '\x0F63'; // la
            deva2Tibt['\x0935'] = '\x0F5D'; // va
            deva2Tibt['\x0938'] = '\x0F66'; // sa
            deva2Tibt['\x0939'] = '\x0F67'; // ha
            deva2Tibt['\x0933'] = "\x0F63\x0F39"; // l underdot a (***** PENDING FURTHER RESEARCH BY CFYNN ****)

            // dependent vowel signs
            deva2Tibt['\x093E'] = '\x0F71'; // aa
            deva2Tibt['\x093F'] = '\x0F72'; // i
            deva2Tibt['\x0940'] = "\x0F71\x0F72"; // ii
            deva2Tibt['\x0941'] = "\x0F74"; // u
            deva2Tibt['\x0942'] = "\x0F71\x0F74"; // uu
            deva2Tibt['\x0947'] = '\x0F7A'; // e
            deva2Tibt['\x094B'] = '\x0F7C'; // o

            deva2Tibt['\x094D'] = '\x0F84'; // virama
            deva2Tibt['\x0964'] = '\x0F0D'; // danda
            deva2Tibt['\x0965'] = '\x0F0E'; // double danda

            // numerals
            deva2Tibt['\x0966'] = '\x0F20';
            deva2Tibt['\x0967'] = '\x0F21';
            deva2Tibt['\x0968'] = '\x0F22';
            deva2Tibt['\x0969'] = '\x0F23';
            deva2Tibt['\x096A'] = '\x0F24';
            deva2Tibt['\x096B'] = '\x0F25';
            deva2Tibt['\x096C'] = '\x0F26';
            deva2Tibt['\x096D'] = '\x0F27';
            deva2Tibt['\x096E'] = '\x0F28';
            deva2Tibt['\x096F'] = '\x0F29';

            // zero-width joiners
            deva2Tibt['\x200C'] = ""; // ZWNJ (ignore)
            deva2Tibt['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Tibetan
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-tibt.xsl");

            string str = Convert(devStr);

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.Unicode);
            sw.Write(str);
            sw.Flush();
            sw.Close();
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public string Convert(string devStr)
        {
			// add intersyllabic tsheg between "syllables"
			devStr = Regex.Replace(devStr, "([\x0900-\x094C])([\x0904-\x0939])", "$1\x0F0B$2");
			devStr = Regex.Replace(devStr, "([\x0900-\x094C])([\x0904-\x0939])", "$1\x0F0B$2");

            StringBuilder sb = new StringBuilder();

            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Tibt.ContainsKey(c))
                    sb.Append(deva2Tibt[c]);
                else
                    sb.Append(c);
            }

            string tib = sb.ToString();

            // Iterate over all of the consonants, looking for tibetan halant + consonant.
            // Replace with the corresponding subjoined consonant (without halant)
            for (int i = 0; i <= 39; i++)
            {
                tib = tib.Replace(String.Concat("\x0F84", System.Convert.ToChar(0xF40 + i)),
                    System.Convert.ToChar(0xF90 + i).ToString());
            }

            // exceptions: yya and vva use the "fixed-form subjoined consonants as the 2nd one
            tib = tib.Replace("\x0F61\x0FB1", "\x0F61\x0FBB"); //yya
            tib = tib.Replace("\x0F5D\x0FAD", "\x0F5D\x0FBA"); //vva

            // exceptions: jjha, yha and vha use explicit (visible) halant between
            tib = tib.Replace("\x0F5B\x0FAC", "\x0F5B\x0F84\x0F5C"); //jjha
            tib = tib.Replace("\x0F61\x0FB7", "\x0F61\x0F84\x0F67"); //yha
            tib = tib.Replace("\x0F5D\x0FB7", "\x0F5D\x0F84\x0F67"); //vha

            return tib;
        }
    }
}


/*
Excepts of emails from Chris Fynn:

>> Regarding the jha character missing from the Tibetan palatal group:

It is a little confusing, but the traditional transliteration of Devanagari (Sanskrit) JHA into Tibetan is 
U+0F5C (or U+0F5B U+0FB7). Similarly Devanagari CA is transliterated as U+0F59 in Tibetan script *not* U+0F45, 
Devanagari CHA is U+0F5A *not* U+0F46, and  Devanagari JA is U+0F5B not U+0F47.

Tibetan grammarians held that the Tibetan consonants CA, CHA and JHA (FSnow: he meant JA. There's no JHA.)
(U+0F45, U+0F46, U+0F47) represented Tibetan sounds not found in Indic languages. 
 
>> Regarding independent vowels, Chris sent the following:

U+0905 = U+0FB8
U+0906 = U+0F68 U+0F71
U+0907 = U+0F68 U+0F72
U+0908 = U+0F68 U+0F72
U+0908 = U+0F68 U+0F71 U+0F72 (or U+0F68 U+0F73)
U+0909 = U+0F68 U+0F74
U+090A = U+0F68 U+0F75
U+090B = U+0F62 U+0F80
U+0960 = U+0F62 U+0F71 U+0F80
U+090C = U+0F63 U+0F80
U+0961 = U+0F63 U+0F71 U+0F80
U+090E = U+0F68 U+0F7A
U+090F = U+0F68 U+0F7B
U+0913 = U+0F68 U+0F7C
U+0914 = U+0F68 U+0F7D 
>> FSnow: note that a few of the above vowel characters (e.g. U+0F75) are discouraged by the standard,
>> and should be replaced by a decomposed sequence

>> 0x0933  LLA (this is transliterated to Latin by L with dot below)

I don't think U+0933 has a precise correspondence in Tibetan script. (This Devanagari character also doesn't 
seem to be in the old Indic alphabets from which Buddhist texts were translated into Tibetan.)

If this letter is equivalent to a conjunct of two LAs then you would probably have to write it as 
U+0F63 U+0FB3 in Tibetan script. If it is a unique consonant I think the way to handle it would be to 
transcribe it as U+0F63 U+0F39. Anyway I'll inquire about this this with a monk here who studied Sanskrit 
for fifteen years in Varanasi as he should know for certain. It may take a few days before I'll have an 
opportunity to do this.

*/