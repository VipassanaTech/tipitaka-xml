using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class Deva2Ipe
    {
        private static IDictionary<string, string> deva2Ipe;

        static Deva2Ipe()
        {
            deva2Ipe = new Dictionary<string, string>();

            deva2Ipe["\u0902"] = "\u00C0"; // niggahita

            // independent vowels
            deva2Ipe["\u00C1"] = "\u00C1"; // a (the IPE inherent "a" is inserted by a regex. let it pass through.)
            deva2Ipe["\u0905"] = "\u00C1"; // a
            deva2Ipe["\u0906"] = "\u00C2"; // aa
            deva2Ipe["\u0907"] = "\u00C3"; // i
            deva2Ipe["\u0908"] = "\u00C4"; // ii
            deva2Ipe["\u0909"] = "\u00C5"; // u
            deva2Ipe["\u090A"] = "\u00C6"; // uu
            deva2Ipe["\u090F"] = "\u00C7"; // e
            deva2Ipe["\u0913"] = "\u00C8"; // o

            // velar stops
            deva2Ipe["\u0915"] = "\u00C9"; // ka
            deva2Ipe["\u0916"] = "\u00CA"; // kha
            deva2Ipe["\u0917"] = "\u00CB"; // ga
            deva2Ipe["\u0918"] = "\u00CC"; // gha
            deva2Ipe["\u0919"] = "\u00CD"; // n overdot a

            // palatal stops
            deva2Ipe["\u091A"] = "\u00CE"; // ca
            deva2Ipe["\u091B"] = "\u00CF"; // cha
            deva2Ipe["\u091C"] = "\u00D0"; // ja
            deva2Ipe["\u091D"] = "\u00D1"; // jha
            deva2Ipe["\u091E"] = "\u00D2"; // n tilde a

            // retroflex stops
            deva2Ipe["\u091F"] = "\u00D3"; // t underdot a
            deva2Ipe["\u0920"] = "\u00D4"; // t underdot ha
            deva2Ipe["\u0921"] = "\u00D5"; // d underdot a
            deva2Ipe["\u0922"] = "\u00D6"; // d underdot ha
            // don"t use D7 multiplication sign
            deva2Ipe["\u0923"] = "\u00D8"; // n underdot a

            // dental stops
            deva2Ipe["\u0924"] = "\u00D9"; // ta
            deva2Ipe["\u0925"] = "\u00DA"; // tha
            deva2Ipe["\u0926"] = "\u00DB"; // da
            deva2Ipe["\u0927"] = "\u00DC"; // dha
            deva2Ipe["\u0928"] = "\u00DD"; // na

            // labial stops
            deva2Ipe["\u092A"] = "\u00DE"; // pa
            deva2Ipe["\u092B"] = "\u00DF"; // pha
            deva2Ipe["\u092C"] = "\u00E0"; // ba
            deva2Ipe["\u092D"] = "\u00E1"; // bha
            deva2Ipe["\u092E"] = "\u00E2"; // ma

            // liquids, fricatives, etc.
            deva2Ipe["\u092F"] = "\u00E3"; // ya
            deva2Ipe["\u0930"] = "\u00E4"; // ra
            deva2Ipe["\u0932"] = "\u00E5"; // la
            deva2Ipe["\u0935"] = "\u00E6"; // va
            deva2Ipe["\u0938"] = "\u00E7"; // sa
            deva2Ipe["\u0939"] = "\u00E8"; // ha
            deva2Ipe["\u0933"] = "\u00E9"; // l underdot a

            // dependent vowel signs
            deva2Ipe["\u093E"] = "\u00C2"; // aa
            deva2Ipe["\u093F"] = "\u00C3"; // i
            deva2Ipe["\u0940"] = "\u00C4"; // ii
            deva2Ipe["\u0941"] = "\u00C5"; // u
            deva2Ipe["\u0942"] = "\u00C6"; // uu
            deva2Ipe["\u0947"] = "\u00C7"; // e
            deva2Ipe["\u094B"] = "\u00C8"; // o

            deva2Ipe["\u094D"] = ""; // virama
            deva2Ipe["\u200C"] = ""; // ZWNJ (ignore)
            deva2Ipe["\u200D"] = ""; // ZWJ (ignore)
        }

        public static string Convert(string devStr)
        {
            // insert "a" after all consonants that are not followed by virama, dependent vowel or "a"
            // (This still works after we inserted ZWJ in the Devanagari. The ZWJ goes after virama.)
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])([^\u093E-\u094D\u00C1])", "$1\u00C1$2");
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])([^\u093E-\u094D\u00C1])", "$1\u00C1$2");
            // TODO: figure out how to backtrack so this replace doesn"t have to be done twice

            // insert a after consonant that is at end of string
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])$", "$1\u00C1");

            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr)
            {
                if (deva2Ipe.ContainsKey(c.ToString()))
                    sb.Append(deva2Ipe[c.ToString()]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
