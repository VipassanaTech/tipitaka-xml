using System;
using System.Collections.Generic;
using System.Text;

namespace CST
{
    public static class Ipe2Latn
    {
        private static Dictionary<string, string> ipe2Latn;

        static Ipe2Latn()
        {
            ipe2Latn = new Dictionary<string, string>();

            ipe2Latn["\x00C0"] = "\x1E43"; // niggahita

            // vowels
            ipe2Latn["\x00C1"] = "a"; // a
            ipe2Latn["\x00C2"] = "\x0101"; // aa
            ipe2Latn["\x00C3"] = "i"; // i
            ipe2Latn["\x00C4"] = "\x012B"; // ii
            ipe2Latn["\x00C5"] = "u"; // u
            ipe2Latn["\x00C6"] = "\x016B"; // uu
            ipe2Latn["\x00C7"] = "e"; // e
            ipe2Latn["\x00C8"] = "o"; // o

            // velar stops
            ipe2Latn["\x00C9"] = "k"; // ka
            ipe2Latn["\x00CA"] = "kh"; // kha
            ipe2Latn["\x00CB"] = "g"; // ga
            ipe2Latn["\x00CC"] = "gh"; // gha
            ipe2Latn["\x00CD"] = "\x1E45"; // n overdot a

            // palatal stops
            ipe2Latn["\x00CE"] = "c"; // ca
            ipe2Latn["\x00CF"] = "ch"; // cha
            ipe2Latn["\x00D0"] = "j"; // ja
            ipe2Latn["\x00D1"] = "jh"; // jha
            ipe2Latn["\x00D2"] = "\u00F1"; // n tilde a

            // retroflex stops
            ipe2Latn["\x00D3"] = "\x1E6D"; // t underdot a
            ipe2Latn["\x00D4"] = "\x1E6Dh"; // t underdot ha
            ipe2Latn["\x00D5"] = "\x1E0D"; // d underdot a
            ipe2Latn["\x00D6"] = "\x1E0Dh"; // d underdot ha
            // D7 multiplication sign is unused
            ipe2Latn["\x00D8"] = "\x1E47"; // n underdot a

            // dental stops
            ipe2Latn["\x00D9"] = "t"; // ta
            ipe2Latn["\x00DA"] = "th"; // tha
            ipe2Latn["\x00DB"] = "d"; // da
            ipe2Latn["\x00DC"] = "dh"; // dha
            ipe2Latn["\x00DD"] = "n"; // na

            // labial stops
            ipe2Latn["\x00DE"] = "p"; // pa
            ipe2Latn["\x00DF"] = "ph"; // pha
            ipe2Latn["\x00E0"] = "b"; // ba
            ipe2Latn["\x00E1"] = "bh"; // bha
            ipe2Latn["\x00E2"] = "m"; // ma

            // liquids, fricatives, etc.
            ipe2Latn["\x00E3"] = "y"; // ya
            ipe2Latn["\x00E4"] = "r"; // ra
            ipe2Latn["\x00E5"] = "l"; // la
            ipe2Latn["\x00E6"] = "v"; // va
            ipe2Latn["\x00E7"] = "s"; // sa
            ipe2Latn["\x00E8"] = "h"; // ha
            ipe2Latn["\x00E9"] = "\x1E37"; // l underdot a
        }

        public static string Convert(string ipe)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in ipe)
            {
                if (ipe2Latn.ContainsKey(c.ToString()))
                    sb.Append(ipe2Latn[c.ToString()]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
