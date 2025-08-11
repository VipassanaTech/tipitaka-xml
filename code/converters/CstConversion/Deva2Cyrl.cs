using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class Deva2Cyrl
    {

        private static IDictionary<char, object> deva2Cyrl;

        static Deva2Cyrl()
        {
            deva2Cyrl = new Dictionary<char, object>();

            deva2Cyrl['\u0902'] = "\u043C\u0323"; // niggahita

            // velar stops
            deva2Cyrl['\u0915'] = '\u0433'; // ka
            deva2Cyrl['\u0916'] = '\u043A'; // kha
            deva2Cyrl['\u0917'] = "\u0433\u0307"; // ga
            deva2Cyrl['\u0918'] = "\u0433\u0445"; // gha
            deva2Cyrl['\u0919'] = "\u043D\u0307"; // n overdot a
            
            // palatal stops
            deva2Cyrl['\u091A'] = '\u0436'; // ca
            deva2Cyrl['\u091B'] = '\u0447'; // cha
            deva2Cyrl['\u091C'] = "\u0436\u0307"; // ja
            deva2Cyrl['\u091D'] = "\u0436\u0445"; // jha
            deva2Cyrl['\u091E'] = "\u043D\u0303"; // n tilde a

            // retroflex stops
            deva2Cyrl['\u091F'] = '\u0434'; // t underdot a
            deva2Cyrl['\u0920'] = '\u0442'; // t underdot ha
            deva2Cyrl['\u0921'] = "\u0434\u0323"; // d underdot a
            deva2Cyrl['\u0922'] = "\u0434\u0445"; // d underdot ha
            deva2Cyrl['\u0923'] = "\u043D\u0323"; // n underdot a

            // dental stops
            deva2Cyrl['\u0924'] = "\u0434\u0307"; // ta
            deva2Cyrl['\u0925'] = "\u0442\u0307"; // tha
            deva2Cyrl['\u0926'] = "\u0434\u0307\u0323"; // da
            deva2Cyrl['\u0927'] = "\u0434\u0307\u0445"; // dha
            deva2Cyrl['\u0928'] = '\u043D'; // na

            // labial stops
            deva2Cyrl['\u092A'] = '\u0431'; // pa
            deva2Cyrl['\u092B'] = '\u043F'; // pha
            deva2Cyrl['\u092C'] = "\u0431\u0323"; // ba
            deva2Cyrl['\u092D'] = "\u0431\u0445"; // bha
            deva2Cyrl['\u092E'] = '\u043C'; // ma

            // liquids, fricatives, etc.
            deva2Cyrl['\u092F'] = '\u044F'; // ya
            deva2Cyrl['\u0930'] = '\u0440'; // ra
            deva2Cyrl['\u0932'] = '\u043B'; // la
            deva2Cyrl['\u0935'] = '\u0432'; // va
            deva2Cyrl['\u0938'] = '\u0441'; // sa
            deva2Cyrl['\u0939'] = '\u0445'; // ha
            deva2Cyrl['\u0933'] = "\u043B\u0323"; // l underdot a

            // independent vowels
            deva2Cyrl['\u0905'] = '\u0430'; // a
            deva2Cyrl['\u0906'] = "\u0430\u0430"; // aa
            deva2Cyrl['\u0907'] = '\u0438'; // i
            deva2Cyrl['\u0908'] = "\u0438\u0439"; // ii
            deva2Cyrl['\u0909'] = '\u0443'; // u
            deva2Cyrl['\u090A'] = "\u0443\u0443"; // uu
            deva2Cyrl['\u090F'] = '\u0437'; // e
            deva2Cyrl['\u0913'] = '\u043E'; // o

            // dependent vowel signs
            deva2Cyrl['\u093E'] = "\u0430\u0430"; // aa
            deva2Cyrl['\u093F'] = '\u0438'; // i
            deva2Cyrl['\u0940'] = "\u0438\u0439"; // ii
            deva2Cyrl['\u0941'] = '\u0443'; // u
            deva2Cyrl['\u0942'] = "\u0443\u0443"; // uu
            deva2Cyrl['\u0947'] = '\u0437'; // e
            deva2Cyrl['\u094B'] = '\u043E'; // o

            // various signs
            deva2Cyrl['\u094D'] = ""; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // numerals
            deva2Cyrl['\u0966'] = '0';
            deva2Cyrl['\u0967'] = '1';
            deva2Cyrl['\u0968'] = '2';
            deva2Cyrl['\u0969'] = '3';
            deva2Cyrl['\u096A'] = '4';
            deva2Cyrl['\u096B'] = '5';
            deva2Cyrl['\u096C'] = '6';
            deva2Cyrl['\u096D'] = '7';
            deva2Cyrl['\u096E'] = '8';
            deva2Cyrl['\u096F'] = '9';

            deva2Cyrl['\u0970'] = "."; // Dev abbreviation sign
            deva2Cyrl['\u200C'] = ""; // ZWNJ (ignore)
            deva2Cyrl['\u200D'] = ""; // ZWJ (ignore)
        }

        public static string ConvertBook(string devStr)
        {
            // change name of stylesheet for Cyrillic
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-cyrl.xsl");

            // mark the Devanagari text for programmatic capitalization
            //Capitalizer capitalizer = new Capitalizer(ParagraphElements, IgnoreElements, CapitalMarker);
            //devStr = capitalizer.MarkCapitals(devStr);

            // remove Dev abbreviation sign before an ellipsis. We don't want a 4th dot after pe.
            devStr = devStr.Replace("\u0970\u2026", "\u2026");

            string str = Convert(devStr);

            //str = capitalizer.Capitalize(str);
            str = ConvertDandas(str);
            str = CleanupPunctuation(str);

            return str;
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public static string Convert(string devStr)
        {
            // Insert Cyrillic 'a' after all consonants that are not followed by virama, dependent vowel or cyrillic a
            // (This still works after we inserted ZWJ in the Devanagari. The ZWJ goes after virama.)
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])([^\u093E-\u094D\u0430])", "$1\u0430$2", RegexOptions.Compiled);
            devStr = Regex.Replace(devStr, "([\u0915-\u0939])([^\u093E-\u094D\u0430])", "$1\u0430$2", RegexOptions.Compiled);
            // TODO: figure out how to backtrack so this replace doesn't have to be done twice

            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Cyrl.ContainsKey(c))
                    sb.Append(deva2Cyrl[c]);
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
