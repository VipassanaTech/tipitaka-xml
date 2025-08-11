using System;
using System.Collections.Generic;
using System.Text;

namespace CST.Conversion
{
    public static class Sinh2Deva
    {
        private static IDictionary<char, object> sinh2Deva;

        static Sinh2Deva()
        {
            sinh2Deva = new Dictionary<char, object>();

            sinh2Deva['\u0D82'] = '\u0902'; // niggahita

            // independent vowels
            sinh2Deva['\u0D85'] = '\u0905'; // a
            sinh2Deva['\u0D86'] = '\u0906'; // aa
            sinh2Deva['\u0D89'] = '\u0907'; // i
            sinh2Deva['\u0D8A'] = '\u0908'; // ii
            sinh2Deva['\u0D8B'] = '\u0909'; // u
            sinh2Deva['\u0D8C'] = '\u090A'; // uu
            sinh2Deva['\u0D91'] = '\u090F'; // e
            sinh2Deva['\u0D94'] = '\u0913'; // o

            // velar stops
            sinh2Deva['\u0D9A'] = '\u0915'; // ka
            sinh2Deva['\u0D9B'] = '\u0916'; // kha
            sinh2Deva['\u0D9C'] = '\u0917'; // ga
            sinh2Deva['\u0D9D'] = '\u0918'; // gha
            sinh2Deva['\u0D9E'] = '\u0919'; // n overdot a

            // palatal stops
            sinh2Deva['\u0DA0'] = '\u091A'; // ca
            sinh2Deva['\u0DA1'] = '\u091B'; // cha
            sinh2Deva['\u0DA2'] = '\u091C'; // ja
            sinh2Deva['\u0DA3'] = '\u091D'; // jha
            sinh2Deva['\u0DA4'] = '\u091E'; // n tilde a

            // retroflex stops
            sinh2Deva['\u0DA7'] = '\u091F'; // t underdot a
            sinh2Deva['\u0DA8'] = '\u0920'; // t underdot ha
            sinh2Deva['\u0DA9'] = '\u0921'; // d underdot a
            sinh2Deva['\u0DAA'] = '\u0922'; // d underdot ha
            sinh2Deva['\u0DAB'] = '\u0923'; // n underdot a

            // dental stops
            sinh2Deva['\u0DAD'] = '\u0924'; // ta
            sinh2Deva['\u0DAE'] = '\u0925'; // tha
            sinh2Deva['\u0DAF'] = '\u0926'; // da
            sinh2Deva['\u0DB0'] = '\u0927'; // dha
            sinh2Deva['\u0DB1'] = '\u0928'; // na

            // labial stops
            sinh2Deva['\u0DB4'] = '\u092A'; // pa
            sinh2Deva['\u0DB5'] = '\u092B'; // pha
            sinh2Deva['\u0DB6'] = '\u092C'; // ba
            sinh2Deva['\u0DB7'] = '\u092D'; // bha
            sinh2Deva['\u0DB8'] = '\u092E'; // ma

            // liquids, fricatives, etc.
            sinh2Deva['\u0DBA'] = '\u092F'; // ya
            sinh2Deva['\u0DBB'] = '\u0930'; // ra
            sinh2Deva['\u0DBD'] = '\u0932'; // la
            sinh2Deva['\u0DC0'] = '\u0935'; // va
            sinh2Deva['\u0DC3'] = '\u0938'; // sa
            sinh2Deva['\u0DC4'] = '\u0939'; // ha
            sinh2Deva['\u0DC5'] = '\u0933'; // l underdot a

            // dependent vowel signs
            sinh2Deva['\u0DCF'] = '\u093E'; // aa
            sinh2Deva['\u0DD2'] = '\u093F'; // i
            sinh2Deva['\u0DD3'] = '\u0940'; // ii
            sinh2Deva['\u0DD4'] = '\u0941'; // u
            sinh2Deva['\u0DD6'] = '\u0942'; // uu
            sinh2Deva['\u0DD9'] = '\u0947'; // e
            sinh2Deva['\u0DDC'] = '\u094B'; // o

            // various signs
            sinh2Deva['\u0DCA'] = '\u094D'; // Sinhala virama -> Dev. virama

            // zero-width joiners
            sinh2Deva['\u200C'] = ""; // ZWNJ (remove)
            sinh2Deva['\u200D'] = ""; // ZWJ (remove)
        }

        public static string Convert(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str.ToCharArray())
            {
                if (sinh2Deva.ContainsKey(c))
                    sb.Append(sinh2Deva[c]);
                else
                    sb.Append(c);
            }

			str = sb.ToString();

			// insert ZWJ in some Devanagari conjuncts
			str = str.Replace("\u0915\u094D\u0915", "\u0915\u094D\u200D\u0915"); // ka + ka
			str = str.Replace("\u0915\u094D\u0932", "\u0915\u094D\u200D\u0932"); // ka + la
			str = str.Replace("\u0915\u094D\u0935", "\u0915\u094D\u200D\u0935"); // ka + va
			str = str.Replace("\u091A\u094D\u091A", "\u091A\u094D\u200D\u091A"); // ca + ca
			str = str.Replace("\u091C\u094D\u091C", "\u091C\u094D\u200D\u091C"); // ja + ja
			str = str.Replace("\u091E\u094D\u091A", "\u091E\u094D\u200D\u091A"); // n(tilde)a + ca
			str = str.Replace("\u091E\u094D\u091C", "\u091E\u094D\u200D\u091C"); // n(tilde)a + ja
            str = str.Replace("\u091E\u094D\u091E", "\u091E\u094D\u200D\u091E"); // n(tilde)a + n(tilde)a
            str = str.Replace("\u0928\u094D\u0928", "\u0928\u094D\u200D\u0928"); // na + na
			str = str.Replace("\u092A\u094D\u0932", "\u092A\u094D\u200D\u0932"); // pa + la
			str = str.Replace("\u0932\u094D\u0932", "\u0932\u094D\u200D\u0932"); // la + la

			return str;
        }
    }
}
