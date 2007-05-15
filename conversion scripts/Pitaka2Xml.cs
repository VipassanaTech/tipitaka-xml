using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Pitaka2Xml
{
    class Pitaka2Xml
    {
        static void Main(string[] args)
        {
            // input file is the required arg
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            int i = 0;
            Hashtable flags = new Hashtable();
            foreach (string arg in args)
            {
                if (arg.StartsWith("-") && arg.Length > 1)
                {
                    flags[arg.ToLower().Substring(1)] = 1;
                    i++;
                }
                else
                {
                    break;
                }
            }

            // input file still required after flags
            if (i >= args.Length)
            {
                PrintUsage();
                return;
            }

            // check for existence of input file
            string file = args[i];
            FileInfo fi = new FileInfo(file);
            if (fi.Exists == false)
            {
                Console.WriteLine("Input file does not exist");
                return;
            }
            i++;

            string dir = "";
            if (i < args.Length)
                dir = args[i];
            else
                dir = fi.DirectoryName;

            DirectoryInfo di = new DirectoryInfo(dir);
            if (di.Exists == false)
            {
                Console.Write("Output directory does not exist. Would you like to create it?[y/n] ");
                if (Console.ReadLine().ToLower().Substring(0, 1) == "y")
                {
                    di.Create();
                }
                else
                {
                    return;
                }
            }

            string xmlStem = fi.Name;
            if (xmlStem.EndsWith(".txt"))
                xmlStem = xmlStem.Substring(0, xmlStem.Length - 4);

            Pitaka2Xml p2x = new Pitaka2Xml();
            p2x.InputFilePath = fi.FullName;
            p2x.outputFileStem = xmlStem;
            p2x.OutputPathStem = di.FullName + "\\" + xmlStem;
            p2x.SplitXml = (flags["split"] != null);
            p2x.Convert();
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Devanagari Unicode Tipitaka Text to XML");
            Console.WriteLine("syntax:");
            Console.WriteLine("  pitaka2xml [optional flags] input_filename [output_directory]");
            Console.WriteLine("flags:");
            Console.WriteLine("  -split split by chapter");
            Console.WriteLine();
        }

        public Pitaka2Xml()
        {
            Paragraphs = new ArrayList();

            xmlParaTags = new Hashtable();
            xmlParaTags[1] = "unk";
            xmlParaTags[2] = "indent";
            xmlParaTags[3] = "bodytext";
            xmlParaTags[4] = "paraalign";
            xmlParaTags[6] = "centre";
            xmlParaTags[10] = "book";
            xmlParaTags[11] = "chapter";
            xmlParaTags[12] = "nikaya";
            xmlParaTags[13] = "title";
            xmlParaTags[14] = "subhead";
            xmlParaTags[15] = "centrebold";
            xmlParaTags[21] = "gatha1";
            xmlParaTags[22] = "gatha2";
            xmlParaTags[25] = "nonindent";
            xmlParaTags[26] = "gathalast";
            xmlParaTags[27] = "gatha3";
            xmlParaTags[40] = "bodytext";
        }

        public string InputFilePath
        {
            get { return inputFilePath; }
            set { inputFilePath = value; }
        }
        private string inputFilePath;

        public string OutputFileStem
        {
            get { return outputFileStem; }
            set { outputFileStem = value; }
        }
        private string outputFileStem;

        public string OutputPathStem
        {
            get { return outputPathStem; }
            set { outputPathStem = value; }
        }
        private string outputPathStem;

        public ArrayList Paragraphs
        {
            get { return paragraphs; }
            set { paragraphs = value; }
        }
        private ArrayList paragraphs;

        public Hashtable XmlParaTags
        {
            get { return xmlParaTags; }
            set { xmlParaTags = value; }
        }
        private Hashtable xmlParaTags;

        public bool SplitXml
        {
            get { return splitXml; }
            set { splitXml = value; }
        }
        private bool splitXml;


        public void Convert()
        {
            StreamReader sr = new StreamReader(InputFilePath);
            string devStr = sr.ReadToEnd();
            sr.Close();

            // delete spaces between hard returns
            devStr = Regex.Replace(devStr, "\r\n *\r\n", "\r\n\r\n");

            // Must handle footnotes before we break paras on double hard returns.
            // See comments in CaptureFootnotes method
            devStr = Regex.Replace(devStr, "<([^>]+)>", new MatchEvaluator(this.CaptureFootnotes));

            // delete leading and trailing whitespace
            devStr = Regex.Replace(devStr, "\\A[\\s]+(\\S+)", "$1");
            devStr = Regex.Replace(devStr, "([\\S]+)[\\s]+\\z", "$1");

            // replace double hard returns with a placeholder in Ethiopic Unicode range
            devStr = devStr.Replace("\r\n\r\n", "\x1234\x1234");

            // put markers on each end so that the first and last paras
            // match the paragraph regex
            devStr = "\x1234" + devStr + "\x1234";

            // put the paragraphs into the Paragraphs array
            devStr = Regex.Replace(devStr, "\x1234[^\x1234]+\x1234", new MatchEvaluator(this.CapturePara));

            // done with devStr. data is in Paragraphs array
            devStr = "";

            foreach (Paragraph para in Paragraphs)
            {
                // populate titles for pars that are possible headings
                if (para.format == 10 || para.format == 11 || para.format == 12 || para.format == 15)
                {
                    para.title = para.text;

                    // remove page refs from titles. refs in heading paras occur in some NRF books.
                    para.title = Regex.Replace(para.title, "[$#@&][\x0966-\x096F]\\.[\x0966-\x096F]{4}", "");
                    // cleanup the spaces around page refs
                    para.title = DoubleSpacesToSingle(para.title);
                    // delete bold markers in titles
                    para.title = para.title.Replace("/", "");
                    // Delete footnotes from titles (there is only one, in s0503m.mul)
                    // The end footnote marker is a placeholder at this point
                    para.title = Regex.Replace(para.title, "<fnote>.*\x1232", 
                        new MatchEvaluator(this.RemoveTitleFootnotes));
                }

                // use placeholders so that we don't screw up the end tags when we look for the
                // single end-of-bold slash
                para.text = para.text.Replace("//./", "\x1233");
                para.text = para.text.Replace("//", "<bold>");
                para.text = para.text.Replace("/", "</bold>");
                para.text = para.text.Replace("\x1233", "<dot>.</dot>");

                // replace the end footnote marker that is inserted in CaptureFootnotes()
                para.text = para.text.Replace("\x1232", "</fnote>");
                
                // replace two hyphens with an en-dash
                para.text = para.text.Replace("--", "\x2013");

                para.text = Regex.Replace(para.text, "[$#@&][\x0966-\x096F]\\.[\x0966-\x096F]{4}",
                    new MatchEvaluator(this.FormatPageRefs));

                // for all paragraphs that are not headings
                if (para.format < 6 || para.format > 15)
                {
                    // Put the <paranum> tag around paragraph numbers.
                    // Looking for Devanagari digits, or a hyphen for number ranges, e.g. 26-27
                    // at the beginning of the para.(^ matches beginning of string only)
                    para.text = Regex.Replace(para.text, "^([\x0966-\x096F\\-]+)", "<paranum>$1</paranum>");
                }

                string tag = (string)xmlParaTags[para.format];
                if (tag == null)
                {
                    Console.WriteLine("Unknown format: " + para.format);
                    tag = (string)xmlParaTags[3]; // body text
                }
                para.text = "<" + tag + ">" + para.text + "</" + tag + ">";
            }

            // Look for the paragraph to use as the title of the first file in the navigation tree.
            // If not splitting, we will use the book name (format 10), otherwise:
            // look backwards for para formats 11, 15 and 10, in that order of preference
            int titleLookAhead = (Paragraphs.Count < 25 ? Paragraphs.Count : 25);
            int titleIndex = -1;
            for (int i = titleLookAhead - 1; i >= 0; i--)
            {
                Paragraph para = (Paragraph)Paragraphs[i];
                if (SplitXml)
                {
                    if (para.format == 11)
                    {
                        titleIndex = i;
                    }
                    else if (para.format == 15 && titleIndex == -1)
                    {
                        titleIndex = i;
                    }
                    // for the book names that are marked with the nikaya tag(12)
                    else if (para.format == 12 && titleIndex == -1)
                    {
                        titleIndex = i;
                    }
                    else if (para.format == 10 && titleIndex == -1)
                    {
                        titleIndex = i;
                    }
                }
                else
                {
                    if (para.format == 10)
                    {
                        titleIndex = i;
                        break;
                    }
                    // there are cases where the book name is tagged with
                    // the nikaya tag(12) or the chapter tag(11)
                    else if (para.format == 12 && titleIndex == -1)
                    {
                        titleIndex = i;
                    }
                    else if (para.format == 11 && titleIndex == -1)
                    {
                        titleIndex = i;
                    }
                }
            }

            ArrayList xmlFragments = new ArrayList();
            if (SplitXml)
            {
                XmlFragment frag = new XmlFragment();
                frag.title = ((Paragraph)Paragraphs[titleIndex]).title;
                frag.startIndex = 0;

                for (int i = titleIndex + 1; i < Paragraphs.Count; i++)
                {
                    Paragraph para = (Paragraph)Paragraphs[i];
                    if (para.format == 11)
                    {
                        frag.endIndex = i - 1;
                        xmlFragments.Add(frag);

                        frag = new XmlFragment();
                        frag.startIndex = i;
                        frag.title = para.title;
                    }
                }
                frag.endIndex = Paragraphs.Count - 1;
                xmlFragments.Add(frag);
            }
            else
            {
                XmlFragment all = new XmlFragment();
                all.title = ((Paragraph)Paragraphs[titleIndex]).title;
                all.startIndex = 0;
                all.endIndex = Paragraphs.Count - 1;
                xmlFragments.Add(all);
            }

            WriteFiles(xmlFragments);
        }

        public void WriteFiles(IList xmlFragments)
        {
            WriteTocFile(xmlFragments);

            for (int i = 0; i < xmlFragments.Count; i++)
            {
                WriteXmlFile((xmlFragments.Count > 1 ? i : -1), (XmlFragment)xmlFragments[i]);
            }
        }

        public void WriteTocFile(IList xmlFragments)
        {
            string outputFilePath = OutputPathStem;
            outputFilePath += ".toc.xml";

            StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.BigEndianUnicode);
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-16\"?>");
            sw.WriteLine("<tree>");
            int i = 0;
            foreach (XmlFragment xmlFragment in xmlFragments)
            {
                string fileName = OutputFileStem + (xmlFragments.Count > 1 ? i.ToString() : "") + ".xml";

                sw.Write("<tree text=\"" + xmlFragment.title + "\" action=\"cscd/");
                sw.WriteLine(fileName + "\" target=\"text\"/>");

                i++;
            }
            sw.WriteLine("</tree>");
            sw.Flush();
            sw.Close();
        }

        public void WriteXmlFile(int fileNum, XmlFragment xmlFragment)
        {
            string outputFilePath = OutputPathStem;
            if (fileNum == -1)
                outputFilePath += ".xml";
            else
                outputFilePath += fileNum + ".xml";

            StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.BigEndianUnicode);

            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-16\"?>");
            sw.WriteLine("<?xml-stylesheet type=\"text/xsl\" href=\"tipitaka-deva.xsl\"?>");
            sw.WriteLine("<text>");
            for (int i = xmlFragment.startIndex; i <= xmlFragment.endIndex; i++)
            {
                sw.WriteLine(((Paragraph)Paragraphs[i]).text);
                sw.WriteLine();
            }
            sw.WriteLine("</text>");

            sw.Flush();
            sw.Close();
        }

        public string CapturePara(Match m)
        {
            string s = m.Value;

            Paragraph para = new Paragraph();

            // replace all hard returns within the para with a space
            s = s.Replace("\r\n", " ");
            s = s.Replace("\r", " ");
            s = s.Replace("\n", " ");

            s = DoubleSpacesToSingle(s);

            // delete the double hard return placeholder
            s = s.Replace("\x1234", "");

            para.format = GetParaFormat(s);

            // delete para format marker
            s = Regex.Replace(s, "%[\x0966-\x096F]{2}", "");

            // change pipe to dollar sign. some of the Myanmar page refs are wrongly marked with pipe.
            s = s.Replace('|', '$');

            // delete leading and trailing whitespace
            s = Regex.Replace(s, "\\A[\\s]+(\\S+)", "$1");
            s = Regex.Replace(s, "([\\S]+)[\\s]+\\z", "$1");

            para.text = s;
            Paragraphs.Add(para);
            return "";
        }

        public string CaptureFootnotes(Match m)
        {
            string fnote = m.Value;
            // chop off the start(<) and end(>) markers
            fnote = fnote.Substring(1, fnote.Length - 2);

            // There are a small number of footnotes that span multiple paragraphs.
            // We must handle them before we parse out the rest of the paragraphs.
            // Para breaks within the footnotes will be marked with the section sign. (§)
            fnote = fnote.Replace("\r\n\r\n", "\x00A7");

            fnote = fnote.Replace("\r\n", " ");
            fnote = fnote.Replace("\r", " ");
            fnote = fnote.Replace("\n", " ");

            // Delete whitespace around the section sign.
            fnote = Regex.Replace(fnote, "[\\s]+\x00A7", "\x00A7");
            fnote = Regex.Replace(fnote, "\x00A7[\\s]+", "\x00A7");

            // can't use the end tag yet (</fnote>). The bold slashes haven't been replaced yet.
            return "<fnote>" + fnote + "\x1232";
        }

        public string RemoveTitleFootnotes(Match m)
        {
            return "";
        }

        public int GetParaFormat(string s)
        {
            Match m = Regex.Match(s, "%[\x0966-\x096F]{2}");
            // paras without a para format marker are format 3
            int format = 3;
            if (m.Success)
            {
                string tag = m.Value;
                format = devDigitValue(tag[1]) * 10 + devDigitValue(tag[2]);
            }
            return format;
        }

        public string FormatPageRefs(Match m)
        {
            string s = m.Value;
            string p = "";
            switch (s[0])
            {
                case '$':
                    p = "M";
                    break;
                case '#':
                    p = "V";
                    break;
                case '@':
                    p = "P";
                    break;
                case '&':
                    p = "T";
                    break;
            }

            return "<link p=\"" + p + "\" target=\"" + devDigitValue(s[1]) + "." + devDigitValue(s[3]) +
                devDigitValue(s[4]) + devDigitValue(s[5]) + devDigitValue(s[6]) + "\"/>";
        }

        private int devDigitValue(char d)
        {
            return System.Convert.ToInt32(d) - 2406;
        }

        private int parseDevNumber(string dn)
        {
            return 0;
        }

        // replace all double spaces with a single space
        private string DoubleSpacesToSingle(string s)
        {
            int before = s.Length;
            while (true)
            {
                s = s.Replace("  ", " ");
                if (before == s.Length)
                    break;
                else
                    before = s.Length;
            }

            return s;
        }
    }

    public class Paragraph
    {
        public string title;
        public string text;
        public int format;
    }

    public class XmlFragment
    {
        public string title;
        public int startIndex;
        public int endIndex;
    }
}
