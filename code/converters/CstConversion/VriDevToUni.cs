using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CST.Conversion
{
    public static class VriDevToUni
    {
        static VriDevToUni()
        {
            charMap = new Dictionary<char, string>();

            charMap['k'] = "\x0915"; // ka
            charMap['K'] = "\x0916"; // kha
            charMap['g'] = "\x0917"; // ga
            charMap['G'] = "\x0918"; // gha
            charMap['N'] = "\x0919"; // n>a

            charMap['c'] = "\x091A"; // ca
            charMap['C'] = "\x091B"; // cha
            charMap['j'] = "\x091C"; // ja
            charMap['J'] = "\x091D"; // jha
            charMap['Y'] = "\x091E"; // �a

            charMap['x'] = "\x091F"; // t.a
            charMap['X'] = "\x0920"; // t.ha
            charMap['z'] = "\x0921"; // d.a
            charMap['Z'] = "\x0922"; // d.ha
            charMap['f'] = "\x0923"; // n.a

            charMap['t'] = "\x0924"; // ta
            charMap['T'] = "\x0925"; // tha
            charMap['d'] = "\x0926"; // da
            charMap['D'] = "\x0927"; // dha
            charMap['n'] = "\x0928"; // na

            charMap['p'] = "\x092A"; // pa
            charMap['P'] = "\x092B"; // pha
            charMap['b'] = "\x092C"; // ba
            charMap['B'] = "\x092D"; // bha
            charMap['m'] = "\x092E"; // ma

            charMap['y'] = "\x092F"; // ya
            charMap['r'] = "\x0930"; // ra
            charMap['l'] = "\x0932"; // la
            charMap['L'] = "\x0933"; // l.a
            charMap['v'] = "\x0935"; // va
            charMap['�'] = "\x0936"; // sha (palatal)
            charMap['�'] = "\x0937"; // sha (retroflex)
            charMap['s'] = "\x0938"; // sa
            charMap['h'] = "\x0939"; // ha
            
            // conjuncts
            charMap['�'] = "\x0915\x094D\x0937"; // ks.a
            charMap['�'] = "\x0919\x094D\x0915"; // n>ka
            charMap['�'] = "\x0919\x094D\x0916"; // n>kha
            charMap['�'] = "\x0919\x094D\x0916\x094D\x092F"; // n>khya
            charMap['�'] = "\x0919\x094D\x0917"; // n>ga
            charMap['�'] = "\x0919\x094D\x0918"; // n>gha

            charMap['�'] = "\x091C\x094D\x091E"; // j�a

            charMap['�'] = "\x091F\x094D\x091F"; // t.t.a
            charMap['�'] = "\x091F\x094D\x0920"; // t.t.ha
            charMap['\x008A'] = "\x0921\x094D\x0921"; // d.d.a
            charMap['�'] = "\x0921\x094D\x0921"; // d.d.a
            charMap['�'] = "\x0921\x094D\x0922"; // d.d.ha

            charMap['H'] = "\x0924\x094D\x0924"; // tta
            charMap['�'] = "\x0924\x094D\x0930"; // tra
            charMap['�'] = "\x0926\x094D\x0917"; // dga
            charMap['�'] = "\x0926\x094D\x0918"; // dgha
            charMap['�'] = "\x0926\x094D\x0926"; // dda
            charMap['�'] = "\x0926\x094D\x0927"; // ddha
            charMap['�'] = "\x0926\x094D\x092D"; // dbha
            charMap['�'] = "\x0926\x094D\x092E"; // dma
            charMap['�'] = "\x0926\x094D\x092F"; // dya
            charMap['�'] = "\x0926\x094D\x0930"; // dra
            charMap['�'] = "\x0926\x094D\x0935"; // dva

            charMap['�'] = "\x0936\x094D\x0930"; // shra (palatal sh)

            charMap['�'] = "\x0939\x094D\x0928"; // hna
            charMap['�'] = "\x0939\x094D\x092E"; // hma
            charMap['�'] = "\x0939\x094D\x092F"; // hya
            charMap['�'] = "\x0939\x094D\x0930"; // hra
            charMap['�'] = "\x0939\x094D\x0932"; // hla
            charMap['�'] = "\x0939\x094D\x0935"; // hva

            //charMap['�'] = ""; // following ya (VRI: BF). not in dictionary.
            

            // half chars

            charMap['E'] = "\x0915\x094D"; // 1/2 ka
            charMap['�'] = "\x0915\x094D\x0937\x094D"; // 1/2 ks.a
            charMap['�'] = "\x0916\x094D"; // 1/2 kha
            charMap['\x009F'] = "\x0917\x094D"; // 1/2 ga
            charMap['�'] = "\x0917\x094D"; // 1/2 ga
            charMap['�'] = "\x0918\x094D"; // 1/2 gha

            charMap['�'] = "\x091A\x094D"; // 1/2 ca
            charMap['\x009C'] = "\x091A\x094D"; // 1/2 ca
            charMap['�'] = "\x091C\x094D"; // 1/2 ja
            charMap['�'] = "\x091D\x094D"; // 1/2 jha (not in dictionary)
            charMap['�'] = "\x091E\x094D"; // 1/2 �a

            charMap['�'] = "\x0923\x094D"; // 1/2 n.a

            charMap['�'] = "\x0924\x094D"; // 1/2 ta
            charMap['�'] = "\x0924\x094D\x0924\x094D"; // 1/2 tta
            charMap['�'] = "\x0925\x094D"; // 1/2 tha
            charMap['\x009B'] = "\x0927\x094D"; // 1/2 dha
            charMap['�'] = "\x0928\x094D"; // 1/2 na
            charMap['�'] = "\x0928\x094D\x0928\x094D"; // 1/2 nna

            charMap['F'] = "\x092A\x094D"; // 1/2 pa
            charMap['�'] = "\x092A\x094D\x0924\x094D"; // 1/2 pta
            charMap['�'] = "\x092A\x094D\x0930\x094D"; // 1/2 pra
            charMap['�'] = "\x092B\x094D"; // 1/2 pha
            charMap['\x0088'] = "\x092C\x094D"; // 1/2 ba
            charMap['\x008F'] = "\x092C\x094D"; // 1/2 ba
            charMap['�'] = "\x092D\x094D"; // 1/2 bha
            charMap['\x009D'] = "\x092E\x094D"; // 1/2 ma
            charMap['�'] = "\x092E\x094D"; // 1/2 ma (occurs in 2 places in the font)

            charMap['\x0086'] = "\x092F\x094D"; // 1/2 ya
            charMap['\x008D'] = "\x092F\x094D"; // 1/2 ya
            charMap['\x00A3'] = "\x0932\x094D"; // 1/2 la
            charMap['�'] = "\x0933\x094D"; // 1/2 l.a
            charMap['�'] = "\x0935\x094D"; // 1/2 va
            charMap['�'] = "\x0936\x094D"; // 1/2 sha (palatal)
            charMap['�'] = "\x0937\x094D"; // 1/2 sha (retroflex)
            charMap['\x0089'] = "\x0938\x094D"; // 1/2 sa
            charMap['\x0090'] = "\x0938\x094D"; // 1/2 sa
            charMap['\x2030'] = "\x0938\x094D"; // 1/2 sa
            charMap['�'] = "\x0938\x094D\x0928\x094D"; // 1/2 sna

            // followers
            charMap['Q'] = "\x0930\x094D"; // hook ra
            charMap['R'] = "\x094D\x0930"; // diagonal ra
            charMap['�'] = "\x094D\x0930"; // caret ra

            indVowels = new HashSet<char>();
            indVowels.Add('S'); // a
            indVowels.Add('W'); // uu
            indVowels.Add('a'); // e
            indVowels.Add('q'); // i
            indVowels.Add('w'); // u

            depVowels = new HashSet<char>();
            depVowels.Add('A'); // aa
            depVowels.Add('I'); // ii
            depVowels.Add('O'); // uu (low form)
            depVowels.Add('U'); // uu
            depVowels.Add('V'); // i (long arm)
            depVowels.Add('e'); // e
            depVowels.Add('i'); // i
            depVowels.Add('o'); // o
            depVowels.Add('u'); // u
            depVowels.Add('\x008E'); // u (low form)
            depVowels.Add('\x2021'); // u (another low form)
            depVowels.Add('\x00C8'); // au
            depVowels.Add('\x00D4'); // ai

            // break this up in pre-processing step
            // 02DC = i + m. (maybe not break up. they both need to jump over the following consonants.)

            fullConsonants = new HashSet<string>();
            fullConsonants.Add("B"); // bha
            fullConsonants.Add("C"); // cha
            fullConsonants.Add("D"); // dha
            fullConsonants.Add("G"); // gha
            fullConsonants.Add("H"); // tta
            fullConsonants.Add("J"); // jha
            fullConsonants.Add("K"); // kha
            fullConsonants.Add("L"); // l.a
            fullConsonants.Add("N"); // n>a
            fullConsonants.Add("P"); // pha
            fullConsonants.Add("T"); // tha
            fullConsonants.Add("X"); // t.ha
            fullConsonants.Add("Y"); // �a
            fullConsonants.Add("Z"); // d.ha
            fullConsonants.Add("b"); // ba
            fullConsonants.Add("c"); // ca
            fullConsonants.Add("d"); // da
            fullConsonants.Add("f"); // n.a
            fullConsonants.Add("g"); // ga
            fullConsonants.Add("h"); // ha
            fullConsonants.Add("j"); // ja
            fullConsonants.Add("k"); // ka
            fullConsonants.Add("l"); // la
            fullConsonants.Add("m"); // ma
            fullConsonants.Add("n"); // na
            fullConsonants.Add("p"); // pa
            fullConsonants.Add("r"); // ra
            fullConsonants.Add("s"); // sa
            fullConsonants.Add("t"); // ta
            fullConsonants.Add("v"); // va
            fullConsonants.Add("x"); // t.a
            fullConsonants.Add("y"); // ya
            fullConsonants.Add("z"); // d.a
            fullConsonants.Add("�"); // dda
            fullConsonants.Add("�"); // t.t.a
            fullConsonants.Add("�"); // dma
            fullConsonants.Add("�"); // t.t.ha
            fullConsonants.Add("�"); // tra
            fullConsonants.Add("�"); // dra
            fullConsonants.Add("�"); // dva
            fullConsonants.Add("�"); // dya
            fullConsonants.Add("\x008A"); // d.d.a
            fullConsonants.Add("�"); // d.d.a (char is in two places in font)
            fullConsonants.Add("�"); // n>kha
            fullConsonants.Add("�"); // n>gha
            fullConsonants.Add("�"); // hya
            fullConsonants.Add("�"); // hma
            fullConsonants.Add("�"); // n>ka
            fullConsonants.Add("�"); // hva
            fullConsonants.Add("�"); // n>khya
            fullConsonants.Add("�"); // n>ga
            fullConsonants.Add("�"); // d.d.ha
            fullConsonants.Add("�"); // following ya (VRI: BF). not in dictionary.
            fullConsonants.Add("�"); // ddha
            fullConsonants.Add("�"); // dgha
            fullConsonants.Add("�"); // dga
            fullConsonants.Add("�"); // dbha
            fullConsonants.Add("�"); // ks.a
            fullConsonants.Add("�"); // shra (palatal sh)
            fullConsonants.Add("�"); // hna
            fullConsonants.Add("�"); // hra
            fullConsonants.Add("�"); // hla
            fullConsonants.Add("�"); // j�a
            fullConsonants.Add("�"); // sha (palatal)
            fullConsonants.Add("�"); // sha (retroflex)
            

            fullConsonantsCC = SetToCharacterClass(fullConsonants);


            halfConsonants = new HashSet<string>();
            halfConsonants.Add("E"); // 1/2 ka
            halfConsonants.Add("F"); // 1/2 pa
            halfConsonants.Add("\x0086"); // 1/2 ya
            halfConsonants.Add("\x008D"); // 1/2 ya
            halfConsonants.Add("\x0088"); // 1/2 ba
            halfConsonants.Add("\x008F"); // 1/2 ba
            halfConsonants.Add("\x0089"); // 1/2 sa
            halfConsonants.Add("\x0090"); // 1/2 sa
            halfConsonants.Add("\x2030"); // 1/2 sa
            halfConsonants.Add("\x009D"); // 1/2 ma
            halfConsonants.Add("�"); // 1/2 ma (occurs in 2 places)
            halfConsonants.Add("\x00A3"); // 1/2 la
            halfConsonants.Add("�"); // 1/2 ta
            halfConsonants.Add("�"); // 1/2 l.a
            halfConsonants.Add("�"); // 1/2 na
            halfConsonants.Add("�"); // 1/2 va
            halfConsonants.Add("�"); // 1/2 �a
            halfConsonants.Add("�"); // 1/2 n.a
            halfConsonants.Add("�"); // 1/2 kha
            halfConsonants.Add("�"); // 1/2 ja
            halfConsonants.Add("�"); // 1/2 bha
            halfConsonants.Add("�"); // 1/2 tha
            halfConsonants.Add("�"); // 1/2 pha
            halfConsonants.Add("�"); // 1/2 jha (not in dictionary)
            halfConsonants.Add("�"); // 1/2 sha (palatal)
            halfConsonants.Add("�"); // 1/2 sha (retroflex)
            halfConsonants.Add("�"); // 1/2 gha

            halfConsonants.Add("\x009F"); // 1/2 ga
            halfConsonants.Add("�"); // 1/2 ga
            halfConsonants.Add("�"); // 1/2 ca
            halfConsonants.Add("\x009C"); // 1/2 ca
            halfConsonants.Add("�"); // 1/2 ks.a
            halfConsonants.Add("\x009B"); // 1/2 dha
            halfConsonants.Add("�"); // 1/2 tta
            halfConsonants.Add("�"); // 1/2 pra
            halfConsonants.Add("�"); // 1/2 nna
            halfConsonants.Add("�"); // 1/2 sna
            halfConsonants.Add("�"); // 1/2 pta
            halfConsonantsCC = SetToCharacterClass(halfConsonants);

            followers = new HashSet<string>();
            followers.Add("Q"); // hook ra
            followers.Add("R"); // diagonal ra
            followers.Add("�"); // caret ra
            followers.Add("�"); // diagonal 1/2 na
            followers.Add("�"); // hook ra + niggahita
            followersCC = SetToCharacterClass(followers);
        }

        private static ISet<char> indVowels;
        private static ISet<char> depVowels;
        private static ISet<string> fullConsonants;
        private static string fullConsonantsCC;
        private static ISet<string> halfConsonants;
        private static string halfConsonantsCC;
        private static ISet<string> followers;
        private static string followersCC;

        private static IDictionary<char, string> charMap;

        public static string ConvertBook(string str)
        {
            str = Pass1(str);
            str = Pass2(str);
            str = Pass3(str);

            return str;
        }

        // vowels and niggahita to Unicode, plus some simplifications
        public static string Pass1(string str)
        {
            // 3-to-1 replacements
            str = str.Replace("SA�", "\x0911"); // ind. a + dep. aa + chandra -> ind. chandra o

            // 2-to-1 replacements
            str = str.Replace("SA", "\x0906"); // ind. a + dep. aa -> ind. aa
            str = str.Replace("qQ", "\x0908"); // ind. i + hook r -> ind. ii
            str = str.Replace("a�", "\x090D"); // ind. e + chandra -> ind. chandra e
            str = str.Replace("ae", "\x0910"); // ind. e + dep. e -> ind. ai
            str = str.Replace("So", "\x0913"); // ind. a + dep. o -> ind. o
            str = str.Replace("S�", "\x0914"); // ind. a + dep. au -> ind. au
            str = str.Replace("A�", "\x0949"); // dep. aa + chandra -> dep. chandra o

            str = str.Replace("�A", "H"); // tta is encoded as 1/2 tta plus aa sign
            
            // 2-to-2 replacements
            str = str.Replace("A�", "\x094B\x0902"); // dep. aa + em. -> dep. o + m.
            str = str.Replace("A\x009A", "\x094B\x0902"); // dep. aa + em. -> dep. o + m.
			str = str.Replace("q�", "\x0908\x0902"); // ind. i + combined hook r and niggahita -> ind. ii + m.

            str = str.Replace("�A", "pR"); // pra is encoded as 1/2 pra plus aa sign
            str = str.Replace("�A", "�n"); // nna is encoded as 1/2 nna plus aa sign
            str = str.Replace("�A", "\x0090n"); // sna is encoded as 1/2 sna plus aa sign
            str = str.Replace("�A", "Ft"); // pta is encoded as 1/2 pta plus aa sign

            // 1-to-2 replacements
            str = str.Replace("\x0099", "\x0940\x0902"); // combined dep. ii + m. -> dep. ii + m.
            str = str.Replace("\x2122", "\x0940\x0902"); // combined dep. ii + m. -> dep. ii + m.
            str = str.Replace("�", "\x0947\x0902"); // combined dep e + m. -> dep. e + m.
            str = str.Replace("\x009A", "\x0947\x0902"); // combined dep e + m. -> dep. e + m.
            str = str.Replace("�", "\x0948\x0902"); // combined dep. ai + m. -> dep. ai + m.

            str = str.Replace("�", "r\x0942"); // ruu (r stays VRI encoded for now)
            str = str.Replace("�", "h\x0943"); // h + vocalic r (h stays VRI encoded for now)
            
            // 1-to-1 replacements
            str = str.Replace("�", "\x0901"); // chandrabindu
            str = str.Replace("M", "\x0902"); // niggahita
            str = str.Replace("S", "\x0905"); // ind. a
            str = str.Replace("q", "\x0907"); // ind. i
            str = str.Replace("w", "\x0909"); // ind. u 
            str = str.Replace("W", "\x090A"); // ind. uu
            str = str.Replace("�", "\x090B"); // ind. vocalic r
            str = str.Replace("a", "\x090F"); // ind. e

            str = str.Replace("A", "\x093E"); // dep. aa
            str = str.Replace("I", "\x0940"); // dep. ii
            str = str.Replace("u", "\x0941"); // dep. u
            str = str.Replace("\x008E", "\x0941"); // dep. u (low form)
            str = str.Replace("\x2021", "\x0941"); // dep. u (2nd low form)
            str = str.Replace("\x0087", "\x0941"); // dep. u (2nd low form)
            str = str.Replace("\x008C", "\x0941"); // dep. u (attaches to r to make ru)
            str = str.Replace("U", "\x0942"); // dep. uu
            str = str.Replace("O", "\x0942"); // dep. uu (low form)
            str = str.Replace("e", "\x0947"); // dep. e
            str = str.Replace("�", "\x0948"); // dep. ai
            str = str.Replace("o", "\x094B"); // dep. o
            str = str.Replace("�", "\x094C"); // dep. au
            str = str.Replace("�", "\x0943"); // dep. vocalic r

            str = str.Replace("�", "\x094D"); // visible halant (virama)
            str = str.Replace("�", "\x0964"); // danda
            str = str.Replace("\x2039", "\x0965"); // double danda
            str = str.Replace("�", "\x0970"); // abbreviation sign
            str = str.Replace("\x0091", "\x2018"); // left single quote
            str = str.Replace("\x0092", "\x2019"); // right single quote
            str = str.Replace("\x0093", "\x201C"); // left double quote
            str = str.Replace("\x0094", "\x201D"); // right double quote
            str = str.Replace("\x0096", "\x2013"); // en dash
            str = str.Replace("\x0097", "\x2014"); // em dash

            // it seems that all of the colons in the dictionary are visarga.
            str = str.Replace(":", "\x0903"); // visarga
            str = str.Replace("�", "\x093D"); // avagraha

            //str = str.Replace("", ""); // 

            // 1-to-0 replacements
            str = str.Replace("�", ""); // seems to be used for spacing within words

            return str;
        }

        private static string Pass2(string str)
        {
            string dependentVowelsCC = "[\x093E-\x094C]";
            string shortIVowels = "[iV\x0098]";
            string underDot = "[�]";

            string syllablePattern = "(" +
                underDot + "*" +   // can precede the i vowels
                shortIVowels + "*" +
                halfConsonantsCC + "*" +
                underDot + "*" +   // precedes the consonant
                fullConsonantsCC + // no quantifier. match exactly one.
                followersCC + "*" +
                dependentVowelsCC + "*" +
                followersCC + "*" + ")";

            str = Regex.Replace(str, syllablePattern, new MatchEvaluator(ConvertSyllable));

            return str;
        }

        // insert ZWJ in some Devanagari conjuncts
        private static string Pass3(string str)
        {

            str = str.Replace("\x0915\x094D\x0915", "\x0915\x094D\x200D\x0915"); // ka + ka
            str = str.Replace("\x0915\x094D\x0932", "\x0915\x094D\x200D\x0932"); // ka + la
            str = str.Replace("\x0915\x094D\x0935", "\x0915\x094D\x200D\x0935"); // ka + va
            str = str.Replace("\x091A\x094D\x091A", "\x091A\x094D\x200D\x091A"); // ca + ca
            str = str.Replace("\x091C\x094D\x091C", "\x091C\x094D\x200D\x091C"); // ja + ja
            str = str.Replace("\x091E\x094D\x091A", "\x091E\x094D\x200D\x091A"); // �a + ca
            str = str.Replace("\x091E\x094D\x091C", "\x091E\x094D\x200D\x091C"); // �a + ja
            str = str.Replace("\x091E\x094D\x091E", "\x091E\x094D\x200D\x091E"); // �a + �a
            str = str.Replace("\x0928\x094D\x0928", "\x0928\x094D\x200D\x0928"); // na + na
            str = str.Replace("\x092A\x094D\x0932", "\x092A\x094D\x200D\x0932"); // pa + la
            str = str.Replace("\x0932\x094D\x0932", "\x0932\x094D\x200D\x0932"); // la + la

            return str;
        }

        public static string ConvertSyllable(Match m)
        {
            string s = m.Value;

            // Move short i vowels after the consonants.
            // We cannot assume that the i vowel comes first in the syllable. There is a underdot
            // character that sometimes precedes it. 
            if (s.Contains("i")) // i with short arm
            {
                s = s.Replace("i", "");
                s = s + "\x093F";
            }

            if (s.Contains("V")) // i with long arm
            {
                s = s.Replace("V", "");
                s = s + "\x093F";
            }

            if (s.Contains("\x0098")) // i with short arm and niggahita
            {
                s = s.Replace("\x0098", "");
                s = s + "\x093F\x0902";
            }

            // if there's a "hook ra", move "ra" to the first position in the syllable
            if (s.IndexOf("Q") >= 0)
            {
                s = s.Replace("Q", "");
                s = "Q" + s;
            }

            // hook ra + niggahita
            if (s.IndexOf("�") >= 0)
            {
                s = s.Replace("�", "");
                s = "Q" + s + "\x0902";
            }

            // cases where underdot character precedes full consonant
            s = s.Replace("�k", "\x0958");
            s = s.Replace("�g", "\x0959");
            s = s.Replace("�j", "\x095B");
            s = s.Replace("�z", "\x095C");
            s = s.Replace("�Z", "\x095D");
            // also precedes ka, ga and ja


            string s2 = "";
            foreach (char c in s.ToCharArray())
            {
                if (charMap.ContainsKey(c))
                    s2 += charMap[c];
                else if (c >= '\x0900' && c <= '\x097F')
                    s2 += c;
                else
                {
                    Console.WriteLine(c);
                }
            }

            return s2;
        }

        private static string SetToCharacterClass(ISet<string> set)
        {
            string cc = "[";
            foreach (string s in set)
            {
                cc += s;
            }
            cc += "]";

            return cc;
    }

        public static string Foo (string str)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in str.ToCharArray())
            {
                // characters that were converted to Unicode in pre-processing step get passed through
                if (c >= '\x0900' && c <= '\x097F')
                    sb.Append(c);
                // Devanagari digits
                else if (c >= '0' && c <= '9')
                    sb.Append(c + 0x0936);

                /*
                else if (fullConsonants.Contains(c))
                {
                    if (clusterHasFullChar)
                    {
                        sb.Append(ConvertCluster(cluster));
                        cluster = "";
                    }

                    clusterHasFullChar = true;
                    cluster += c;
                }
                else if (halfConsonants.Contains(c))
                {
                    if (clusterHasFullChar)
                    {
                        sb.Append(ConvertCluster(cluster));
                        cluster = "";
                        clusterHasFullChar = false;
                    }

                    cluster += c;
                }
                else if (followers.Contains(c))
                {
                    cluster += c;
                }
                */
            }

            //sb.Append(ConvertCluster(cluster));

            return sb.ToString();
        }
        /*
        characters not in the dictionary:
        � (\x00BF) combining ya, open on front
        � (\x00D7) r combined with ???
        � (\x00DB) half tra
        � (\x00E0) half ha
        � (\x00E2) half stra?  
        � (\x00E6) om
        � (\x00EE) eyelash ra
        � (\x00EF) dep. vocalic rr
        � (\x00FB) 1/2 khr
        � (\x00FF) a spacer??
        */
    }
}
