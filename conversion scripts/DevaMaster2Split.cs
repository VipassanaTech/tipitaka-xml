using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Cst;

/*
  DevaMaster2Split

  Splits the Devanagari master XML books in "deva master" into the per-chapter
  files that tipitaka.org serves from deva/cscd, plus a .toc.xml for each book.
  It replaces "pitaka2xml.exe -split", which split the old text files in
  "root text files". Those text files are no longer corrected, so the site has
  to be built from the XML (VipassanaTech/tipitaka-xml#2679).

  The logic follows Pitaka2Xml.cs. Pitaka2Xml worked from the paragraph format
  codes in the text files (%11 and so on); this tool works from the rend
  attribute that those codes became in the XML. It reads only the sequence of
  paragraphs and their rend values. <div> markup is ignored, because it is
  complete in some books, partial in others and absent in most, and the output
  is flat <p rend="..."> paragraphs as before:
    <p rend="X">       stays as it is
    <head rend="X">    becomes <p rend="X">
    <trailer rend="X"> becomes <p rend="X">

  A new file starts at each rend="chapter" paragraph. Unlike Pitaka2Xml, the
  start of the file moves back over the headings and the "Namo tassa" line
  directly above the chapter heading, so that a book's opening lines are at
  the top of its first file and not at the bottom of the previous one. This
  was the error that had to be fixed by hand at every rollout
  (VipassanaTech/tipitaka-xml#2093).

  It also applies Pitaka2Xml's text normalization (en dash, double danda,
  ellipsis, abbreviation sign, ZWJ in some conjuncts; see DevaNormalization.cs)
  to the text of each paragraph, and reports each book where that changed
  anything. DevaNormalize.exe should already have been run on the master, but
  the site is built correctly even where it has not.

  syntax:
    DevaMaster2Split input [output_directory]

  input is a book file or a directory of them; *.toc.xml files are skipped.
  The output directory is created if it does not exist. The exit code is 1 if
  any book could not be split. The other books are still written.

  Build: dotnet build -c Release "DevaMaster2Split.csproj", then copy
  bin\DevaMaster2Split\Release\net48\DevaMaster2Split.exe into this directory.
*/

namespace DevaMaster2Split
{
    class DevaMaster2Split
    {
        static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                PrintUsage();
                return 1;
            }

            string input = args[0];
            List<string> books = new List<string>();
            string defaultOutputDir;
            if (Directory.Exists(input))
            {
                foreach (string path in Directory.GetFiles(input, "*.xml"))
                {
                    // GetFiles also matches extensions that only start with "xml" on .NET Framework
                    if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                        !path.EndsWith(".toc.xml", StringComparison.OrdinalIgnoreCase))
                        books.Add(path);
                }
                books.Sort(StringComparer.Ordinal);
                defaultOutputDir = input;
            }
            else if (File.Exists(input))
            {
                books.Add(input);
                defaultOutputDir = Path.GetDirectoryName(Path.GetFullPath(input));
            }
            else
            {
                Console.Error.WriteLine("Input file or directory does not exist: " + input);
                return 1;
            }

            string outputDir = (args.Length > 1 ? args[1] : defaultOutputDir);
            Directory.CreateDirectory(outputDir);

            int failures = 0;
            foreach (string book in books)
            {
                try
                {
                    DevaMaster2Split splitter = new DevaMaster2Split();
                    splitter.Split(book, outputDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(Path.GetFileName(book) + ": " + ex.Message);
                    failures++;
                }
            }

            if (failures > 0)
            {
                Console.Error.WriteLine(failures + " of " + books.Count + " books could not be split");
                return 1;
            }
            return 0;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Splits Devanagari master XML books into chapter files");
            Console.WriteLine("syntax:");
            Console.WriteLine("  DevaMaster2Split input [output_directory]");
            Console.WriteLine("input is a book XML file or a directory of them (*.toc.xml are skipped)");
        }

        // Paragraph styles that can be the title of a file in the navigation tree.
        // These are the styles of Pitaka2Xml's formats 10, 11, 12 and 15.
        private static readonly HashSet<string> titleRends =
            new HashSet<string> { "book", "chapter", "nikaya", "subsubhead" };

        // Heading styles that stay with the chapter heading below them.
        private static readonly HashSet<string> leadInRends =
            new HashSet<string> { "nikaya", "book", "title", "subhead", "subsubhead" };

        // "namo tass": matches "namo tassa bhagavato..." in all its spellings
        private const string Namo = "\u0928\u092E\u094B \u0924\u0938\u094D\u0938";

        private static readonly Regex paraRegex = DevaNormalization.ParagraphRegex;
        private static readonly Regex attrRegex = new Regex("([\\w:.-]+)\\s*=\\s*\"([^\"]*)\"");
        private static readonly Regex divRegex = new Regex("</?div(\\s[^>]*)?>");

        private List<Paragraph> paragraphs = new List<Paragraph>();
        private string fileStem;

        public void Split(string inputPath, string outputDir)
        {
            fileStem = Path.GetFileName(inputPath);
            if (fileStem.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                fileStem = fileStem.Substring(0, fileStem.Length - 4);

            ReadParagraphs(inputPath);

            int titleIndex = FindTitleIndex();
            if (titleIndex == -1)
                throw new Exception("no book, chapter, nikaya or subsubhead paragraph in the first 25 paragraphs to use as the title");

            List<XmlFragment> xmlFragments = new List<XmlFragment>();
            XmlFragment frag = new XmlFragment();
            frag.title = paragraphs[titleIndex].title;
            frag.startIndex = 0;

            // the previous chapter heading, which must stay in the previous file
            int lastChapter = titleIndex;
            for (int i = titleIndex + 1; i < paragraphs.Count; i++)
            {
                if (paragraphs[i].rend != "chapter")
                    continue;

                int start = i;
                while (start - 1 > lastChapter && IsLeadIn(paragraphs[start - 1]))
                    start--;

                frag.endIndex = start - 1;
                xmlFragments.Add(frag);

                frag = new XmlFragment();
                frag.startIndex = start;
                frag.title = paragraphs[i].title;
                lastChapter = i;
            }
            frag.endIndex = paragraphs.Count - 1;
            xmlFragments.Add(frag);

            string outputPathStem = Path.Combine(outputDir, fileStem);
            WriteTocFile(outputPathStem, xmlFragments);
            for (int i = 0; i < xmlFragments.Count; i++)
                WriteXmlFile(outputPathStem, (xmlFragments.Count > 1 ? i : -1), xmlFragments[i]);
        }

        // A heading, or the "Namo tassa" line, that belongs with the chapter heading below it.
        private static bool IsLeadIn(Paragraph para)
        {
            if (leadInRends.Contains(para.rend))
                return true;
            return para.rend == "centre" && para.text.Contains(Namo);
        }

        // Pitaka2Xml's title search: look backwards through the first 25 paragraphs for
        // chapter, subsubhead, nikaya and book paragraphs, in that order of preference.
        private int FindTitleIndex()
        {
            int titleLookAhead = (paragraphs.Count < 25 ? paragraphs.Count : 25);
            int titleIndex = -1;
            for (int i = titleLookAhead - 1; i >= 0; i--)
            {
                string rend = paragraphs[i].rend;
                if (rend == "chapter")
                    titleIndex = i;
                else if (titleIndex == -1 && (rend == "subsubhead" || rend == "nikaya" || rend == "book"))
                    titleIndex = i;
            }
            return titleIndex;
        }

        private void ReadParagraphs(string inputPath)
        {
            // Detect the encoding from the byte order mark, not the XML declaration:
            // the books are UTF-16, but at least one is saved as UTF-8 while it
            // still declares UTF-16.
            string xml;
            using (StreamReader sr = new StreamReader(inputPath, Encoding.UTF8, true))
                xml = sr.ReadToEnd();

            // Parse the whole book first, so that a book that is not well-formed is
            // reported instead of being split wrongly.
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.LoadXml(xml);
            XmlNode body = doc.SelectSingleNode("/TEI.2/text/body");
            if (body == null)
                throw new Exception("no /TEI.2/text/body element");
            int expected = body.SelectNodes(".//p | .//head | .//trailer").Count;
            if (body.SelectNodes(".//p//p | .//p//head | .//p//trailer | .//head//p | .//trailer//p").Count > 0)
                throw new Exception("a paragraph contains another paragraph");

            // Take the paragraphs from the text itself rather than from the parsed
            // document, so that their contents are copied exactly as they are.
            int bodyStart = xml.IndexOf("<body");
            bodyStart = xml.IndexOf('>', bodyStart) + 1;
            int bodyEnd = xml.LastIndexOf("</body>");
            string bodyXml = xml.Substring(bodyStart, bodyEnd - bodyStart);

            foreach (Match m in paraRegex.Matches(bodyXml))
            {
                Paragraph para = new Paragraph();
                foreach (Match a in attrRegex.Matches(m.Groups[2].Value))
                {
                    string name = a.Groups[1].Value;
                    if (name == "rend")
                        para.rend = a.Groups[2].Value;
                    else if (name == "n")
                        para.num = a.Groups[2].Value;
                    else
                        throw new Exception("unexpected attribute " + name + " on <" + m.Groups[1].Value + ">");
                }
                if (para.rend == null)
                    throw new Exception("<" + m.Groups[1].Value + "> with no rend: " + Excerpt(m.Value));

                para.text = DevaNormalization.Normalize(m.Groups[3].Value);
                if (para.text != m.Groups[3].Value)
                    normalizedCount++;
                if (titleRends.Contains(para.rend))
                    para.title = MakeTitle(para.text);
                paragraphs.Add(para);
            }

            if (paragraphs.Count != expected)
                throw new Exception("found " + paragraphs.Count + " paragraphs in the text but " + expected + " in the XML");

            // Anything between the paragraphs other than <div> tags would be lost.
            string rest = divRegex.Replace(paraRegex.Replace(bodyXml, ""), "").Trim();
            if (rest.Length > 0)
                throw new Exception("text outside any paragraph: " + Excerpt(rest));

            if (normalizedCount > 0)
                Console.WriteLine(Path.GetFileName(inputPath) + ": normalized the text of " + normalizedCount +
                    (normalizedCount == 1 ? " paragraph" : " paragraphs") + ". The same correction is needed in deva master.");
        }

        private int normalizedCount;


        // The title of a heading for the navigation tree: its text without notes,
        // page breaks or other markup, as Pitaka2Xml made it from the text files.
        private static string MakeTitle(string text)
        {
            string title = Regex.Replace(text, "<note>.*?</note>", "", RegexOptions.Singleline);
            title = Regex.Replace(title, "<[^>]*>", "");
            title = Regex.Replace(title, "\\s+", " ").Trim();
            return title.Replace("\"", "&quot;");
        }

        private static string Excerpt(string s)
        {
            s = Regex.Replace(s, "\\s+", " ");
            return (s.Length > 80 ? s.Substring(0, 80) + "..." : s);
        }

        private void WriteTocFile(string outputPathStem, List<XmlFragment> xmlFragments)
        {
            using (StreamWriter sw = NewWriter(outputPathStem + ".toc.xml"))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-16\"?>");
                sw.WriteLine("<tree>");
                for (int i = 0; i < xmlFragments.Count; i++)
                {
                    string fileName = fileStem + (xmlFragments.Count > 1 ? i.ToString() : "") + ".xml";
                    sw.Write("<tree text=\"" + xmlFragments[i].title + "\" action=\"cscd/");
                    sw.WriteLine(fileName + "\" target=\"text\"/>");
                }
                sw.WriteLine("</tree>");
            }
        }

        private void WriteXmlFile(string outputPathStem, int fileNum, XmlFragment xmlFragment)
        {
            string outputFilePath = outputPathStem + (fileNum == -1 ? "" : fileNum.ToString()) + ".xml";
            using (StreamWriter sw = NewWriter(outputFilePath))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-16\"?>");
                sw.WriteLine("<?xml-stylesheet type=\"text/xsl\" href=\"tipitaka-deva.xsl\"?>");
                sw.WriteLine("<TEI.2>");
                sw.WriteLine("<teiHeader></teiHeader>");
                sw.WriteLine("<text>");
                sw.WriteLine("<front></front>");
                sw.WriteLine("<body>");

                for (int i = xmlFragment.startIndex; i <= xmlFragment.endIndex; i++)
                {
                    Paragraph para = paragraphs[i];
                    sw.WriteLine("<p rend=\"" + para.rend + "\"" +
                        (para.num != null ? " n=\"" + para.num + "\"" : "") +
                        ">" + para.text + "</p>");
                    sw.WriteLine();
                }

                sw.WriteLine("</body>");
                sw.WriteLine("<back></back>");
                sw.WriteLine("</text>");
                sw.WriteLine("</TEI.2>");
            }
        }

        // UTF-16 little-endian with a byte order mark and CRLF line ends, as Pitaka2Xml wrote,
        // on any platform
        private static StreamWriter NewWriter(string path)
        {
            StreamWriter sw = new StreamWriter(path, false, Encoding.Unicode);
            sw.NewLine = "\r\n";
            return sw;
        }
    }

    public class Paragraph
    {
        public string rend;
        public string num;
        public string text;
        public string title;
    }

    public class XmlFragment
    {
        public string title;
        public int startIndex;
        public int endIndex;
    }
}
