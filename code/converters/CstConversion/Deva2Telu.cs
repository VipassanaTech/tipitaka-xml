using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class Deva2Telu
    {
        private static IDictionary<char, object> dev2Telugu;

        static Deva2Telu()
        {
            dev2Telugu = new Dictionary<char, object>();

            dev2Telugu['\u0902'] = '\u0C02'; // niggahita

            // independent vowels
            dev2Telugu['\u0905'] = '\u0C05'; // a
            dev2Telugu['\u0906'] = '\u0C06'; // aa
            dev2Telugu['\u0907'] = '\u0C07'; // i
            dev2Telugu['\u0908'] = '\u0C08'; // ii
            dev2Telugu['\u0909'] = '\u0C09'; // u
            dev2Telugu['\u090A'] = '\u0C0A'; // uu
            dev2Telugu['\u090F'] = '\u0C0F'; // e
            dev2Telugu['\u0913'] = '\u0C13'; // o

            // velar stops
            dev2Telugu['\u0915'] = '\u0C15'; // ka
            dev2Telugu['\u0916'] = '\u0C16'; // kha
            dev2Telugu['\u0917'] = '\u0C17'; // ga
            dev2Telugu['\u0918'] = '\u0C18'; // gha
            dev2Telugu['\u0919'] = '\u0C19'; // n overdot a
            
            // palatal stops
            dev2Telugu['\u091A'] = '\u0C1A'; // ca
            dev2Telugu['\u091B'] = '\u0C1B'; // cha
            dev2Telugu['\u091C'] = '\u0C1C'; // ja
            dev2Telugu['\u091D'] = '\u0C1D'; // jha
            dev2Telugu['\u091E'] = '\u0C1E'; // n tilde a

            // retroflex stops
            dev2Telugu['\u091F'] = '\u0C1F'; // t underdot a
            dev2Telugu['\u0920'] = '\u0C20'; // t underdot ha
            dev2Telugu['\u0921'] = '\u0C21'; // d underdot a
            dev2Telugu['\u0922'] = '\u0C22'; // d underdot ha
            dev2Telugu['\u0923'] = '\u0C23'; // n underdot a

            // dental stops
            dev2Telugu['\u0924'] = '\u0C24'; // ta
            dev2Telugu['\u0925'] = '\u0C25'; // tha
            dev2Telugu['\u0926'] = '\u0C26'; // da
            dev2Telugu['\u0927'] = '\u0C27'; // dha
            dev2Telugu['\u0928'] = '\u0C28'; // na

            // labial stops
            dev2Telugu['\u092A'] = '\u0C2A'; // pa
            dev2Telugu['\u092B'] = '\u0C2B'; // pha
            dev2Telugu['\u092C'] = '\u0C2C'; // ba
            dev2Telugu['\u092D'] = '\u0C2D'; // bha
            dev2Telugu['\u092E'] = '\u0C2E'; // ma

            // liquids, fricatives, etc.
            dev2Telugu['\u092F'] = '\u0C2F'; // ya
            dev2Telugu['\u0930'] = '\u0C30'; // ra
            dev2Telugu['\u0932'] = '\u0C32'; // la
            dev2Telugu['\u0935'] = '\u0C35'; // va
            dev2Telugu['\u0938'] = '\u0C38'; // sa
            dev2Telugu['\u0939'] = '\u0C39'; // ha
            dev2Telugu['\u0933'] = '\u0C33'; // l underdot a

            // dependent vowel signs
            dev2Telugu['\u093E'] = '\u0C3E'; // aa
            dev2Telugu['\u093F'] = '\u0C3F'; // i
            dev2Telugu['\u0940'] = '\u0C40'; // ii
            dev2Telugu['\u0941'] = '\u0C41'; // u
            dev2Telugu['\u0942'] = '\u0C42'; // uu
            dev2Telugu['\u0947'] = '\u0C47'; // e
            dev2Telugu['\u094B'] = '\u0C4B'; // o

            dev2Telugu['\u094D'] = '\u0C4D'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // numerals
            dev2Telugu['\u0966'] = '\u0C66';
            dev2Telugu['\u0967'] = '\u0C67';
            dev2Telugu['\u0968'] = '\u0C68';
            dev2Telugu['\u0969'] = '\u0C69';
            dev2Telugu['\u096A'] = '\u0C6A';
            dev2Telugu['\u096B'] = '\u0C6B';
            dev2Telugu['\u096C'] = '\u0C6C';
            dev2Telugu['\u096D'] = '\u0C6D';
            dev2Telugu['\u096E'] = '\u0C6E';
            dev2Telugu['\u096F'] = '\u0C6F';

            // zero-width joiners
            dev2Telugu['\u200C'] = ""; // ZWNJ (ignore)
            dev2Telugu['\u200D'] = ""; // ZWJ (ignore)
        }

        public static string ConvertBook(string devStr)
        {
            // change name of stylesheet for Telugu
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-telu.xsl");

            string str = Convert(devStr);

            str = ConvertDandas(str);
            return CleanupPunctuation(str);
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public static string Convert(string devStr)
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

        public static string ConvertDandas(string str)
        {
            // in gathas, single dandas convert to semicolon, double to period
            // Regex note: the +? is the lazy quantifier which finds the shortest match
            str = Regex.Replace(str, "<p rend=\"gatha[a-z0-9]*\".+?</p>",
                new MatchEvaluator(ConvertGathaDandas), RegexOptions.Compiled);

            // remove double dandas around namo tassa
            str = Regex.Replace(str, "<p rend=\"centre\".+?</p>",
                new MatchEvaluator(RemoveNamoTassaDandas), RegexOptions.Compiled);

            // convert all others to period
            str = str.Replace("\u0964", ".");
            str = str.Replace("\u0965", ".");
            return str;
        }

        public static string ConvertGathaDandas(Match m)
        {
            string str = m.Value;
            str = str.Replace("\u0964", ";");
            str = str.Replace("\u0965", ".");
            return str;
        }

        public static string RemoveNamoTassaDandas(Match m)
        {
            string str = m.Value;
            return str.Replace("\u0965", "");
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
