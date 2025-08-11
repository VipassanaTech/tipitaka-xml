using System;
using System.Collections.Generic;
using System.Text;

namespace CST.Conversion
{
    public static class Knda2Deva
    {
        private static IDictionary<char, object> knda2Deva;

        static Knda2Deva()
        {
            knda2Deva = new Dictionary<char, object>();

            // various signs
            knda2Deva['\x0C82'] = '\x0902'; // anusvara
            knda2Deva['\x0C83'] = '\x0903'; // visarga

            // independent vowels
            knda2Deva['\x0C85'] = '\x0905'; // a
            knda2Deva['\x0C86'] = '\x0906'; // aa
            knda2Deva['\x0C87'] = '\x0907'; // i
            knda2Deva['\x0C88'] = '\x0908'; // ii
            knda2Deva['\x0C89'] = '\x0909'; // u
            knda2Deva['\x0C8A'] = '\x090A'; // uu
            knda2Deva['\x0C8B'] = '\x090B'; // vocalic r
            knda2Deva['\x0C8C'] = '\x090C'; // vocalic l
            knda2Deva['\x0C8F'] = '\x090F'; // e
            knda2Deva['\x0C90'] = '\x0910'; // ai
            knda2Deva['\x0C93'] = '\x0913'; // o
            knda2Deva['\x0C94'] = '\x0914'; // au

            // velar stops
            knda2Deva['\x0C95'] = '\x0915'; // ka
            knda2Deva['\x0C96'] = '\x0916'; // kha
            knda2Deva['\x0C97'] = '\x0917'; // ga
            knda2Deva['\x0C98'] = '\x0918'; // gha
            knda2Deva['\x0C99'] = '\x0919'; // n overdot a

            // palatal stops
            knda2Deva['\x0C9A'] = '\x091A'; // ca
            knda2Deva['\x0C9B'] = '\x091B'; // cha
            knda2Deva['\x0C9C'] = '\x091C'; // ja
            knda2Deva['\x0C9D'] = '\x091D'; // jha
            knda2Deva['\x0C9E'] = '\x091E'; // n tilde a

            // retroflex stops
            knda2Deva['\x0C9F'] = '\x091F'; // t underdot a
            knda2Deva['\x0CA0'] = '\x0920'; // t underdot ha
            knda2Deva['\x0CA1'] = '\x0921'; // d underdot a
            knda2Deva['\x0CA2'] = '\x0922'; // d underdot ha
            knda2Deva['\x0CA3'] = '\x0923'; // n underdot a

            // dental stops
            knda2Deva['\x0CA4'] = '\x0924'; // ta
            knda2Deva['\x0CA5'] = '\x0925'; // tha
            knda2Deva['\x0CA6'] = '\x0926'; // da
            knda2Deva['\x0CA7'] = '\x0927'; // dha
            knda2Deva['\x0CA8'] = '\x0928'; // na

            // labial stops
            knda2Deva['\x0CAA'] = '\x092A'; // pa
            knda2Deva['\x0CAB'] = '\x092B'; // pha
            knda2Deva['\x0CAC'] = '\x092C'; // ba
            knda2Deva['\x0CAD'] = '\x092D'; // bha
            knda2Deva['\x0CAE'] = '\x092E'; // ma

            // liquids, fricatives, etc.
            knda2Deva['\x0CAF'] = '\x092F'; // ya
            knda2Deva['\x0CB0'] = '\x0930'; // ra
            knda2Deva['\x0CB1'] = '\x0931'; // rra (Dravidian-specific)
            knda2Deva['\x0CB2'] = '\x0932'; // la
            knda2Deva['\x0CB3'] = '\x0933'; // l underdot a
            knda2Deva['\x0CB5'] = '\x0935'; // va
            knda2Deva['\x0CB6'] = '\x0936'; // sha (palatal)
            knda2Deva['\x0CB7'] = '\x0937'; // sha (retroflex)
            knda2Deva['\x0CB8'] = '\x0938'; // sa
            knda2Deva['\x0CB9'] = '\x0939'; // ha

            // various signs
            knda2Deva['\x0CBC'] = '\x093C'; // nukta
            knda2Deva['\x0CBD'] = '\x093D'; // avagraha

            // dependent vowel signs
            knda2Deva['\x0CBE'] = '\x093E'; // aa
            knda2Deva['\x0CBF'] = '\x093F'; // i
            knda2Deva['\x0CC0'] = '\x0940'; // ii
            knda2Deva['\x0CC1'] = '\x0941'; // u
            knda2Deva['\x0CC2'] = '\x0942'; // uu
            knda2Deva['\x0CC3'] = '\x0943'; // vocalic r
            knda2Deva['\x0CC4'] = '\x0944'; // vocalic rr
            knda2Deva['\x0CC7'] = '\x0947'; // e
            knda2Deva['\x0CC8'] = '\x0948'; // ai
            knda2Deva['\x0CCB'] = '\x094B'; // o
            knda2Deva['\x0CCC'] = '\x094C'; // au

            // various signs
            knda2Deva['\x0CCD'] = '\x094D'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            knda2Deva['\x0CE6'] = '\x0966';
            knda2Deva['\x0CE7'] = '\x0967';
            knda2Deva['\x0CE8'] = '\x0968';
            knda2Deva['\x0CE9'] = '\x0969';
            knda2Deva['\x0CEA'] = '\x096A';
            knda2Deva['\x0CEB'] = '\x096B';
            knda2Deva['\x0CEC'] = '\x096C';
            knda2Deva['\x0CED'] = '\x096D';
            knda2Deva['\x0CEE'] = '\x096E';
            knda2Deva['\x0CEF'] = '\x096F';
        }

        public static string Convert(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str.ToCharArray())
            {
                if (knda2Deva.ContainsKey(c))
                    sb.Append(knda2Deva[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
