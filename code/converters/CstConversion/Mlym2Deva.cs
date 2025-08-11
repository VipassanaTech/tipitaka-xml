using System;
using System.Collections.Generic;
using System.Text;

namespace CST.Conversion
{
    public static class Mlym2Deva
    {
        private static IDictionary<char, object> mlym2Deva;

        static Mlym2Deva()
        {
            mlym2Deva = new Dictionary<char, object>();

            // various signs
            mlym2Deva['\u0D02'] = '\u0902'; // anusvara
            mlym2Deva['\u0D03'] = '\u0903'; // visarga

            // independent vowels
            mlym2Deva['\u0D05'] = '\u0905'; // a
            mlym2Deva['\u0D06'] = '\u0906'; // aa
            mlym2Deva['\u0D07'] = '\u0907'; // i
            mlym2Deva['\u0D08'] = '\u0908'; // ii
            mlym2Deva['\u0D09'] = '\u0909'; // u
            mlym2Deva['\u0D0A'] = '\u090A'; // uu
            mlym2Deva['\u0D0B'] = '\u090B'; // vocalic r
            mlym2Deva['\u0D0C'] = '\u090C'; // vocalic l
            mlym2Deva['\u0D0E'] = '\u090F'; // e -- both the long and short forms of Malayalam e map to Devanagari e
            mlym2Deva['\u0D0F'] = '\u090F'; // e
            mlym2Deva['\u0D10'] = '\u0910'; // ai
            mlym2Deva['\u0D12'] = '\u0913'; // o -- both the long and short forms of Malayalam o map to Devanagari o
            mlym2Deva['\u0D13'] = '\u0913'; // o
            mlym2Deva['\u0D14'] = '\u0914'; // au

            // velar stops
            mlym2Deva['\u0D15'] = '\u0915'; // ka
            mlym2Deva['\u0D16'] = '\u0916'; // kha
            mlym2Deva['\u0D17'] = '\u0917'; // ga
            mlym2Deva['\u0D18'] = '\u0918'; // gha
            mlym2Deva['\u0D19'] = '\u0919'; // n overdot a

            // palatal stops
            mlym2Deva['\u0D1A'] = '\u091A'; // ca
            mlym2Deva['\u0D1B'] = '\u091B'; // cha
            mlym2Deva['\u0D1C'] = '\u091C'; // ja
            mlym2Deva['\u0D1D'] = '\u091D'; // jha
            mlym2Deva['\u0D1E'] = '\u091E'; // n tilde a

            // retroflex stops
            mlym2Deva['\u0D1F'] = '\u091F'; // t underdot a
            mlym2Deva['\u0D20'] = '\u0920'; // t underdot ha
            mlym2Deva['\u0D21'] = '\u0921'; // d underdot a
            mlym2Deva['\u0D22'] = '\u0922'; // d underdot ha
            mlym2Deva['\u0D23'] = '\u0923'; // n underdot a

            // dental stops
            mlym2Deva['\u0D24'] = '\u0924'; // ta
            mlym2Deva['\u0D25'] = '\u0925'; // tha
            mlym2Deva['\u0D26'] = '\u0926'; // da
            mlym2Deva['\u0D27'] = '\u0927'; // dha
            mlym2Deva['\u0D28'] = '\u0928'; // na

            // labial stops
            mlym2Deva['\u0D2A'] = '\u092A'; // pa
            mlym2Deva['\u0D2B'] = '\u092B'; // pha
            mlym2Deva['\u0D2C'] = '\u092C'; // ba
            mlym2Deva['\u0D2D'] = '\u092D'; // bha
            mlym2Deva['\u0D2E'] = '\u092E'; // ma

            // liquids, fricatives, etc.
            mlym2Deva['\u0D2F'] = '\u092F'; // ya
            mlym2Deva['\u0D30'] = '\u0930'; // ra
            mlym2Deva['\u0D31'] = '\u0931'; // rra (Dravidian-specific)
            mlym2Deva['\u0D32'] = '\u0932'; // la
            mlym2Deva['\u0D33'] = '\u0933'; // l underdot a
            mlym2Deva['\u0D35'] = '\u0935'; // va
            mlym2Deva['\u0D36'] = '\u0936'; // sha (palatal)
            mlym2Deva['\u0D37'] = '\u0937'; // sha (retroflex)
            mlym2Deva['\u0D38'] = '\u0938'; // sa
            mlym2Deva['\u0D39'] = '\u0939'; // ha

            // dependent vowel signs
            mlym2Deva['\u0D3E'] = '\u093E'; // aa
            mlym2Deva['\u0D3F'] = '\u093F'; // i
            mlym2Deva['\u0D40'] = '\u0940'; // ii
            mlym2Deva['\u0D41'] = '\u0941'; // u
            mlym2Deva['\u0D42'] = '\u0942'; // uu
            mlym2Deva['\u0D43'] = '\u0943'; // vocalic r
            mlym2Deva['\u0D46'] = '\u0947'; // e -- both the long and short forms of Malayalam e map to Devanagari e
            mlym2Deva['\u0D47'] = '\u0947'; // e
            mlym2Deva['\u0D48'] = '\u0948'; // ai
            mlym2Deva['\u0D4A'] = '\u094B'; // o -- both the long and short forms of Malayalam o map to Devanagari o
            mlym2Deva['\u0D4B'] = '\u094B'; // o
            mlym2Deva['\u0D4C'] = '\u094C'; // au

            // various signs
            mlym2Deva['\u0D4D'] = '\u094D'; // virama

            // additional vowels for Sanskrit
            mlym2Deva['\u0D60'] = '\u0960'; // vocalic rr
            mlym2Deva['\u0D61'] = '\u0961'; // vocalic ll

            // we let dandas (U+0964) and double dandas (U+0965) pass through 
            // and handle them in ConvertDandas()

            // digits
            mlym2Deva['\u0D66'] = '\u0966';
            mlym2Deva['\u0D67'] = '\u0967';
            mlym2Deva['\u0D68'] = '\u0968';
            mlym2Deva['\u0D69'] = '\u0969';
            mlym2Deva['\u0D6A'] = '\u096A';
            mlym2Deva['\u0D6B'] = '\u096B';
            mlym2Deva['\u0D6C'] = '\u096C';
            mlym2Deva['\u0D6D'] = '\u096D';
            mlym2Deva['\u0D6E'] = '\u096E';
            mlym2Deva['\u0D6F'] = '\u096F';
        }

        public static string Convert(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str.ToCharArray())
            {
                if (mlym2Deva.ContainsKey(c))
                    sb.Append(mlym2Deva[c]);
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
			str = str.Replace("\u091E\u094D\u091A", "\u091E\u094D\u200D\u091A"); // ña + ca
			str = str.Replace("\u091E\u094D\u091C", "\u091E\u094D\u200D\u091C"); // ña + ja
			str = str.Replace("\u091E\u094D\u091E", "\u091E\u094D\u200D\u091E"); // ña + ña
			str = str.Replace("\u0928\u094D\u0928", "\u0928\u094D\u200D\u0928"); // na + na
			str = str.Replace("\u092A\u094D\u0932", "\u092A\u094D\u200D\u0932"); // pa + la
			str = str.Replace("\u0932\u094D\u0932", "\u0932\u094D\u200D\u0932"); // la + la

			return str;
        }
    }
}
