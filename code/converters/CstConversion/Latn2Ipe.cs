using System;
using System.Collections.Generic;
using System.Text;

namespace CST.Conversion
{
    public static class Latn2Ipe
    {
        private static IDictionary<string, string> latn2Ipe;
        private static ISet<char> latnAspiratables;

        static Latn2Ipe()
        {
            latn2Ipe = new Dictionary<string, string>();

            latn2Ipe["\u1E43"] = "\u00C0"; // niggahita

            // vowels
            latn2Ipe["a"] = "\u00C1"; // a
            latn2Ipe["\u0101"] = "\u00C2"; // aa
            latn2Ipe["i"] = "\u00C3"; // i
            latn2Ipe["\u012B"] = "\u00C4"; // ii
            latn2Ipe["u"] = "\u00C5"; // u
            latn2Ipe["\u016B"] = "\u00C6"; // uu
            latn2Ipe["e"] = "\u00C7"; // e
            latn2Ipe["o"] = "\u00C8"; // o

            // velar stops
            latn2Ipe["k"] = "\u00C9"; // ka
            latn2Ipe["kh"] = "\u00CA"; // kha
            latn2Ipe["g"] = "\u00CB"; // ga
            latn2Ipe["gh"] = "\u00CC"; // gha
            latn2Ipe["\u1E45"] = "\u00CD"; // n overdot a

            // palatal stops
            latn2Ipe["c"] = "\u00CE"; // ca
            latn2Ipe["ch"] = "\u00CF"; // cha
            latn2Ipe["j"] = "\u00D0"; // ja
            latn2Ipe["jh"] = "\u00D1"; // jha
            latn2Ipe["\u00F1"] = "\u00D2"; // n tilde a

            // retroflex stops
            latn2Ipe["\u1E6D"] = "\u00D3"; // t underdot a
            latn2Ipe["\u1E6Dh"] = "\u00D4"; // t underdot ha
            latn2Ipe["\u1E0D"] = "\u00D5"; // d underdot a
            latn2Ipe["\u1E0Dh"] = "\u00D6"; // d underdot ha
            // D7 multiplication sign is unused in IPE
            latn2Ipe["\u1E47"] = "\u00D8"; // n underdot a

            // dental stops
            latn2Ipe["t"] = "\u00D9"; // ta
            latn2Ipe["th"] = "\u00DA"; // tha
            latn2Ipe["d"] = "\u00DB"; // da
            latn2Ipe["dh"] = "\u00DC"; // dha
            latn2Ipe["n"] = "\u00DD"; // na

            // labial stops
            latn2Ipe["p"] = "\u00DE"; // pa
            latn2Ipe["ph"] = "\u00DF"; // pha
            latn2Ipe["b"] = "\u00E0"; // ba
            latn2Ipe["bh"] = "\u00E1"; // bha
            latn2Ipe["m"] = "\u00E2"; // ma

            // liquids, fricatives, etc.
            latn2Ipe["y"] = "\u00E3"; // ya
            latn2Ipe["r"] = "\u00E4"; // ra
            latn2Ipe["l"] = "\u00E5"; // la
            latn2Ipe["v"] = "\u00E6"; // va
            latn2Ipe["s"] = "\u00E7"; // sa
            latn2Ipe["h"] = "\u00E8"; // ha
            latn2Ipe["\u1E37"] = "\u00E9"; // l underdot a

            latnAspiratables = new HashSet<char>();
            latnAspiratables.Add('k');
            latnAspiratables.Add('g');
            latnAspiratables.Add('c');
            latnAspiratables.Add('j');
            latnAspiratables.Add('\u1E6D'); // t underdot
            latnAspiratables.Add('\u1E0D'); // d underdot
            latnAspiratables.Add('t');
            latnAspiratables.Add('d');
            latnAspiratables.Add('p');
            latnAspiratables.Add('b');
        }

        public static string Convert(string latn)
        {
            StringBuilder sb = new StringBuilder();
            char[] arr = latn.ToLower().ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                char c = arr[i];
                if (i < arr.Length - 1 && latnAspiratables.Contains(c) && arr[i + 1] == 'h')
                {
                    string str = c.ToString() + "h";
                    if (latn2Ipe.ContainsKey(str))
                        sb.Append(latn2Ipe[str]);
                    i++;
                }
                else
                {
                    if (latn2Ipe.ContainsKey(c.ToString()))
                        sb.Append(latn2Ipe[c.ToString()]);
                    else
                        sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
