using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class Deva2Khmr
    {
        private static IDictionary<char, object> dev2Khmer;

        static Deva2Khmr()
        {
            dev2Khmer = new Dictionary<char, object>();

            dev2Khmer['\u0902'] = '\u17C6'; // niggahita

            // independent vowels
            dev2Khmer['\u0905'] = '\u17A2'; // a
            dev2Khmer['\u0906'] = "\u17A2\u17B6"; // aa
            dev2Khmer['\u0907'] = '\u17A5'; // i
            dev2Khmer['\u0908'] = '\u17A6'; // ii
            dev2Khmer['\u0909'] = '\u17A7'; // u
            dev2Khmer['\u090A'] = '\u17A9'; // uu
            dev2Khmer['\u090F'] = '\u17AF'; // e
            dev2Khmer['\u0913'] = '\u17B1'; // o

            // velar stops
            dev2Khmer['\u0915'] = '\u1780'; // ka
            dev2Khmer['\u0916'] = '\u1781'; // kha
            dev2Khmer['\u0917'] = '\u1782'; // ga
            dev2Khmer['\u0918'] = '\u1783'; // gha
            dev2Khmer['\u0919'] = '\u1784'; // n overdot a
            
            // palatal stops
            dev2Khmer['\u091A'] = '\u1785'; // ca
            dev2Khmer['\u091B'] = '\u1786'; // cha
            dev2Khmer['\u091C'] = '\u1787'; // ja
            dev2Khmer['\u091D'] = '\u1788'; // jha
            dev2Khmer['\u091E'] = '\u1789'; // n tilde a

            // retroflex stops
            dev2Khmer['\u091F'] = '\u178A'; // t underdot a
            dev2Khmer['\u0920'] = '\u178B'; // t underdot ha
            dev2Khmer['\u0921'] = '\u178C'; // d underdot a
            dev2Khmer['\u0922'] = '\u178D'; // d underdot ha
            dev2Khmer['\u0923'] = '\u178E'; // n underdot a

            // dental stops
            dev2Khmer['\u0924'] = '\u178F'; // ta
            dev2Khmer['\u0925'] = '\u1790'; // tha
            dev2Khmer['\u0926'] = '\u1791'; // da
            dev2Khmer['\u0927'] = '\u1792'; // dha
            dev2Khmer['\u0928'] = '\u1793'; // na

            // labial stops
            dev2Khmer['\u092A'] = '\u1794'; // pa
            dev2Khmer['\u092B'] = '\u1795'; // pha
            dev2Khmer['\u092C'] = '\u1796'; // ba
            dev2Khmer['\u092D'] = '\u1797'; // bha
            dev2Khmer['\u092E'] = '\u1798'; // ma

            // liquids, fricatives, etc.
            dev2Khmer['\u092F'] = '\u1799'; // ya
            dev2Khmer['\u0930'] = '\u179A'; // ra
            dev2Khmer['\u0932'] = '\u179B'; // la
            dev2Khmer['\u0935'] = '\u179C'; // va
            dev2Khmer['\u0938'] = '\u179F'; // sa
            dev2Khmer['\u0939'] = '\u17A0'; // ha
            dev2Khmer['\u0933'] = '\u17A1'; // l underdot a

            // dependent vowel signs
            dev2Khmer['\u093E'] = '\u17B6'; // aa
            dev2Khmer['\u093F'] = '\u17B7'; // i
            dev2Khmer['\u0940'] = '\u17B8'; // ii
            dev2Khmer['\u0941'] = '\u17BB'; // u
            dev2Khmer['\u0942'] = '\u17BC'; // uu
            dev2Khmer['\u0947'] = '\u17C1'; // e
            dev2Khmer['\u094B'] = '\u17C4'; // o

            dev2Khmer['\u094D'] = '\u17D2'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            dev2Khmer['\u0966'] = '\u17E0';
            dev2Khmer['\u0967'] = '\u17E1';
            dev2Khmer['\u0968'] = '\u17E2';
            dev2Khmer['\u0969'] = '\u17E3';
            dev2Khmer['\u096A'] = '\u17E4';
            dev2Khmer['\u096B'] = '\u17E5';
            dev2Khmer['\u096C'] = '\u17E6';
            dev2Khmer['\u096D'] = '\u17E7';
            dev2Khmer['\u096E'] = '\u17E8';
            dev2Khmer['\u096F'] = '\u17E9';

            dev2Khmer['\u0970'] = "."; // Dev abbreviation sign
            dev2Khmer['\u200C'] = ""; // ZWNJ (ignore)
            dev2Khmer['\u200D'] = ""; // ZWJ (ignore)
        }

        public static string ConvertBook(string devStr)
        {
            // change name of stylesheet for Khmer
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-khmr.xsl");

            string str = Convert(devStr);

            str = ConvertDandas(str);
            str = CleanupPunctuation(str);

            return str;
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public static string Convert(string devStr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (dev2Khmer.ContainsKey(c))
                    sb.Append(dev2Khmer[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static string ConvertDandas(string str)
        {
            // in gathas, single dandas convert to semicolon, double to period
            // Regex note: the +? is the lazy quantifier which finds the shortest match
            str = Regex.Replace(str, "<p rend=\"gatha[a-z0-9]*\".+?</p>",
                new MatchEvaluator(ConvertGathaDandas));

            // remove double dandas around namo tassa
            str = Regex.Replace(str, "<p rend=\"centre\".+?</p>",
                new MatchEvaluator(ConvertNamoTassaDandas));

            // convert all others to KHAN
            str = str.Replace("\u0964", "\u17D4");
            str = str.Replace("\u0965", "\u17D4");
            return str;
        }

        public static string ConvertGathaDandas(Match m)
        {
            string str = m.Value;
            str = str.Replace("\u0964", ";");
            str = str.Replace("\u0965", "\u17D4");
            return str;
        }

        public static string ConvertNamoTassaDandas(Match m)
        {
            string str = m.Value;
            return str.Replace("\u0965", "\u17D4");
        }

        // There should be no spaces before these
        // punctuation marks. 
        public static string CleanupPunctuation(string str)
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
