using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class Deva2Latn
    {
        private static IDictionary<char, object> deva2Latn;

        static Deva2Latn()
        {
            deva2Latn = new Dictionary<char, object>();

            deva2Latn['\u0902'] = '\u1E43'; // niggahita

            // independent vowels
            deva2Latn['\u0905'] = 'a'; // a
            deva2Latn['\u0906'] = '\u0101'; // aa
            deva2Latn['\u0907'] = 'i'; // i
            deva2Latn['\u0908'] = '\u012B'; // ii
            deva2Latn['\u0909'] = 'u'; // u
			deva2Latn['\u0910'] = "ai"; // ai
            deva2Latn['\u090A'] = '\u016B'; // uu
            deva2Latn['\u090F'] = 'e'; // e
            deva2Latn['\u0913'] = 'o'; // o
			deva2Latn['\u0914'] = "au"; // au

            // velar stops
            deva2Latn['\u0915'] = 'k'; // ka
            deva2Latn['\u0916'] = "kh"; // kha
            deva2Latn['\u0917'] = 'g'; // ga
            deva2Latn['\u0918'] = "gh"; // gha
            deva2Latn['\u0919'] = "\u1E45"; // n overdot a
            
            // palatal stops
            deva2Latn['\u091A'] = 'c'; // ca
            deva2Latn['\u091B'] = "ch"; // cha
            deva2Latn['\u091C'] = 'j'; // ja
            deva2Latn['\u091D'] = "jh"; // jha
            deva2Latn['\u091E'] = "\u00F1"; // n tilde a

            // retroflex stops
            deva2Latn['\u091F'] = '\u1E6D'; // t underdot a
            deva2Latn['\u0920'] = "\u1E6Dh"; // t underdot ha
            deva2Latn['\u0921'] = '\u1E0D'; // d underdot a
            deva2Latn['\u0922'] = "\u1E0Dh"; // d underdot ha
            deva2Latn['\u0923'] = '\u1E47'; // n underdot a

            // dental stops
            deva2Latn['\u0924'] = 't'; // ta
            deva2Latn['\u0925'] = "th"; // tha
            deva2Latn['\u0926'] = 'd'; // da
            deva2Latn['\u0927'] = "dh"; // dha
            deva2Latn['\u0928'] = 'n'; // na

            // labial stops
            deva2Latn['\u092A'] = 'p'; // pa
            deva2Latn['\u092B'] = "ph"; // pha
            deva2Latn['\u092C'] = 'b'; // ba
            deva2Latn['\u092D'] = "bh"; // bha
            deva2Latn['\u092E'] = 'm'; // ma

            // liquids, fricatives, etc.
            deva2Latn['\u092F'] = 'y'; // ya
            deva2Latn['\u0930'] = 'r'; // ra
            deva2Latn['\u0932'] = 'l'; // la
            deva2Latn['\u0935'] = 'v'; // va
            deva2Latn['\u0938'] = 's'; // sa
            deva2Latn['\u0939'] = 'h'; // ha
            deva2Latn['\u0933'] = "\u1E37"; // l underdot a

            // dependent vowel signs
            deva2Latn['\u093E'] = '\u0101'; // aa
            deva2Latn['\u093F'] = 'i'; // i
            deva2Latn['\u0940'] = "\u012B"; // ii
            deva2Latn['\u0941'] = 'u'; // u
            deva2Latn['\u0942'] = '\u016B'; // uu
            deva2Latn['\u0947'] = 'e'; // e
			deva2Latn['\u0948'] = "ai"; // ai
            deva2Latn['\u094B'] = 'o'; // o
			deva2Latn['\u094C'] = "au"; // au

            deva2Latn['\u094D'] = ""; // virama

            // numerals
            deva2Latn['\u0966'] = '0';
            deva2Latn['\u0967'] = '1';
            deva2Latn['\u0968'] = '2';
            deva2Latn['\u0969'] = '3';
            deva2Latn['\u096A'] = '4';
            deva2Latn['\u096B'] = '5';
            deva2Latn['\u096C'] = '6';
            deva2Latn['\u096D'] = '7';
            deva2Latn['\u096E'] = '8';
            deva2Latn['\u096F'] = '9';

            // we let dandas and double dandas pass through and handle
            // them in ConvertDandas()
            //deva2Latn['\u0964'] = '.'; // danda 
            deva2Latn['\u0970'] = '.'; // Devanagari abbreviation sign
            
            deva2Latn['\u200C'] = ""; // ZWNJ (ignore)
            deva2Latn['\u200D'] = ""; // ZWJ (ignore)
        }

        public static string ConvertBook(string devStr)
        {
            // mark the Devanagari text for programmatic capitalization
            LatinCapitalizer capitalizer = new LatinCapitalizer(
                new string[] {"p", "head", "trailer" }, 
                new string[] {"note"},
                "\u4676");
            devStr = capitalizer.MarkCapitals(devStr);

            // remove Dev abbreviation sign before an ellipsis. We don't want a 4th dot after pe.
            devStr = devStr.Replace("\u0970\u2026", "\u2026");

            string str = Convert(devStr);

            str = capitalizer.Capitalize(str);
            str = ConvertDandas(str);
            str = CleanupPunctuation(str);

            // convert "nti to n"ti, per Dhananjay email 3 Aug 07
            // commenting out per Ramnath/Priti email  29 Aug 07
            //str = Regex.Replace(str, "([’”]*)nti", "n$1ti");

            return str;
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public static string Convert(string devStr)
        {
            // insert 'a' after all consonants that are not followed by virama, dependent vowel or 'a'
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])([^\u093E-\u094Da])", "$1a$2", RegexOptions.Compiled);
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])([^\u093E-\u094Da])", "$1a$2", RegexOptions.Compiled);
            // TODO: figure out how to backtrack so this replace doesn't have to be done twice

            // subtle bug not encountered in Tipitaka files:
            // insert a after consonant that is at end of string
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])$", "$1a", RegexOptions.Compiled);

            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Latn.ContainsKey(c))
                    sb.Append(deva2Latn[c]);
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
