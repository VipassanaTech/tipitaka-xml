using System;
using System.Collections.Generic;
using System.Text;

namespace CST.Conversion
{
    public static class Guru2Deva
    {
        private static IDictionary<char, object> guru2Deva;

        static Guru2Deva()
        {
            guru2Deva = new Dictionary<char, object>();

            // various signs
            guru2Deva['\u0A01'] = '\u0901'; // candrabindhu
            guru2Deva['\u0A02'] = '\u0902'; // anusvara
            guru2Deva['\u0A03'] = '\u0903'; // visarga

            // independent vowels
            guru2Deva['\u0A05'] = '\u0905'; // a
            guru2Deva['\u0A06'] = '\u0906'; // aa
            guru2Deva['\u0A07'] = '\u0907'; // i
            guru2Deva['\u0A08'] = '\u0908'; // ii
            guru2Deva['\u0A09'] = '\u0909'; // u
            guru2Deva['\u0A0A'] = '\u090A'; // uu
            guru2Deva['\u0A0F'] = '\u090F'; // e
            guru2Deva['\u0A10'] = '\u0910'; // ai
            guru2Deva['\u0A13'] = '\u0913'; // o
            guru2Deva['\u0A14'] = '\u0914'; // au

            // velar stops
            guru2Deva['\u0A15'] = '\u0915'; // ka
            guru2Deva['\u0A16'] = '\u0916'; // kha
            guru2Deva['\u0A17'] = '\u0917'; // ga
            guru2Deva['\u0A18'] = '\u0918'; // gha
            guru2Deva['\u0A19'] = '\u0919'; // n overdot a

            // palatal stops
            guru2Deva['\u0A1A'] = '\u091A'; // ca
            guru2Deva['\u0A1B'] = '\u091B'; // cha
            guru2Deva['\u0A1C'] = '\u091C'; // ja
            guru2Deva['\u0A1D'] = '\u091D'; // jha
            guru2Deva['\u0A1E'] = '\u091E'; // n tilde a

            // retroflex stops
            guru2Deva['\u0A1F'] = '\u091F'; // t underdot a
            guru2Deva['\u0A20'] = '\u0920'; // t underdot ha
            guru2Deva['\u0A21'] = '\u0921'; // d underdot a
            guru2Deva['\u0A22'] = '\u0922'; // d underdot ha
            guru2Deva['\u0A23'] = '\u0923'; // n underdot a

            // dental stops
            guru2Deva['\u0A24'] = '\u0924'; // ta
            guru2Deva['\u0A25'] = '\u0925'; // tha
            guru2Deva['\u0A26'] = '\u0926'; // da
            guru2Deva['\u0A27'] = '\u0927'; // dha
            guru2Deva['\u0A28'] = '\u0928'; // na

            // labial stops
            guru2Deva['\u0A2A'] = '\u092A'; // pa
            guru2Deva['\u0A2B'] = '\u092B'; // pha
            guru2Deva['\u0A2C'] = '\u092C'; // ba
            guru2Deva['\u0A2D'] = '\u092D'; // bha
            guru2Deva['\u0A2E'] = '\u092E'; // ma

            // liquids, fricatives, etc.
            guru2Deva['\u0A2F'] = '\u092F'; // ya
            guru2Deva['\u0A30'] = '\u0930'; // ra
            guru2Deva['\u0A32'] = '\u0932'; // la
            guru2Deva['\u0A33'] = '\u0933'; // l underdot a
            guru2Deva['\u0AB5'] = '\u0935'; // va
            guru2Deva['\u0A36'] = '\u0936'; // sha (palatal)
            guru2Deva['\u0A38'] = '\u0938'; // sa
            guru2Deva['\u0A39'] = '\u0939'; // ha

            // dependent vowel signs
            guru2Deva['\u0A3E'] = '\u093E'; // aa
            guru2Deva['\u0A3F'] = '\u093F'; // i
            guru2Deva['\u0A40'] = '\u0940'; // ii
            guru2Deva['\u0A41'] = '\u0941'; // u
            guru2Deva['\u0A42'] = '\u0942'; // uu
            guru2Deva['\u0A47'] = '\u0947'; // e
            guru2Deva['\u0A48'] = '\u0948'; // ai
            guru2Deva['\u0A4B'] = '\u094B'; // o
            guru2Deva['\u0A4C'] = '\u094C'; // au

            // various signs
            guru2Deva['\u0A4D'] = '\u094D'; // virama

            // let Devanagari danda (U+0964) and double danda (U+0965) 
            // pass through unmodified

            // digits
            guru2Deva['\u0A66'] = '\u0966';
            guru2Deva['\u0A67'] = '\u0967';
            guru2Deva['\u0A68'] = '\u0968';
            guru2Deva['\u0A69'] = '\u0969';
            guru2Deva['\u0A6A'] = '\u096A';
            guru2Deva['\u0A6B'] = '\u096B';
            guru2Deva['\u0A6C'] = '\u096C';
            guru2Deva['\u0A6D'] = '\u096D';
            guru2Deva['\u0A6E'] = '\u096E';
            guru2Deva['\u0A6F'] = '\u096F';
        }

        public static string Convert(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str.ToCharArray())
            {
                if (guru2Deva.ContainsKey(c))
                    sb.Append(guru2Deva[c]);
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
