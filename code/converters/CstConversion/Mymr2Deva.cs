using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CST.Conversion
{
    public static class Mymr2Deva
    {
        private static IDictionary<char, object> mymr2Deva;

        static Mymr2Deva()
        {
            mymr2Deva = new Dictionary<char, object>();

            // velar stops
            mymr2Deva['\u1000'] = '\u0915'; // ka
            mymr2Deva['\u1001'] = '\u0916'; // kha
            mymr2Deva['\u1002'] = '\u0917'; // ga
            mymr2Deva['\u1003'] = '\u0918'; // gha
            mymr2Deva['\u1004'] = '\u0919'; // n overdot a
            
            // palatal stops
            mymr2Deva['\u1005'] = '\u091A'; // ca
            mymr2Deva['\u1006'] = '\u091B'; // cha
            mymr2Deva['\u1007'] = '\u091C'; // ja
            mymr2Deva['\u1008'] = '\u091D'; // jha
            mymr2Deva['\u1009'] = '\u091E'; // n tilde a
            mymr2Deva['\u100A'] = "\u1009\u1039\u1009"; // double n tilde a

            // retroflex stops
            mymr2Deva['\u100B'] = '\u091F'; // t underdot a
            mymr2Deva['\u100C'] = '\u0920'; // t underdot ha
            mymr2Deva['\u100D'] = '\u0921'; // d underdot a
            mymr2Deva['\u100E'] = '\u0922'; // d underdot ha
            mymr2Deva['\u100F'] = '\u0923'; // n underdot a

            // dental stops
            mymr2Deva['\u1010'] = '\u0924'; // ta
            mymr2Deva['\u1011'] = '\u0925'; // tha
            mymr2Deva['\u1012'] = '\u0926'; // da
            mymr2Deva['\u1013'] = '\u0927'; // dha
            mymr2Deva['\u1014'] = '\u0928'; // na

            // labial stops
            mymr2Deva['\u1015'] = '\u092A'; // pa
            mymr2Deva['\u1016'] = '\u092B'; // pha
            mymr2Deva['\u1017'] = '\u092C'; // ba
            mymr2Deva['\u1018'] = '\u092D'; // bha
            mymr2Deva['\u1019'] = '\u092E'; // ma

            // liquids, fricatives, etc.
            mymr2Deva['\u101A'] = '\u092F'; // ya
            mymr2Deva['\u101B'] = '\u0930'; // ra
            mymr2Deva['\u101C'] = '\u0932'; // la
            mymr2Deva['\u101D'] = '\u0935'; // va
            mymr2Deva['\u101E'] = '\u0938'; // sa
            mymr2Deva['\u101F'] = '\u0939'; // ha
            mymr2Deva['\u1020'] = '\u0933'; // l underdot a

            // independent vowels
            mymr2Deva['\u1021'] = '\u0905'; // a
            //deva2Mymr['\u0906'] = "\u1021\u102C"; // independent aa handled by regex in Convert()
            mymr2Deva['\u1023'] = '\u0907'; // i
            mymr2Deva['\u1024'] = '\u0908'; // ii
            mymr2Deva['\u1025'] = '\u0909'; // u
            mymr2Deva['\u1026'] = '\u090A'; // uu
            mymr2Deva['\u1027'] = '\u090F'; // e
            mymr2Deva['\u1029'] = '\u0913'; // o

            // dependent vowel signs
            mymr2Deva['\u102C'] = '\u093E'; // aa
            mymr2Deva['\u102D'] = '\u093F'; // i
            mymr2Deva['\u102E'] = '\u0940'; // ii
            mymr2Deva['\u102F'] = '\u0941'; // u
            mymr2Deva['\u1030'] = '\u0942'; // uu
            mymr2Deva['\u1031'] = '\u0947'; // e
            //deva2Mymr['\u094B'] = "\u1031\u102C"; // dependent o handled by regex in Convert()

            // remove asat/killer, used for rendering in Myanmar like ZWJ/ZWNJ in Deva
            mymr2Deva['\u103A'] = "";

            // replace the dependent consonant signs ya, ra, wa and ha (with no preceding virama) with virama + deva letter
            mymr2Deva['\u103B'] = "\u1039\u101A";
            mymr2Deva['\u103C'] = "\u1039\u101B";
            mymr2Deva['\u103D'] = "\u1039\u101D";
            mymr2Deva['\u103E'] = "\u1039\u101F";

            // Myanmar great sa becomes Deva sa + virama + sa
            mymr2Deva['\u103F'] = "\u101E\u1039\u101E";

            // numerals
            mymr2Deva['\u1040'] = '\u0966';
            mymr2Deva['\u1041'] = '\u0967';
            mymr2Deva['\u1042'] = '\u0968';
            mymr2Deva['\u1043'] = '\u0969';
            mymr2Deva['\u1044'] = '\u096A';
            mymr2Deva['\u1045'] = '\u096B';
            mymr2Deva['\u1046'] = '\u096C';
            mymr2Deva['\u1047'] = '\u096D';
            mymr2Deva['\u1048'] = '\u096E';
            mymr2Deva['\u1049'] = '\u096F';

            // other
            mymr2Deva['\u104A'] = '\u0964'; // danda
            mymr2Deva['\u1036'] = '\u0902'; // niggahita
            mymr2Deva['\u1039'] = '\u094D'; // virama
            //deva2Mymr['\u200C'] = ""; // ZWNJ (ignore)
            //deva2Mymr['\u200D'] = ""; // ZWJ (ignore)
        }

        // more generalized, reusable conversion method:
        // no stylesheet modifications, capitalization, etc.
        public static string Convert(string str)
        {
            // replace all sign tall aa with sign aa
            str = str.Replace("\u102B", "\u102C");

            // independent "a" plus dependent sign aa = independent aa
			str = str.Replace("\u1021\u102C", "\u0906");

            // dependent e plus dependent sign aa = dependent o
			str = str.Replace("\u1031\u102C", "\u094B");

            StringBuilder sb = new StringBuilder();
			foreach (char c in str.ToCharArray())
            {
                if (mymr2Deva.ContainsKey(c))
                    sb.Append(mymr2Deva[c]);
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
