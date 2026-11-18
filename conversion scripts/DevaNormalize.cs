using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Cst;

/*
  DevaNormalize

  Normalizes the text of the Devanagari XML books in "deva master" after they
  have been corrected: en dash for --, double danda, ellipsis for ..., the
  abbreviation sign after a letter, and ZWJ in eleven conjuncts (the rules are
  in DevaNormalization.cs). Correctors do not need to type any of these.

  Only the text of the paragraphs in <body> is changed, never the markup. The
  rest of each file is kept byte for byte, including its encoding, byte order
  mark and line ends, so a book that is already normalized is not rewritten.

  syntax:
    DevaNormalize [-check] input

  input is a book file or a directory of them; *.toc.xml files are skipped.
  With -check, no file is changed: the books that need normalizing are listed
  and the exit code is 1 if there are any. Without -check, the exit code is 1
  only if a book could not be read; it is left unchanged.

  Build: dotnet build -c Release "DevaNormalize.csproj", then copy
  bin\DevaNormalize\Release\net48\DevaNormalize.exe into this directory.
*/

namespace DevaNormalize
{
    class DevaNormalize
    {
        static int Main(string[] args)
        {
            bool check = false;
            string input = null;
            foreach (string arg in args)
            {
                if (arg.Equals("-check", StringComparison.OrdinalIgnoreCase))
                    check = true;
                else if (input == null)
                    input = arg;
                else
                {
                    PrintUsage();
                    return 1;
                }
            }
            if (input == null)
            {
                PrintUsage();
                return 1;
            }

            List<string> books = new List<string>();
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
            }
            else if (File.Exists(input))
            {
                books.Add(input);
            }
            else
            {
                Console.Error.WriteLine("Input file or directory does not exist: " + input);
                return 1;
            }

            int failures = 0;
            int needed = 0;
            foreach (string book in books)
            {
                try
                {
                    int changed = NormalizeBook(book, check);
                    if (changed > 0)
                    {
                        needed++;
                        Console.WriteLine(Path.GetFileName(book) + ": " +
                            (check ? "needs normalizing in " : "normalized ") + changed +
                            (changed == 1 ? " paragraph" : " paragraphs"));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(Path.GetFileName(book) + ": " + ex.Message + " (not changed)");
                    failures++;
                }
            }

            Console.WriteLine((check ? "Books that need normalizing: " : "Books normalized: ") + needed + " of " + books.Count);
            if (failures > 0)
            {
                Console.Error.WriteLine(failures + " of " + books.Count + " books could not be read");
                return 1;
            }
            return (check && needed > 0 ? 1 : 0);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Normalizes the text of Devanagari master XML books");
            Console.WriteLine("syntax:");
            Console.WriteLine("  DevaNormalize [-check] input");
            Console.WriteLine("input is a book XML file or a directory of them (*.toc.xml are skipped)");
            Console.WriteLine("-check lists the books that need normalizing without changing them");
        }

        // Returns the number of paragraphs that need normalizing, and unless check
        // is set, rewrites the file with them normalized.
        private static int NormalizeBook(string path, bool check)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Encoding encoding;
            int bomLength = DetectEncoding(bytes, out encoding);
            string xml = encoding.GetString(bytes, bomLength, bytes.Length - bomLength);

            // Refuse a book that is not well-formed rather than guess at its paragraphs.
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.LoadXml(xml);

            int bodyStart = xml.IndexOf("<body");
            int bodyEnd = xml.LastIndexOf("</body>");
            if (bodyStart < 0 || bodyEnd < bodyStart)
                throw new Exception("no <body> element");
            bodyStart = xml.IndexOf('>', bodyStart) + 1;

            int changed;
            string body = DevaNormalization.NormalizeParagraphs(
                xml.Substring(bodyStart, bodyEnd - bodyStart), out changed);
            if (changed == 0 || check)
                return changed;

            string result = xml.Substring(0, bodyStart) + body + xml.Substring(bodyEnd);
            byte[] encoded = encoding.GetBytes(result);
            byte[] output = new byte[bomLength + encoded.Length];
            Array.Copy(bytes, output, bomLength);
            Array.Copy(encoded, 0, output, bomLength, encoded.Length);

            // write a temporary file first, so that a failure cannot leave a half-written book
            string temp = path + ".tmp";
            File.WriteAllBytes(temp, output);
            File.Copy(temp, path, true);
            File.Delete(temp);
            return changed;
        }

        // Returns the length of the byte order mark. A book with no byte order mark is
        // read as UTF-8, whatever its XML declaration says.
        private static int DetectEncoding(byte[] b, out Encoding encoding)
        {
            if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE)
            {
                encoding = new UnicodeEncoding(false, false, true);
                return 2;
            }
            if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF)
            {
                encoding = new UnicodeEncoding(true, false, true);
                return 2;
            }
            encoding = new UTF8Encoding(false, true);
            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
                return 3;
            return 0;
        }
    }
}
