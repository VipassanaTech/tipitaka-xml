using System;
using System.Collections.Generic;
using System.Text;

namespace CST.Conversion
{
    public static class Beng2Deva
    {
        private static IDictionary<char, object> beng2Deva;

        static Beng2Deva()
        {
            beng2Deva = new Dictionary<char, object>();

            beng2Deva['\u0982'] = '\u0902'; // niggahita

            // independent vowels
            beng2Deva['\u0985'] = '\u0905'; // a
            beng2Deva['\u0986'] = '\u0906'; // aa
            beng2Deva['\u0987'] = '\u0907'; // i
            beng2Deva['\u0988'] = '\u0908'; // ii
            beng2Deva['\u0989'] = '\u0909'; // u
            beng2Deva['\u098A'] = '\u090A'; // uu
            beng2Deva['\u098B'] = '\u090B'; // vocalic r
            beng2Deva['\u098C'] = '\u090C'; // vocalic l
            beng2Deva['\u098F'] = '\u090F'; // e
            beng2Deva['\u0990'] = '\u0910'; // ai
            beng2Deva['\u0993'] = '\u0913'; // o
            beng2Deva['\u0994'] = '\u0914'; // au

            // velar stops
            beng2Deva['\u0995'] = '\u0915'; // ka
            beng2Deva['\u0996'] = '\u0916'; // kha
            beng2Deva['\u0997'] = '\u0917'; // ga
            beng2Deva['\u0998'] = '\u0918'; // gha
            beng2Deva['\u0999'] = '\u0919'; // n overdot a

            // palatal stops
            beng2Deva['\u099A'] = '\u091A'; // ca
            beng2Deva['\u099B'] = '\u091B'; // cha
            beng2Deva['\u099C'] = '\u091C'; // ja
            beng2Deva['\u099D'] = '\u091D'; // jha
            beng2Deva['\u099E'] = '\u091E'; // n tilde a

            // retroflex stops
            beng2Deva['\u099F'] = '\u091F'; // t underdot a
            beng2Deva['\u09A0'] = '\u0920'; // t underdot ha
            beng2Deva['\u09A1'] = '\u0921'; // d underdot a
            beng2Deva['\u09A2'] = '\u0922'; // d underdot ha
            beng2Deva['\u09A3'] = '\u0923'; // n underdot a

            // dental stops
            beng2Deva['\u09A4'] = '\u0924'; // ta
            beng2Deva['\u09A5'] = '\u0925'; // tha
            beng2Deva['\u09A6'] = '\u0926'; // da
            beng2Deva['\u09A7'] = '\u0927'; // dha
            beng2Deva['\u09A8'] = '\u0928'; // na

            // labial stops
            beng2Deva['\u09AA'] = '\u092A'; // pa
            beng2Deva['\u09AB'] = '\u092B'; // pha
            beng2Deva['\u09AC'] = '\u092C'; // ba
            beng2Deva['\u09AD'] = '\u092D'; // bha
            beng2Deva['\u09AE'] = '\u092E'; // ma

            // liquids, fricatives, etc.
            beng2Deva['\u09AF'] = '\u092F'; // ya
            beng2Deva['\u09B0'] = '\u0930'; // ra
            beng2Deva['\u09B2'] = '\u0932'; // la

            // do the la with a String.Replace before the character replacement loop
            //beng2Deva["\u09B2\u09BC"] = '\u0933'; // l underdot a *** la with dot, there's no l underdot in Bengali***
            
            beng2Deva['\u09F0'] = '\u0935'; // va *** Bengali ra with middle diagonal. Used for Assamese. ***
            beng2Deva['\u09B6'] = '\u0936'; // sha (palatal)
            beng2Deva['\u09B7'] = '\u0937'; // sha (retroflex)
            beng2Deva['\u09B8'] = '\u0938'; // sa
            beng2Deva['\u09B9'] = '\u0939'; // ha

            // dependent vowel signs
            beng2Deva['\u09BE'] = '\u093E'; // aa
            beng2Deva['\u09BF'] = '\u093F'; // i
            beng2Deva['\u09C0'] = '\u0940'; // ii
            beng2Deva['\u09C1'] = '\u0941'; // u
            beng2Deva['\u09C2'] = '\u0942'; // uu
            beng2Deva['\u09C3'] = '\u0943'; // vocalic r
            beng2Deva['\u09C7'] = '\u0947'; // e
            beng2Deva['\u09C8'] = '\u0948'; // ai
            beng2Deva['\u09CB'] = '\u094B'; // o
            beng2Deva['\u09CC'] = '\u094C'; // au

            beng2Deva['\u09CD'] = '\u094D'; // virama

            // numerals
            beng2Deva['\u09E6'] = '\u0966';
            beng2Deva['\u09E7'] = '\u0967';
            beng2Deva['\u09E8'] = '\u0968';
            beng2Deva['\u09E9'] = '\u0969';
            beng2Deva['\u09EA'] = '\u096A';
            beng2Deva['\u09EB'] = '\u096B';
            beng2Deva['\u09EC'] = '\u096C';
            beng2Deva['\u09ED'] = '\u096D';
            beng2Deva['\u09EE'] = '\u096E';
            beng2Deva['\u09EF'] = '\u096F';
        }

        public static string Convert(string str)
        {
            // la with dot
            str = str.Replace("\u09B2\u09BC", "\u0933");

            StringBuilder sb = new StringBuilder();
            foreach (char c in str.ToCharArray())
            {
                if (beng2Deva.ContainsKey(c))
                    sb.Append(beng2Deva[c]);
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
