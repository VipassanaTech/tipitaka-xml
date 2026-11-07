using System.Text.RegularExpressions;

/*
  DevaNormalization

  The text normalization of the Devanagari XML, shared by DevaNormalize.exe
  (which corrects the books in "deva master") and DevaMaster2Split.exe (which
  applies it again as it splits them for the site, in case a correction has
  not been normalized yet). The rules are the ones Pitaka2Xml applied to the
  text files.
*/

namespace Cst
{
    public static class DevaNormalization
    {
        // A <p>, <head> or <trailer> paragraph: group 1 is the element name,
        // group 2 its attributes and group 3 its contents.
        public static readonly Regex ParagraphRegex = new Regex(
            "<(p|head|trailer)(\\s[^>]*)?>(.*?)</\\1>", RegexOptions.Singleline);

        private static readonly Regex markupOrText = new Regex("(<[^>]*>)|([^<]+)");
        private static readonly Regex letterZero = new Regex("([\u0901-\u0963])\u0966");

        // Normalizes the text of a paragraph's contents. Only the text between tags is
        // changed, never the markup. Every rule is idempotent: text that is already
        // normalized does not match it.
        public static string Normalize(string xml)
        {
            return markupOrText.Replace(xml,
                m => (m.Groups[1].Success ? m.Value : NormalizeText(m.Value)));
        }

        // Normalizes every paragraph in a run of XML, such as the contents of <body>,
        // and returns the number of paragraphs that changed.
        public static string NormalizeParagraphs(string xml, out int changed)
        {
            int count = 0;
            string result = ParagraphRegex.Replace(xml, m =>
            {
                string contents = m.Groups[3].Value;
                string normalized = Normalize(contents);
                if (normalized == contents)
                    return m.Value;
                count++;
                return "<" + m.Groups[1].Value + m.Groups[2].Value + ">" + normalized + "</" + m.Groups[1].Value + ">";
            });
            changed = count;
            return result;
        }

        private static string NormalizeText(string s)
        {
            // two hyphens to an en dash
            s = s.Replace("--", "\u2013");

            // two dandas to the Devanagari double danda character
            s = s.Replace("\u0964\u0964", "\u0965");

            // three dots to an ellipsis
            s = s.Replace("...", "\u2026");

            // a zero after a Devanagari letter to the Devanagari abbreviation sign
            s = letterZero.Replace(s, "$1\u0970");

            // ZWJ after the virama in these conjuncts, so they are written with a half form
            s = s.Replace("\u0915\u094D\u0915", "\u0915\u094D\u200D\u0915"); // ka + ka
            s = s.Replace("\u0915\u094D\u0932", "\u0915\u094D\u200D\u0932"); // ka + la
            s = s.Replace("\u0915\u094D\u0935", "\u0915\u094D\u200D\u0935"); // ka + va
            s = s.Replace("\u091A\u094D\u091A", "\u091A\u094D\u200D\u091A"); // ca + ca
            s = s.Replace("\u091C\u094D\u091C", "\u091C\u094D\u200D\u091C"); // ja + ja
            s = s.Replace("\u091E\u094D\u091A", "\u091E\u094D\u200D\u091A"); // nya + ca
            s = s.Replace("\u091E\u094D\u091C", "\u091E\u094D\u200D\u091C"); // nya + ja
            s = s.Replace("\u091E\u094D\u091E", "\u091E\u094D\u200D\u091E"); // nya + nya
            s = s.Replace("\u0928\u094D\u0928", "\u0928\u094D\u200D\u0928"); // na + na
            s = s.Replace("\u092A\u094D\u0932", "\u092A\u094D\u200D\u0932"); // pa + la
            s = s.Replace("\u0932\u094D\u0932", "\u0932\u094D\u200D\u0932"); // la + la
            return s;
        }
    }
}
