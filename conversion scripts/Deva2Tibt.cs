using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dev2Tibetan
{
    class Dev2Tibetan
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

                Dev2Tibetan d2 = new Dev2Tibetan();
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
            Console.WriteLine("Unicode Devanagari to Unicode Tibetan, Pali characters conversion");
            Console.WriteLine("syntax:");
            Console.WriteLine("dev2tib input [output]");
        }
        // end static methods


        private Hashtable dev2Tibetan;

        public Dev2Tibetan()
        {
            dev2Tibetan = new Hashtable();

            // velar stops
            dev2Tibetan['\x0915'] = '\x0F40'; // ka
            dev2Tibetan['\x0916'] = '\x0F41'; // kha
            dev2Tibetan['\x0917'] = '\x0F42'; // ga
            dev2Tibetan['\x0918'] = '\x0F43'; // gha
            dev2Tibetan['\x0919'] = '\x0F44'; // n overdot a
            
            // palatal stops
            // Note that these 4 stops are represented by the Tibetan
            // tsa, tsha, dza, dzha, not ca, cha, ... (per cfynn. see below)
            dev2Tibetan['\x091A'] = '\x0F59'; // ca
            dev2Tibetan['\x091B'] = '\x0F5A'; // cha 
            dev2Tibetan['\x091C'] = '\x0F5B'; // ja
            dev2Tibetan['\x091D'] = '\x0F5C'; // jha  
            dev2Tibetan['\x091E'] = '\x0F49'; // ña

            // retroflex stops
            dev2Tibetan['\x091F'] = '\x0F4A'; // t underdot a
            dev2Tibetan['\x0920'] = '\x0F4B'; // t underdot ha
            dev2Tibetan['\x0921'] = '\x0F4C'; // d underdot a
            dev2Tibetan['\x0922'] = '\x0F4D'; // d underdot ha
            dev2Tibetan['\x0923'] = '\x0F4E'; // n underdot a

            // dental stops
            dev2Tibetan['\x0924'] = '\x0F4F'; // ta
            dev2Tibetan['\x0925'] = '\x0F50'; // tha
            dev2Tibetan['\x0926'] = '\x0F51'; // da
            dev2Tibetan['\x0927'] = '\x0F52'; // dha
            dev2Tibetan['\x0928'] = '\x0F53'; // na

            // labial stops
            dev2Tibetan['\x092A'] = '\x0F54'; // pa
            dev2Tibetan['\x092B'] = '\x0F55'; // pha
            dev2Tibetan['\x092C'] = '\x0F56'; // ba
            dev2Tibetan['\x092D'] = '\x0F57'; // bha
            dev2Tibetan['\x092E'] = '\x0F58'; // ma

            // liquids, fricatives, etc.
            dev2Tibetan['\x092F'] = '\x0F61'; // ya
            dev2Tibetan['\x0930'] = '\x0F62'; // ra
            dev2Tibetan['\x0932'] = '\x0F63'; // la
            dev2Tibetan['\x0935'] = '\x0F5D'; // va
            dev2Tibetan['\x0938'] = '\x0F66'; // sa
            dev2Tibetan['\x0939'] = '\x0F67'; // ha
            dev2Tibetan['\x0933'] = "\x0F63\x0F39"; // l underdot a (***** PENDING FURTHER RESEARCH BY CFYNN ****)

            // independent vowels
            dev2Tibetan['\x0905'] = '\x0F68'; // a
            dev2Tibetan['\x0906'] = "\x0F68\x0F71"; // aa
            dev2Tibetan['\x0907'] = "\x0F68\x0F72"; // i
            dev2Tibetan['\x0908'] = "\x0F68\x0F71\x0F72"; // ii
            dev2Tibetan['\x0909'] = "\x0F68\x0F74"; // u
            dev2Tibetan['\x090A'] = "\x0F68\x0F71\x0F74"; // uu
            dev2Tibetan['\x090F'] = "\x0F68\x0F7B"; // e
            dev2Tibetan['\x0913'] = "\x0F68\x0F7D"; // o

            // dependent vowel signs
            dev2Tibetan['\x093E'] = '\x0F71'; // aa
            dev2Tibetan['\x093F'] = '\x0F72'; // i
            dev2Tibetan['\x0940'] = "\x0F71\x0F72"; // ii
            dev2Tibetan['\x0941'] = "\x0F74"; // u
            dev2Tibetan['\x0942'] = "\x0F71\x0F74"; // uu
            dev2Tibetan['\x0947'] = '\x0F7B'; // e
            dev2Tibetan['\x094B'] = '\x0F7D'; // o

            // numerals
            dev2Tibetan['\x0966'] = '\x0F20';
            dev2Tibetan['\x0967'] = '\x0F21';
            dev2Tibetan['\x0968'] = '\x0F22';
            dev2Tibetan['\x0969'] = '\x0F23';
            dev2Tibetan['\x096A'] = '\x0F24';
            dev2Tibetan['\x096B'] = '\x0F25';
            dev2Tibetan['\x096C'] = '\x0F26';
            dev2Tibetan['\x096D'] = '\x0F27';
            dev2Tibetan['\x096E'] = '\x0F28';
            dev2Tibetan['\x096F'] = '\x0F29';

            // other
            dev2Tibetan['\x0902'] = '\x0F7E'; // niggahita
            dev2Tibetan['\x094D'] = '\x0F84'; // virama
            dev2Tibetan['\x200C'] = ""; // ZWNJ (ignore)
            dev2Tibetan['\x200D'] = ""; // ZWJ (ignore)
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

            // change name of stylesheet for Tibetan
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-tibt.xsl");

            char[] dev = devStr.ToCharArray();
            StringBuilder sb = new StringBuilder();

            foreach (char c in dev)
            {
                if (dev2Tibetan.ContainsKey(c))
                    sb.Append(dev2Tibetan[c]);
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

            sw.Write(tib);
            sw.Flush();
            sw.Close();
            sr.Close();
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