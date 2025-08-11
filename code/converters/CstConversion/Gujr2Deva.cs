using System;
using System.Collections.Generic;
using System.Text;

namespace CST.Conversion
{
    public static class Gujr2Deva
    {
        private static IDictionary<char, object> gujr2Deva;

        static Gujr2Deva()
        {
            gujr2Deva = new Dictionary<char, object>();

            // various signs
            gujr2Deva['\u0A82'] = '\u0902'; // niggahita

            // independent vowels
            gujr2Deva['\u0A85'] = '\u0905'; // a
            gujr2Deva['\u0A86'] = '\u0906'; // aa
            gujr2Deva['\u0A87'] = '\u0907'; // i
            gujr2Deva['\u0A88'] = '\u0908'; // ii
            gujr2Deva['\u0A89'] = '\u0909'; // u
            gujr2Deva['\u0A8A'] = '\u090A'; // uu
            gujr2Deva['\u0A8F'] = '\u090F'; // e
            gujr2Deva['\u0A93'] = '\u0913'; // o

            // velar stops
            gujr2Deva['\u0A95'] = '\u0915'; // ka
            gujr2Deva['\u0A96'] = '\u0916'; // kha
            gujr2Deva['\u0A97'] = '\u0917'; // ga
            gujr2Deva['\u0A98'] = '\u0918'; // gha
            gujr2Deva['\u0A99'] = '\u0919'; // n overdot a

            // palatal stops
            gujr2Deva['\u0A9A'] = '\u091A'; // ca
            gujr2Deva['\u0A9B'] = '\u091B'; // cha
            gujr2Deva['\u0A9C'] = '\u091C'; // ja
            gujr2Deva['\u0A9D'] = '\u091D'; // jha
            gujr2Deva['\u0A9E'] = '\u091E'; // n tilde a

            // retroflex stops
            gujr2Deva['\u0A9F'] = '\u091F'; // t underdot a
            gujr2Deva['\u0AA0'] = '\u0920'; // t underdot ha
            gujr2Deva['\u0AA1'] = '\u0921'; // d underdot a
            gujr2Deva['\u0AA2'] = '\u0922'; // d underdot ha
            gujr2Deva['\u0AA3'] = '\u0923'; // n underdot a

            // dental stops
            gujr2Deva['\u0AA4'] = '\u0924'; // ta
            gujr2Deva['\u0AA5'] = '\u0925'; // tha
            gujr2Deva['\u0AA6'] = '\u0926'; // da
            gujr2Deva['\u0AA7'] = '\u0927'; // dha
            gujr2Deva['\u0AA8'] = '\u0928'; // na

            // labial stops
            gujr2Deva['\u0AAA'] = '\u092A'; // pa
            gujr2Deva['\u0AAB'] = '\u092B'; // pha
            gujr2Deva['\u0AAC'] = '\u092C'; // ba
            gujr2Deva['\u0AAD'] = '\u092D'; // bha
            gujr2Deva['\u0AAE'] = '\u092E'; // ma

            // liquids, fricatives, etc.
            gujr2Deva['\u0AAF'] = '\u092F'; // ya
            gujr2Deva['\u0AB0'] = '\u0930'; // ra
            gujr2Deva['\u0AB2'] = '\u0932'; // la
            gujr2Deva['\u0AB3'] = '\u0933'; // l underdot a
            gujr2Deva['\u0AB5'] = '\u0935'; // va
            gujr2Deva['\u0AB6'] = '\u0936'; // sha (palatal)
            gujr2Deva['\u0AB7'] = '\u0937'; // sha (retroflex)
            gujr2Deva['\u0AB8'] = '\u0938'; // sa
            gujr2Deva['\u0AB9'] = '\u0939'; // ha

            // dependent vowel signs
            gujr2Deva['\u0ABE'] = '\u093E'; // aa
            gujr2Deva['\u0ABF'] = '\u093F'; // i
            gujr2Deva['\u0AC0'] = '\u0940'; // ii
            gujr2Deva['\u0AC1'] = '\u0941'; // u
            gujr2Deva['\u0AC2'] = '\u0942'; // uu
            gujr2Deva['\u0AC7'] = '\u0947'; // e
            gujr2Deva['\u0ACB'] = '\u094B'; // o

            // various signs
            gujr2Deva['\u0ACD'] = '\u094D'; // virama

            // we let dandas (U+0964) and double dandas (U+0965) pass through 
            // and handle them in ConvertDandas()

            // numerals
            gujr2Deva['\u0AE6'] = '\u0966';
            gujr2Deva['\u0AE7'] = '\u0967';
            gujr2Deva['\u0AE8'] = '\u0968';
            gujr2Deva['\u0AE9'] = '\u0969';
            gujr2Deva['\u0AEA'] = '\u096A';
            gujr2Deva['\u0AEB'] = '\u096B';
            gujr2Deva['\u0AEC'] = '\u096C';
            gujr2Deva['\u0AED'] = '\u096D';
            gujr2Deva['\u0AEE'] = '\u096E';
            gujr2Deva['\u0AEF'] = '\u096F';
        }

        public static string Convert(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str.ToCharArray())
            {
                if (gujr2Deva.ContainsKey(c))
                    sb.Append(gujr2Deva[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}