using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CST.Conversion
{
    public static class Deva2Knda
    {
        private static IDictionary<char, object> deva2Knda;

        static Deva2Knda()
        {
            deva2Knda = new Dictionary<char, object>();

            // various signs
            deva2Knda['\u0902'] = '\u0C82'; // anusvara
            deva2Knda['\u0903'] = '\u0C83'; // visarga

            // independent vowels
            deva2Knda['\u0905'] = '\u0C85'; // a
            deva2Knda['\u0906'] = '\u0C86'; // aa
            deva2Knda['\u0907'] = '\u0C87'; // i
            deva2Knda['\u0908'] = '\u0C88'; // ii
            deva2Knda['\u0909'] = '\u0C89'; // u
            deva2Knda['\u090A'] = '\u0C8A'; // uu
            deva2Knda['\u090B'] = '\u0C8B'; // vocalic r
            deva2Knda['\u090C'] = '\u0C8C'; // vocalic l
            deva2Knda['\u090F'] = '\u0C8F'; // e
            deva2Knda['\u0910'] = '\u0C90'; // ai
            deva2Knda['\u0913'] = '\u0C93'; // o
            deva2Knda['\u0914'] = '\u0C94'; // au

            // velar stops
            deva2Knda['\u0915'] = '\u0C95'; // ka
            deva2Knda['\u0916'] = '\u0C96'; // kha
            deva2Knda['\u0917'] = '\u0C97'; // ga
            deva2Knda['\u0918'] = '\u0C98'; // gha
            deva2Knda['\u0919'] = '\u0C99'; // n overdot a
            
            // palatal stops
            deva2Knda['\u091A'] = '\u0C9A'; // ca
            deva2Knda['\u091B'] = '\u0C9B'; // cha
            deva2Knda['\u091C'] = '\u0C9C'; // ja
            deva2Knda['\u091D'] = '\u0C9D'; // jha
            deva2Knda['\u091E'] = '\u0C9E'; // n tilde a

            // retroflex stops
            deva2Knda['\u091F'] = '\u0C9F'; // t underdot a
            deva2Knda['\u0920'] = '\u0CA0'; // t underdot ha
            deva2Knda['\u0921'] = '\u0CA1'; // d underdot a
            deva2Knda['\u0922'] = '\u0CA2'; // d underdot ha
            deva2Knda['\u0923'] = '\u0CA3'; // n underdot a

            // dental stops
            deva2Knda['\u0924'] = '\u0CA4'; // ta
            deva2Knda['\u0925'] = '\u0CA5'; // tha
            deva2Knda['\u0926'] = '\u0CA6'; // da
            deva2Knda['\u0927'] = '\u0CA7'; // dha
            deva2Knda['\u0928'] = '\u0CA8'; // na

            // labial stops
            deva2Knda['\u092A'] = '\u0CAA'; // pa
            deva2Knda['\u092B'] = '\u0CAB'; // pha
            deva2Knda['\u092C'] = '\u0CAC'; // ba
            deva2Knda['\u092D'] = '\u0CAD'; // bha
            deva2Knda['\u092E'] = '\u0CAE'; // ma

            // liquids, fricatives, etc.
            deva2Knda['\u092F'] = '\u0CAF'; // ya
            deva2Knda['\u0930'] = '\u0CB0'; // ra
            deva2Knda['\u0931'] = '\u0CB1'; // rra (Dravidian-specific)
            deva2Knda['\u0932'] = '\u0CB2'; // la
            deva2Knda['\u0933'] = '\u0CB3'; // l underdot a
            deva2Knda['\u0935'] = '\u0CB5'; // va
            deva2Knda['\u0936'] = '\u0CB6'; // sha (palatal)
            deva2Knda['\u0937'] = '\u0CB7'; // sha (retroflex)
            deva2Knda['\u0938'] = '\u0CB8'; // sa
            deva2Knda['\u0939'] = '\u0CB9'; // ha

            // various signs
            deva2Knda['\u093C'] = '\u0CBC'; // nukta
            deva2Knda['\u093D'] = '\u0CBD'; // avagraha

            // dependent vowel signs
            deva2Knda['\u093E'] = '\u0CBE'; // aa
            deva2Knda['\u093F'] = '\u0CBF'; // i
            deva2Knda['\u0940'] = '\u0CC0'; // ii
            deva2Knda['\u0941'] = '\u0CC1'; // u
            deva2Knda['\u0942'] = '\u0CC2'; // uu
            deva2Knda['\u0943'] = '\u0CC3'; // vocalic r
            deva2Knda['\u0944'] = '\u0CC4'; // vocalic rr
            deva2Knda['\u0947'] = '\u0CC7'; // e
            deva2Knda['\u0948'] = '\u0CC8'; // ai
            deva2Knda['\u094B'] = '\u0CCB'; // o
            deva2Knda['\u094C'] = '\u0CCC'; // au

            // various signs
            deva2Knda['\u094D'] = '\u0CCD'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            deva2Knda['\u0966'] = '\u0CE6';
            deva2Knda['\u0967'] = '\u0CE7';
            deva2Knda['\u0968'] = '\u0CE8';
            deva2Knda['\u0969'] = '\u0CE9';
            deva2Knda['\u096A'] = '\u0CEA';
            deva2Knda['\u096B'] = '\u0CEB';
            deva2Knda['\u096C'] = '\u0CEC';
            deva2Knda['\u096D'] = '\u0CED';
            deva2Knda['\u096E'] = '\u0CEE';
            deva2Knda['\u096F'] = '\u0CEF';

            // zero-width joiners
            deva2Knda['\u200C'] = ""; // ZWNJ (remove)
            deva2Knda['\u200D'] = ""; // ZWJ (remove)
        }

        public static string ConvertBook(string devStr)
        {
            // change name of stylesheet for Kannada
            devStr = devStr.Replace("tipitaka-deva.xsl", "tipitaka-knda.xsl");

            return Convert(devStr);
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public static string Convert(string devStr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in devStr.ToCharArray())
            {
                if (deva2Knda.ContainsKey(c))
                    sb.Append(deva2Knda[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
