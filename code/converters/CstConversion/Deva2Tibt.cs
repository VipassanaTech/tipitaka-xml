using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class Deva2Tibt
    {
        private static IDictionary<char, object> deva2Tibt;

        static Deva2Tibt()
        {
            deva2Tibt = new Dictionary<char, object>();

            deva2Tibt['\u0902'] = '\u0F7E'; // niggahita

            // independent vowels
            deva2Tibt['\u0905'] = '\u0F68'; // a
            deva2Tibt['\u0906'] = "\u0F68\u0F71"; // aa
            deva2Tibt['\u0907'] = "\u0F68\u0F72"; // i
            deva2Tibt['\u0908'] = "\u0F68\u0F71\u0F72"; // ii
            deva2Tibt['\u0909'] = "\u0F68\u0F74"; // u
            deva2Tibt['\u090A'] = "\u0F68\u0F71\u0F74"; // uu
            deva2Tibt['\u090F'] = "\u0F68\u0F7A"; // e
            deva2Tibt['\u0913'] = "\u0F68\u0F7C"; // o

            // velar stops
            deva2Tibt['\u0915'] = '\u0F40'; // ka
            deva2Tibt['\u0916'] = '\u0F41'; // kha
            deva2Tibt['\u0917'] = '\u0F42'; // ga
            deva2Tibt['\u0918'] = '\u0F43'; // gha
            deva2Tibt['\u0919'] = '\u0F44'; // n overdot a
            
            // palatal stops
            // Note that these 4 stops are represented by the Tibetan
            // tsa, tsha, dza, dzha, not ca, cha, ... (per cfynn. see below)
            deva2Tibt['\u091A'] = '\u0F59'; // ca
            deva2Tibt['\u091B'] = '\u0F5A'; // cha 
            deva2Tibt['\u091C'] = '\u0F5B'; // ja
            deva2Tibt['\u091D'] = '\u0F5C'; // jha  
            deva2Tibt['\u091E'] = '\u0F49'; // n tilde a

            // retroflex stops
            deva2Tibt['\u091F'] = '\u0F4A'; // t underdot a
            deva2Tibt['\u0920'] = '\u0F4B'; // t underdot ha
            deva2Tibt['\u0921'] = '\u0F4C'; // d underdot a
            deva2Tibt['\u0922'] = '\u0F4D'; // d underdot ha
            deva2Tibt['\u0923'] = '\u0F4E'; // n underdot a

            // dental stops
            deva2Tibt['\u0924'] = '\u0F4F'; // ta
            deva2Tibt['\u0925'] = '\u0F50'; // tha
            deva2Tibt['\u0926'] = '\u0F51'; // da
            deva2Tibt['\u0927'] = '\u0F52'; // dha
            deva2Tibt['\u0928'] = '\u0F53'; // na

            // labial stops
            deva2Tibt['\u092A'] = '\u0F54'; // pa
            deva2Tibt['\u092B'] = '\u0F55'; // pha
            deva2Tibt['\u092C'] = '\u0F56'; // ba
            deva2Tibt['\u092D'] = '\u0F57'; // bha
            deva2Tibt['\u092E'] = '\u0F58'; // ma

            // liquids, fricatives, etc.
            deva2Tibt['\u092F'] = '\u0F61'; // ya
            deva2Tibt['\u0930'] = '\u0F62'; // ra
            deva2Tibt['\u0932'] = '\u0F63'; // la
            deva2Tibt['\u0935'] = '\u0F5D'; // va
            deva2Tibt['\u0938'] = '\u0F66'; // sa
            deva2Tibt['\u0939'] = '\u0F67'; // ha
            deva2Tibt['\u0933'] = "\u0F63\u0F39"; // l underdot a (***** PENDING FURTHER RESEARCH BY CFYNN ****)

            // dependent vowel signs
            deva2Tibt['\u093E'] = '\u0F71'; // aa
            deva2Tibt['\u093F'] = '\u0F72'; // i
            deva2Tibt['\u0940'] = "\u0F71\u0F72"; // ii
            deva2Tibt['\u0941'] = "\u0F74"; // u
            deva2Tibt['\u0942'] = "\u0F71\u0F74"; // uu
            deva2Tibt['\u0947'] = '\u0F7A'; // e
            deva2Tibt['\u094B'] = '\u0F7C'; // o

            deva2Tibt['\u094D'] = '\u0F84'; // virama
            deva2Tibt['\u0964'] = '\u0F0D'; // danda
            deva2Tibt['\u0965'] = '\u0F0E'; // double danda

            // numerals
            deva2Tibt['\u0966'] = '\u0F20';
            deva2Tibt['\u0967'] = '\u0F21';
            deva2Tibt['\u0968'] = '\u0F22';
            deva2Tibt['\u0969'] = '\u0F23';
            deva2Tibt['\u096A'] = '\u0F24';
            deva2Tibt['\u096B'] = '\u0F25';
            deva2Tibt['\u096C'] = '\u0F26';
            deva2Tibt['\u096D'] = '\u0F27';
            deva2Tibt['\u096E'] = '\u0F28';
            deva2Tibt['\u096F'] = '\u0F29';

            // zero-width joiners
            deva2Tibt['\u200C'] = ""; // ZWNJ (ignore)
            deva2Tibt['\u200D'] = ""; // ZWJ (ignore)
        }

        public static string ConvertBook(string devStr)
        {
            // change name of stylesheet for Tibetan
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-tibt.xsl");

            return Convert(devStr);
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public static string Convert(string devStr)
        {
			// add intersyllabic tsheg between "syllables".
			devStr = Regex.Replace(devStr, "([\u0900-\u094C])([\u0904-\u0939])", "$1\u0F0B$2");
			devStr = Regex.Replace(devStr, "([\u0900-\u094C])([\u0904-\u0939])", "$1\u0F0B$2");

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
                tib = tib.Replace(String.Concat("\u0F84", System.Convert.ToChar(0xF40 + i)),
                    System.Convert.ToChar(0xF90 + i).ToString());
            }

            // exceptions: yya and vva use the "fixed-form subjoined consonants as the 2nd one
            tib = tib.Replace("\u0F61\u0FB1", "\u0F61\u0FBB"); //yya
            tib = tib.Replace("\u0F5D\u0FAD", "\u0F5D\u0FBA"); //vva

            // exceptions: jjha, yha and vha use explicit (visible) halant between
            tib = tib.Replace("\u0F5B\u0FAC", "\u0F5B\u0F84\u0F5C"); //jjha
            tib = tib.Replace("\u0F61\u0FB7", "\u0F61\u0F84\u0F67"); //yha
            tib = tib.Replace("\u0F5D\u0FB7", "\u0F5D\u0F84\u0F67"); //vha

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