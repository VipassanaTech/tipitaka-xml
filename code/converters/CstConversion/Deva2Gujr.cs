using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class Deva2Gujr
    {
        private static IDictionary<char, object> deva2Gujarati;

        static Deva2Gujr()
        {
            deva2Gujarati = new Dictionary<char, object>();

            // various signs
            deva2Gujarati['\u0902'] = '\u0A82'; // niggahita

            // independent vowels
            deva2Gujarati['\u0905'] = '\u0A85'; // a
            deva2Gujarati['\u0906'] = '\u0A86'; // aa
            deva2Gujarati['\u0907'] = '\u0A87'; // i
            deva2Gujarati['\u0908'] = '\u0A88'; // ii
            deva2Gujarati['\u0909'] = '\u0A89'; // u
            deva2Gujarati['\u090A'] = '\u0A8A'; // uu
            deva2Gujarati['\u090F'] = '\u0A8F'; // e
            deva2Gujarati['\u0913'] = '\u0A93'; // o

            // velar stops
            deva2Gujarati['\u0915'] = '\u0A95'; // ka
            deva2Gujarati['\u0916'] = '\u0A96'; // kha
            deva2Gujarati['\u0917'] = '\u0A97'; // ga
            deva2Gujarati['\u0918'] = '\u0A98'; // gha
            deva2Gujarati['\u0919'] = '\u0A99'; // n overdot a
            
            // palatal stops
            deva2Gujarati['\u091A'] = '\u0A9A'; // ca
            deva2Gujarati['\u091B'] = '\u0A9B'; // cha
            deva2Gujarati['\u091C'] = '\u0A9C'; // ja
            deva2Gujarati['\u091D'] = '\u0A9D'; // jha
            deva2Gujarati['\u091E'] = '\u0A9E'; // n tilde a

            // retroflex stops
            deva2Gujarati['\u091F'] = '\u0A9F'; // t underdot a
            deva2Gujarati['\u0920'] = '\u0AA0'; // t underdot ha
            deva2Gujarati['\u0921'] = '\u0AA1'; // d underdot a
            deva2Gujarati['\u0922'] = '\u0AA2'; // d underdot ha
            deva2Gujarati['\u0923'] = '\u0AA3'; // n underdot a

            // dental stops
            deva2Gujarati['\u0924'] = '\u0AA4'; // ta
            deva2Gujarati['\u0925'] = '\u0AA5'; // tha
            deva2Gujarati['\u0926'] = '\u0AA6'; // da
            deva2Gujarati['\u0927'] = '\u0AA7'; // dha
            deva2Gujarati['\u0928'] = '\u0AA8'; // na

            // labial stops
            deva2Gujarati['\u092A'] = '\u0AAA'; // pa
            deva2Gujarati['\u092B'] = '\u0AAB'; // pha
            deva2Gujarati['\u092C'] = '\u0AAC'; // ba
            deva2Gujarati['\u092D'] = '\u0AAD'; // bha
            deva2Gujarati['\u092E'] = '\u0AAE'; // ma

            // liquids, fricatives, etc.
            deva2Gujarati['\u092F'] = '\u0AAF'; // ya
            deva2Gujarati['\u0930'] = '\u0AB0'; // ra
            deva2Gujarati['\u0932'] = '\u0AB2'; // la
            deva2Gujarati['\u0933'] = '\u0AB3'; // l underdot a
            deva2Gujarati['\u0935'] = '\u0AB5'; // va
            deva2Gujarati['\u0936'] = '\u0AB6'; // sha (palatal)
            deva2Gujarati['\u0937'] = '\u0AB7'; // sha (retroflex)
            deva2Gujarati['\u0938'] = '\u0AB8'; // sa
            deva2Gujarati['\u0939'] = '\u0AB9'; // ha

            // dependent vowel signs
            deva2Gujarati['\u093E'] = '\u0ABE'; // aa
            deva2Gujarati['\u093F'] = '\u0ABF'; // i
            deva2Gujarati['\u0940'] = '\u0AC0'; // ii
            deva2Gujarati['\u0941'] = '\u0AC1'; // u
            deva2Gujarati['\u0942'] = '\u0AC2'; // uu
            deva2Gujarati['\u0947'] = '\u0AC7'; // e
            deva2Gujarati['\u094B'] = '\u0ACB'; // o

            // various signs
            deva2Gujarati['\u094D'] = '\u0ACD'; // virama

            // we let dandas (U+0964) and double dandas (U+0965) pass through 
            // and handle them in ConvertDandas()

            // numerals
            deva2Gujarati['\u0966'] = '\u0AE6';
            deva2Gujarati['\u0967'] = '\u0AE7';
            deva2Gujarati['\u0968'] = '\u0AE8';
            deva2Gujarati['\u0969'] = '\u0AE9';
            deva2Gujarati['\u096A'] = '\u0AEA';
            deva2Gujarati['\u096B'] = '\u0AEB';
            deva2Gujarati['\u096C'] = '\u0AEC';
            deva2Gujarati['\u096D'] = '\u0AED';
            deva2Gujarati['\u096E'] = '\u0AEE';
            deva2Gujarati['\u096F'] = '\u0AEF';

            // zero-width joiners
            deva2Gujarati['\u200C'] = ""; // ZWNJ (remove)
            deva2Gujarati['\u200D'] = ""; // ZWJ (remove)
        }

        public static string ConvertBook(string devStr)
        {
            // change name of stylesheet for Gujarati
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-gujr.xsl");

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
                if (deva2Gujarati.ContainsKey(c))
                    sb.Append(deva2Gujarati[c]);
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
                new MatchEvaluator(RemoveNamoTassaDandas));

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
