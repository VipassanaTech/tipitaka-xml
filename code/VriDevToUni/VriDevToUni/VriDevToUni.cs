using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VRI.Conversion
{
    public static class VriDevToUni
    {
        static VriDevToUni()
        {
            charMap = new Dictionary<char, string>();

            charMap['k'] = "\u0915"; // ka
            charMap['K'] = "\u0916"; // kha
            charMap['g'] = "\u0917"; // ga
            charMap['G'] = "\u0918"; // gha
            charMap['N'] = "\u0919"; // n>a

            charMap['c'] = "\u091A"; // ca
            charMap['C'] = "\u091B"; // cha
            charMap['j'] = "\u091C"; // ja
            charMap['J'] = "\u091D"; // jha
            charMap['Y'] = "\u091E"; // ña

            charMap['x'] = "\u091F"; // t.a
            charMap['X'] = "\u0920"; // t.ha
            charMap['z'] = "\u0921"; // d.a
            charMap['Z'] = "\u0922"; // d.ha
            charMap['f'] = "\u0923"; // n.a

            charMap['t'] = "\u0924"; // ta
            charMap['T'] = "\u0925"; // tha
            charMap['d'] = "\u0926"; // da
            charMap['D'] = "\u0927"; // dha
            charMap['n'] = "\u0928"; // na

            charMap['p'] = "\u092A"; // pa
            charMap['P'] = "\u092B"; // pha
            charMap['b'] = "\u092C"; // ba
            charMap['B'] = "\u092D"; // bha
            charMap['m'] = "\u092E"; // ma

            charMap['y'] = "\u092F"; // ya
            charMap['r'] = "\u0930"; // ra
            charMap['l'] = "\u0932"; // la
            charMap['L'] = "\u0933"; // l.a
            charMap['v'] = "\u0935"; // va
            charMap['Õ'] = "\u0936"; // sha (palatal)
            charMap['ú'] = "\u0937"; // sha (retroflex)
            charMap['s'] = "\u0938"; // sa
            charMap['h'] = "\u0939"; // ha
            
            // conjuncts
            charMap['ö'] = "\u0915\u094D\u0937"; // ks.a
            charMap['¸'] = "\u0919\u094D\u0915"; // n>ka
            charMap['\u0327'] = "\u0919\u094D\u0915"; // n>ka
            charMap['±'] = "\u0919\u094D\u0916"; // n>kha
            charMap['º'] = "\u0919\u094D\u0916\u094D\u092F"; // n>khya
            charMap['½'] = "\u0919\u094D\u0917"; // n>ga
            charMap['³'] = "\u0919\u094D\u0918"; // n>gha

            charMap['ô'] = "\u091C\u094D\u091E"; // jña

            charMap['¢'] = "\u091F\u094D\u091F"; // t.t.a
            charMap['§'] = "\u091F\u094D\u0920"; // t.t.ha
            charMap['ù'] = "\u0920\u094D\u0920"; // t.ht.ha
            charMap['\u008A'] = "\u0921\u094D\u0921"; // d.d.a
            charMap['°'] = "\u0921\u094D\u0921"; // d.d.a
            charMap['\u0160'] = "\u0921\u094D\u0921"; // d.d.a
            charMap['¾'] = "\u0921\u094D\u0922"; // d.d.ha

            charMap['H'] = "\u0924\u094D\u0924"; // tta
            charMap['©'] = "\u0924\u094D\u0930"; // tra
            charMap['Ý'] = "\u0926\u094D\u0917"; // dga
            charMap['Ë'] = "\u0926\u094D\u0918"; // dgha
            charMap['¡'] = "\u0926\u094D\u0926"; // dda
            charMap['Â'] = "\u0926\u094D\u0927"; // ddha
            charMap['Þ'] = "\u0926\u094D\u092D"; // dbha
            charMap['¤'] = "\u0926\u094D\u092E"; // dma
            charMap['¬'] = "\u0926\u094D\u092F"; // dya
            charMap['ª'] = "\u0926\u094D\u0930"; // dra
            charMap['«'] = "\u0926\u094D\u0935"; // dva

            charMap['ü'] = "\u0936\u094D\u0930"; // shra (palatal sh)

            charMap['ø'] = "\u0939\u094D\u0928"; // hna
            charMap['µ'] = "\u0939\u094D\u092E"; // hma
            charMap['\u03BC'] = "\u0939\u094D\u092E"; // hma
            charMap['´'] = "\u0939\u094D\u092F"; // hya
            charMap['\u0301'] = "\u0939\u094D\u092F"; // hya
            charMap['ó'] = "\u0939\u094D\u0930"; // hra
            charMap['õ'] = "\u0939\u094D\u0932"; // hla
            charMap['¹'] = "\u0939\u094D\u0935"; // hva

            //charMap['¿'] = ""; // following ya (VRI: BF). not in dictionary.
            

            // half chars

            charMap['E'] = "\u0915\u094D"; // 1/2 ka
            charMap['ç'] = "\u0915\u094D\u0937\u094D"; // 1/2 ks.a
            charMap['¼'] = "\u0916\u094D"; // 1/2 kha
            charMap['\u009F'] = "\u0917\u094D"; // 1/2 ga
            charMap['Ÿ'] = "\u0917\u094D"; // 1/2 ga
            charMap['Ð'] = "\u0918\u094D"; // 1/2 gha

            charMap['œ'] = "\u091A\u094D"; // 1/2 ca
            charMap['\u009C'] = "\u091A\u094D"; // 1/2 ca
            charMap['À'] = "\u091C\u094D"; // 1/2 ja
            charMap['Í'] = "\u091D\u094D"; // 1/2 jha (not in dictionary)
            charMap['²'] = "\u091E\u094D"; // 1/2 ña

            charMap['»'] = "\u0923\u094D"; // 1/2 n.a

            charMap['¥'] = "\u0924\u094D"; // 1/2 ta
            charMap['Ø'] = "\u0924\u094D\u0924\u094D"; // 1/2 tta
            charMap['É'] = "\u0925\u094D"; // 1/2 tha
            charMap['\u009B'] = "\u0927\u094D"; // 1/2 dha
            charMap['\u203A'] = "\u0927\u094D"; // 1/2 dha
            charMap['®'] = "\u0928\u094D"; // 1/2 na
            charMap['Ú'] = "\u0928\u094D\u0928\u094D"; // 1/2 nna

            charMap['F'] = "\u092A\u094D"; // 1/2 pa
            charMap['ð'] = "\u092A\u094D\u0924\u094D"; // 1/2 pta
            charMap['á'] = "\u092A\u094D\u0930\u094D"; // 1/2 pra
            charMap['Ê'] = "\u092B\u094D"; // 1/2 pha
            charMap['\u0088'] = "\u092C\u094D"; // 1/2 ba
            charMap['\u008F'] = "\u092C\u094D"; // 1/2 ba
            charMap['\u02C6'] = "\u092C\u094D"; // 1/2 ba
            charMap['Ä'] = "\u092D\u094D"; // 1/2 bha
            charMap['\u009D'] = "\u092E\u094D"; // 1/2 ma
            charMap['ý'] = "\u092E\u094D"; // 1/2 ma (occurs in 2 places in the font)

            charMap['\u0086'] = "\u092F\u094D"; // 1/2 ya
            charMap['\u008D'] = "\u092F\u094D"; // 1/2 ya
            charMap['\u2020'] = "\u092F\u094D"; // 1/2 ya
            charMap['\u00A3'] = "\u0932\u094D"; // 1/2 la
            charMap['¨'] = "\u0933\u094D"; // 1/2 l.a
            charMap['¯'] = "\u0935\u094D"; // 1/2 va
            charMap['\u0304'] = "\u0935\u094D"; // 1/2 va
            charMap['Î'] = "\u0936\u094D"; // 1/2 sha (palatal)
            charMap['Ï'] = "\u0937\u094D"; // 1/2 sha (retroflex)
            charMap['\u0089'] = "\u0938\u094D"; // 1/2 sa
            charMap['\u0090'] = "\u0938\u094D"; // 1/2 sa
            charMap['\u2030'] = "\u0938\u094D"; // 1/2 sa
            charMap['ê'] = "\u0938\u094D\u0928\u094D"; // 1/2 sna

            // followers
            charMap['Q'] = "\u0930\u094D"; // hook ra
            charMap['R'] = "\u094D\u0930"; // diagonal ra
            charMap['Ã'] = "\u094D\u0930"; // caret ra

            indVowels = new HashSet<string>();
            indVowels.Add("S"); // a
            indVowels.Add("W"); // uu
            indVowels.Add("a"); // e
            indVowels.Add("q"); // i
            indVowels.Add("w"); // u

            depVowels = new HashSet<string>();
            depVowels.Add("A"); // aa
            depVowels.Add("I"); // ii
            depVowels.Add("O"); // uu (low form)
            depVowels.Add("U"); // uu
            depVowels.Add("V"); // i (long arm)
            depVowels.Add("e"); // e
            depVowels.Add("i"); // i
            depVowels.Add("o"); // o
            depVowels.Add("u"); // u
            depVowels.Add("\u008E"); // u (low form)
            depVowels.Add("\u2021"); // u (another low form)
            depVowels.Add("\u00C8"); // au
            depVowels.Add("\u00D4"); // ai
            depVowels.Add("\u00ED"); // vocalic r

            depVowelsCC = SetToCharacterClass(depVowels);

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
            fullConsonants.Add("Y"); // ña
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
            fullConsonants.Add("¡"); // dda
            fullConsonants.Add("¢"); // t.t.a
            fullConsonants.Add("ù"); //t.ht.ha
            fullConsonants.Add("¤"); // dma
            fullConsonants.Add("§"); // t.t.ha
            fullConsonants.Add("©"); // tra
            fullConsonants.Add("ª"); // dra
            fullConsonants.Add("«"); // dva
            fullConsonants.Add("¬"); // dya
            fullConsonants.Add("\u008A"); // d.d.a
            fullConsonants.Add("°"); // d.d.a
            fullConsonants.Add("\u0160"); // d.d.a
            fullConsonants.Add("±"); // n>kha
            fullConsonants.Add("³"); // n>gha
            fullConsonants.Add("´"); // hya
            fullConsonants.Add("\u0301"); // hya
            fullConsonants.Add("µ"); // hma
            fullConsonants.Add("\u03BC"); // hma
            fullConsonants.Add("¸"); // n>ka
            fullConsonants.Add("\u0327"); // n>ka
            fullConsonants.Add("¹"); // hva
            fullConsonants.Add("º"); // n>khya
            fullConsonants.Add("½"); // n>ga
            fullConsonants.Add("¾"); // d.d.ha
            fullConsonants.Add("¿"); // following ya (VRI: BF). not in dictionary.
            fullConsonants.Add("Â"); // ddha
            fullConsonants.Add("Ë"); // dgha
            fullConsonants.Add("Ý"); // dga
            fullConsonants.Add("Þ"); // dbha
            fullConsonants.Add("ö"); // ks.a
            fullConsonants.Add("ü"); // shra (palatal sh)
            fullConsonants.Add("ø"); // hna
            fullConsonants.Add("ó"); // hra
            fullConsonants.Add("õ"); // hla
            fullConsonants.Add("ô"); // jña
            fullConsonants.Add("Õ"); // sha (palatal)
            fullConsonants.Add("ú"); // sha (retroflex)
            
            fullConsonantsCC = SetToCharacterClass(fullConsonants);

            halfConsonants = new HashSet<string>();
            halfConsonants.Add("E"); // 1/2 ka
            halfConsonants.Add("F"); // 1/2 pa
            halfConsonants.Add("\u0086"); // 1/2 ya
            halfConsonants.Add("\u008D"); // 1/2 ya
            halfConsonants.Add("\u2020"); // 1/2 ya
            halfConsonants.Add("\u0088"); // 1/2 ba
            halfConsonants.Add("\u008F"); // 1/2 ba
            halfConsonants.Add("\u02C6"); // 1/2 ba
            halfConsonants.Add("\u0089"); // 1/2 sa
            halfConsonants.Add("\u0090"); // 1/2 sa
            halfConsonants.Add("\u2030"); // 1/2 sa
            halfConsonants.Add("\u009D"); // 1/2 ma
            halfConsonants.Add("ý"); // 1/2 ma (occurs in 2 places)
            halfConsonants.Add("\u00A3"); // 1/2 la
            halfConsonants.Add("¥"); // 1/2 ta
            halfConsonants.Add("¨"); // 1/2 l.a
            halfConsonants.Add("®"); // 1/2 na
            halfConsonants.Add("¯"); // 1/2 va
            halfConsonants.Add("\u0304"); // 1/2 va
            halfConsonants.Add("²"); // 1/2 ña
            halfConsonants.Add("»"); // 1/2 n.a
            halfConsonants.Add("¼"); // 1/2 kha
            halfConsonants.Add("À"); // 1/2 ja
            halfConsonants.Add("Ä"); // 1/2 bha
            halfConsonants.Add("É"); // 1/2 tha
            halfConsonants.Add("Ê"); // 1/2 pha
            halfConsonants.Add("Í"); // 1/2 jha (not in dictionary)
            halfConsonants.Add("Î"); // 1/2 sha (palatal)
            halfConsonants.Add("Ï"); // 1/2 sha (retroflex)
            halfConsonants.Add("Ð"); // 1/2 gha

            halfConsonants.Add("\u009F"); // 1/2 ga
            halfConsonants.Add("Ÿ"); // 1/2 ga
            halfConsonants.Add("œ"); // 1/2 ca
            halfConsonants.Add("\u009C"); // 1/2 ca
            halfConsonants.Add("ç"); // 1/2 ks.a
            halfConsonants.Add("\u009B"); // 1/2 dha
            halfConsonants.Add("\u203A"); // 1/2 dha
            halfConsonants.Add("Ø"); // 1/2 tta
            halfConsonants.Add("á"); // 1/2 pra
            halfConsonants.Add("Ú"); // 1/2 nna
            halfConsonants.Add("ê"); // 1/2 sna
            halfConsonants.Add("ð"); // 1/2 pta
            halfConsonantsCC = SetToCharacterClass(halfConsonants);

            followers = new HashSet<string>();
            followers.Add("Q"); // hook ra
            followers.Add("R"); // diagonal ra
            followers.Add("Ã"); // caret ra
            followers.Add("Ñ"); // diagonal 1/2 na
            followers.Add("Ü"); // hook ra + niggahita
            followersCC = SetToCharacterClass(followers);
        }

        private static HashSet<string> indVowels;
        private static HashSet<string> depVowels;
        private static string depVowelsCC;
        private static HashSet<string> fullConsonants;
        private static string fullConsonantsCC;
        private static HashSet<string> halfConsonants;
        private static string halfConsonantsCC;
        private static HashSet<string> followers;
        private static string followersCC;

        private static Dictionary<char, string> charMap;

        public static string ConvertBook(string str)
        {
            str = Pass1(str);
            str = Pass2(str);
            str = InsertZWJ(str);

            AnalyzeConvertedFile(str);

            return str;
        }

        // vowels and niggahita to Unicode, plus some simplifications
        public static string Pass1(string str)
        {
            // our conversion from Word to text has converted the 3 fraction characters (1/4, 1/2, 3/4) to 
            // number, fraction slash, number
            str = str.Replace("1\u20444", "¼");
            str = str.Replace("1\u20442", "½");
            str = str.Replace("3\u20444", "¾");

            // Handle mid-word whitespace, added for spacing of dependent vowels and other 
            // zero-width characters. No other multi-character replacements work unless these are
            // removed first. First remove spaces that precede dependent vowels and other overstrike characters.
            str = Regex.Replace(str,
                " ([AIMOQRUÃÑÜeou\u0087\u009A\u00C3\u00D1\u00D2\u00D3\u00D4\u00D9\u00DC\u00E4])", 
                "$1");

            // Remove spaces before dependent vowels.
            str = Regex.Replace(str,
                " (" + depVowelsCC + ")",
                "$1");

            // Remove spaces that follow half consonants.
            str = Regex.Replace(str,
                "(" + halfConsonantsCC + ") ",
                "$1");

            // Remove space after "i" characters (that precede consonants)
            str = Regex.Replace(str, "([Vi]) ", "$1"); 

            // 3-to-2 replacements
            str = str.Replace("SAš", "\u0913\u0902"); // ind. a + dep. aa  + dep. e dot -> ind. o, niggahita

            // 3-to-1 replacements
            str = str.Replace("SAä", "\u0911"); // ind. a + dep. aa + chandra -> ind. chandra o
            str = str.Replace("SAe", "\u0913"); // ind. a + dep. aa  + dep. e -> ind. o
            str = str.Replace("SA\u00D4", "\u0914"); // ind. a + dep. aa + dep. ai -> ind. au

            // 2-to-1 replacements
            str = str.Replace("SA", "\u0906"); // ind. a + dep. aa -> ind. aa
            str = str.Replace("qQ", "\u0908"); // ind. i + hook r -> ind. ii
            str = str.Replace("aä", "\u090D"); // ind. e + chandra -> ind. chandra e
            str = str.Replace("ae", "\u0910"); // ind. e + dep. e -> ind. ai
            str = str.Replace("So", "\u0913"); // ind. a + dep. o -> ind. o
            str = str.Replace("SÈ", "\u0914"); // ind. a + dep. au -> ind. au
            str = str.Replace("Aä", "\u0949"); // dep. aa + chandra -> dep. chandra o
            
            str = str.Replace("ØA", "H"); // tta encoded as 1/2 tta plus aa sign
            str = str.Replace("£A", "l"); // la encoded as 1/2 la plus aa sign
            str = str.Replace("ÛA", "©"); // tra encoded as 1/2 tra plus aa sign

            // 2-to-2 replacements
            str = str.Replace("Aš", "\u094B\u0902"); // dep. aa + em. -> dep. o + m.
            str = str.Replace("A\u009A", "\u094B\u0902"); // dep. aa + em. -> dep. o + m.
			str = str.Replace("qÜ", "\u0908\u0902"); // ind. i + combined hook r and niggahita -> ind. ii + m.

            str = str.Replace("áA", "pR"); // pra is encoded as 1/2 pra plus aa sign
            str = str.Replace("ÚA", "®n"); // nna is encoded as 1/2 nna plus aa sign
            str = str.Replace("êA", "\u0090n"); // sna is encoded as 1/2 sna plus aa sign
            str = str.Replace("ðA", "Ft"); // pta is encoded as 1/2 pta plus aa sign

            // 1-to-2 replacements
            str = str.Replace("\u0099", "\u0940\u0902"); // combined dep. ii + m. -> dep. ii + m.
            str = str.Replace("\u2122", "\u0940\u0902"); // combined dep. ii + m. -> dep. ii + m.
            str = str.Replace("š", "\u0947\u0902"); // combined dep e + m. -> dep. e + m.
            str = str.Replace("\u009A", "\u0947\u0902"); // combined dep e + m. -> dep. e + m.
            str = str.Replace("Ó", "\u0948\u0902"); // combined dep. ai + m. -> dep. ai + m.

            str = str.Replace("Á", "r\u0942"); // ruu (r stays VRI encoded for now)
            str = str.Replace("ã", "h\u0943"); // h + vocalic r (h stays VRI encoded for now)
            
            // 1-to-1 replacements
            str = str.Replace("Ù", "\u0901"); // chandrabindu
            str = str.Replace("M", "\u0902"); // niggahita
            str = str.Replace("S", "\u0905"); // ind. a
            str = str.Replace("q", "\u0907"); // ind. i
            str = str.Replace("w", "\u0909"); // ind. u 
            str = str.Replace("W", "\u090A"); // ind. uu
            str = str.Replace("å", "\u090B"); // ind. vocalic r
            str = str.Replace("a", "\u090F"); // ind. e

            str = str.Replace("A", "\u093E"); // dep. aa
            str = str.Replace("I", "\u0940"); // dep. ii
            str = str.Replace("u", "\u0941"); // dep. u
            str = str.Replace("\u008E", "\u0941"); // dep. u (low form)
            str = str.Replace("\u2021", "\u0941"); // dep. u (2nd low form)
            str = str.Replace("\u0087", "\u0941"); // dep. u (2nd low form)
            str = str.Replace("\u008C", "\u0941"); // dep. u (attaches to r to make ru)
            str = str.Replace("\u0152", "\u0941"); // dep. u (attaches to r to make ru)
            str = str.Replace("U", "\u0942"); // dep. uu
            str = str.Replace("O", "\u0942"); // dep. uu (low form)
            str = str.Replace("e", "\u0947"); // dep. e
            str = str.Replace("Ô", "\u0948"); // dep. ai
            str = str.Replace("o", "\u094B"); // dep. o
            str = str.Replace("È", "\u094C"); // dep. au
            str = str.Replace("\u00ED", "\u0943"); // dep. vocalic r

            str = str.Replace("Ò", "\u094D"); // visible halant (virama)
            str = str.Replace("¦", "\u0964"); // danda
            str = str.Replace("\u2039", "\u0965"); // double danda
            str = str.Replace("Ì", "\u0970"); // abbreviation sign
            str = str.Replace("\u0091", "\u2018"); // left single quote
            str = str.Replace("\u0092", "\u2019"); // right single quote
            str = str.Replace("\u0093", "\u201C"); // left double quote
            str = str.Replace("\u0094", "\u201D"); // right double quote
            str = str.Replace("\u0096", "\u2013"); // en dash
            str = str.Replace("\u0097", "\u2014"); // em dash

            // it seems that all of the colons in the dictionary are visarga.
            str = str.Replace(":", "\u0903"); // visarga
            str = str.Replace("Å", "\u093D"); // avagraha

            // numbers
            str = str.Replace("0", "\u0966");
            str = str.Replace("1", "\u0967");
            str = str.Replace("2", "\u0968");
            str = str.Replace("3", "\u0969");
            str = str.Replace("4", "\u096A");
            str = str.Replace("5", "\u096B");
            str = str.Replace("6", "\u096C");
            str = str.Replace("7", "\u096D");
            str = str.Replace("8", "\u096E");
            str = str.Replace("9", "\u096F"); 

            // 1-to-0 replacements
            str = str.Replace("þ", ""); // seems to be used for spacing within words

            return str;
        }

        private static string Pass2(string str)
        {
            string dependentVowelsCC = "[\u093E-\u094C]";
            string shortIVowels = "[iV\u0098\u02DC]";
            string underDot = "[ì]";

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

        // insert ZWJ into some Devanagari conjuncts
        private static string InsertZWJ(string str)
        {
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

            str = str.Replace("\u0937\u094D\u0920", "\u0937\u094D\u200D\u0920"); // sha (retroflex) + t.ha

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
                s = s + "\u093F";
            }

            if (s.Contains("V")) // i with long arm
            {
                s = s.Replace("V", "");
                s = s + "\u093F";
            }

            if (s.Contains("\u0098") || s.Contains("\u02DC")) // i with short arm and niggahita
            {
                s = s.Replace("\u0098", "");
                s = s.Replace("\u02DC", "");
                s = s + "\u093F\u0902";
            }

            // if there's a "hook ra", move "ra" to the first position in the syllable
            if (s.IndexOf("Q") >= 0)
            {
                s = s.Replace("Q", "");
                s = "Q" + s;
            }

            // hook ra + niggahita
            if (s.IndexOf("Ü") >= 0)
            {
                s = s.Replace("Ü", "");
                s = "Q" + s + "\u0902";
            }

            // cases where underdot character precedes full consonant
            s = s.Replace("ìk", "\u0958");
            s = s.Replace("ìg", "\u0959");
            s = s.Replace("ìj", "\u095B");
            s = s.Replace("ìz", "\u095C");
            s = s.Replace("ìZ", "\u095D");
            // also precedes ka, ga and ja


            string s2 = "";
            foreach (char c in s.ToCharArray())
            {
                if (charMap.ContainsKey(c))
                    s2 += charMap[c];
                else if (c >= '\u0900' && c <= '\u097F')
                    s2 += c;
                else
                {
                    Console.WriteLine(c);
                }
            }

            return s2;
        }

        private static string SetToCharacterClass(HashSet<string> set)
        {
            string cc = "[";
            foreach (string s in set)
            {
                cc += s;
            }
            cc += "]";

            return cc;
        }

        private static void AnalyzeConvertedFile(string str)
        {
            HashSet<char> charset = new HashSet<char>();

            foreach (char c in str.ToCharArray())
            {
                if (c >= '\u0900' && c <= '\u097F')
                {
                }
                else
                {
                    charset.Add(c);
                }
            }

            int i = 0;
        }

        // FSnow 2020-09-07: This seems to be from some different approach to the conversion
        public static string Foo (string str)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in str.ToCharArray())
            {
                // characters that were converted to Unicode in pre-processing step get passed through
                if (c >= '\u0900' && c <= '\u097F')
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
        ¿ (\u00BF) combining ya, open on front
        × (\u00D7) r combined with ???
        Û (\u00DB) half tra
        à (\u00E0) half ha
        â (\u00E2) half stra?  
        æ (\u00E6) om
        î (\u00EE) eyelash ra
        ï (\u00EF) dep. vocalic rr
        û (\u00FB) 1/2 khr
        ÿ (\u00FF) a spacer??
        */
    }
}
