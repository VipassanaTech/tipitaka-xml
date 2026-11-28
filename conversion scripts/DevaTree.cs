/*
  DevaTree

  Updates tipitaka.org/deva/tree.json, the site's navigation tree, so that it
  matches the chapter files that DevaMaster2Split writes into deva/cscd
  (VipassanaTech/tipitaka-xml#2726).

  The ids of the tree's nodes are in the site's URLs (deva/#452 opens the
  chapter whose node has id 452), so every existing node keeps its id, and the
  same id must still open the same chapter:
  - Within each book, the tree's leaves are matched to the entries of the
    book's .toc.xml by title, in order. Leaves and entries that are left over
    between two matched pairs are matched in order, and listed in the report,
    because their titles differ. A matched leaf takes the entry's file and
    title, so the tree follows the titles in deva master.
  - An entry with no leaf (a chapter the tree never had, usually one that
    repeats a title in its book) gets a new leaf, with an id above the
    largest id in the tree, placed after the leaf of the entry before it.
  - Folders are kept exactly as they are.
  A file that DevaMaster2Split merged from a heading-only file and the chapter
  after it has two entries in the .toc.xml, so both leaves are kept and open
  the same file.

  Books in excludedBooks have no node in the tree and none is made for them:
  e1216n is still being edited and should not be found from the tree yet.
  Any other book with no leaf in the tree is an error, since where its folder
  belongs is a decision for a person.

  The other scripts' trees have the same nodes, ids and files as the
  Devanagari tree; only the titles differ. They are made from the Devanagari
  tree: "labels" writes its titles as the paragraphs of an XML file, which
  each script's deva2*.exe converts like a book, and "apply" puts the
  converted titles into a copy of the Devanagari tree. For romn the titles are
  then title-cased, as the Latin tree always has been, except that a letter
  after an apostrophe (an elision, as in juhotyaa'dayo) stays lower case.
  tree-build.ps1 at the root of the repository runs all of this.

  syntax:
    DevaTree tree.json split_directory output.json
    DevaTree labels tree.json labels.xml
    DevaTree apply tree.json converted_labels.xml script output.json

  split_directory holds the .toc.xml files DevaMaster2Split wrote. Trees are
  written in the same form as tree.json: UTF-16 little-endian with a byte
  order mark and a two-space indent, with LF line ends, or CRLF if the file
  being replaced has them. The report goes to the console. The exit code is 1
  if the tree could not be made to match the files, or the converted titles
  do not match the tree; then nothing is written.

  Build: dotnet build -c Release "DevaTree.csproj", then copy
  bin\DevaTree\Release\net48\DevaTree.exe into this directory.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DevaTree
{
    class DevaTree
    {
        static int Main(string[] args)
        {
            bool labels = args.Length == 3 && args[0] == "labels";
            bool apply = args.Length == 5 && args[0] == "apply";
            if (!labels && !apply && (args.Length != 3 || args[0] == "apply"))
            {
                Console.WriteLine("Updates tipitaka.org/deva/tree.json to match the split chapter files,");
                Console.WriteLine("and makes the other scripts' trees from it");
                Console.WriteLine("syntax:");
                Console.WriteLine("  DevaTree tree.json split_directory output.json");
                Console.WriteLine("  DevaTree labels tree.json labels.xml");
                Console.WriteLine("  DevaTree apply tree.json converted_labels.xml script output.json");
                return 1;
            }
            // the report has Devanagari titles
            Console.OutputEncoding = new UTF8Encoding(false);
            try
            {
                if (labels)
                    return Labels.Write(args[1], args[2]);
                if (apply)
                    return Labels.Apply(args[1], args[2], args[3], args[4]);
                object tree = Json.Parse(ReadText(args[0]));
                Dictionary<string, List<TocEntry>> tocs = ReadTocs(args[1]);
                Updater updater = new Updater(tree, tocs);
                List<string> errors = updater.Update();
                updater.PrintReport();
                if (errors.Count > 0)
                {
                    foreach (string e in errors)
                        Console.WriteLine("ERROR: " + e);
                    Console.WriteLine("Nothing was written.");
                    return 1;
                }
                WriteTree(args[2], tree);
                Console.WriteLine("Wrote " + args[2]);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                return 1;
            }
        }

        // UTF-16 little-endian with a byte order mark, with the line ends of the file it replaces
        public static void WriteTree(string path, object tree)
        {
            string text = Json.Write(tree);
            if (File.Exists(path) && ReadText(path).Contains("\r\n"))
                text = text.Replace("\n", "\r\n");
            File.WriteAllText(path, text, new UnicodeEncoding(false, true));
        }

        // tree.json is UTF-16 with a byte order mark; the BOM decides, with UTF-8 if there is none.
        public static string ReadText(string path)
        {
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8, true))
                return sr.ReadToEnd();
        }

        private static readonly Regex tocRegex = new Regex("<tree text=\"([^\"]*)\" action=\"([^\"]*)\"");

        // book ("vin01m.mul") => its entries in order
        private static Dictionary<string, List<TocEntry>> ReadTocs(string dir)
        {
            Dictionary<string, List<TocEntry>> tocs = new Dictionary<string, List<TocEntry>>();
            string[] files = Directory.GetFiles(dir, "*.toc.xml");
            if (files.Length == 0)
                throw new Exception("no .toc.xml files in " + dir);
            foreach (string path in files)
            {
                string name = Path.GetFileName(path);
                string book = name.Substring(0, name.Length - ".toc.xml".Length);
                List<TocEntry> entries = new List<TocEntry>();
                foreach (Match m in tocRegex.Matches(ReadText(path)))
                {
                    TocEntry e = new TocEntry();
                    e.title = Unescape(m.Groups[1].Value);
                    e.href = m.Groups[2].Value;
                    entries.Add(e);
                }
                tocs[book] = entries;
            }
            return tocs;
        }

        private static string Unescape(string s)
        {
            return s.Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
        }
    }

    // The titles of a tree as an XML book for the converters, and back.
    static class Labels
    {
        private static readonly Regex paraRegex = new Regex("<p rend=\"bodytext\">(.*?)</p>", RegexOptions.Singleline);

        public static int Write(string treePath, string outputPath)
        {
            object tree = Json.Parse(DevaTree.ReadText(treePath));
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n");
            sb.Append("<?xml-stylesheet type=\"text/xsl\" href=\"tipitaka-deva.xsl\"?>\r\n");
            sb.Append("<TEI.2>\r\n<teiHeader></teiHeader>\r\n<text>\r\n<front></front>\r\n<body>\r\n");
            int n = 0;
            foreach (JsonObject node in Updater.AllNodes(tree))
            {
                sb.Append("<p rend=\"bodytext\">").Append(Escape(node.GetString("text"))).Append("</p>\r\n\r\n");
                n++;
            }
            sb.Append("</body>\r\n<back></back>\r\n</text>\r\n</TEI.2>\r\n");
            File.WriteAllText(outputPath, sb.ToString(), new UnicodeEncoding(false, true));
            Console.WriteLine("Wrote the " + n + " titles of " + treePath + " to " + outputPath);
            return 0;
        }

        public static int Apply(string treePath, string convertedPath, string script, string outputPath)
        {
            object tree = Json.Parse(DevaTree.ReadText(treePath));
            List<JsonObject> nodes = Updater.AllNodes(tree).ToList();
            MatchCollection titles = paraRegex.Matches(DevaTree.ReadText(convertedPath));
            if (titles.Count != nodes.Count)
            {
                Console.WriteLine("ERROR: " + convertedPath + " has " + titles.Count + " titles, but " + treePath +
                    " has " + nodes.Count + " nodes. Nothing was written.");
                return 1;
            }
            int changed = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                string title = Unescape(titles[i].Groups[1].Value);
                if (script == "romn")
                    title = TitleCase(title);
                if (title.Length == 0 || Regex.IsMatch(title, "[\u0900-\u0963\u0966-\u096F\u0971-\u097F]"))
                {
                    Console.WriteLine("ERROR: node " + i + " was not converted to " + script + ": " + title + ". Nothing was written.");
                    return 1;
                }
                if (title != nodes[i].GetString("text"))
                    changed++;
                nodes[i].Set("text", title);
            }
            DevaTree.WriteTree(outputPath, tree);
            Console.WriteLine("Wrote " + outputPath + " (" + script + ", " + nodes.Count + " titles)");
            return 0;
        }

        // Every word capitalized, as the romn tree's titles have always been, but not a
        // letter after an apostrophe, which marks an elision inside a word.
        public static string TitleCase(string s)
        {
            s = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
            return Regex.Replace(s, "([\u2019'])(\\p{Lu})", m => m.Groups[1].Value + m.Groups[2].Value.ToLowerInvariant());
        }

        private static string Escape(string s)
        {
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string Unescape(string s)
        {
            return s.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Replace("&amp;", "&");
        }
    }

    class TocEntry
    {
        public string title;
        public string href;
    }

    // A leaf of the tree, with the list that holds it.
    class Leaf
    {
        public JsonObject node;
        public List<object> parent;
        public string book;
    }

    class Updater
    {
        // Books that are deliberately not in the tree.
        private static readonly HashSet<string> excludedBooks = new HashSet<string> { "e1216n.nrf" };

        private object tree;
        private Dictionary<string, List<TocEntry>> tocs;
        private List<Leaf> leaves = new List<Leaf>();
        private List<string> errors = new List<string>();

        private List<string> hrefChanges = new List<string>();
        private List<string> titleChanges = new List<string>();
        private List<string> added = new List<string>();
        private List<string> skipped = new List<string>();
        private int oldIdCount;

        private static readonly Regex bookRegex = new Regex("^cscd/(.+?)\\d*\\.xml$");

        public Updater(object tree, Dictionary<string, List<TocEntry>> tocs)
        {
            this.tree = tree;
            this.tocs = tocs;
        }

        public List<string> Update()
        {
            CollectLeaves(tree, null);
            HashSet<long> oldIds = new HashSet<long>(AllIds());
            oldIdCount = oldIds.Count;
            long nextId = oldIds.Count > 0 ? oldIds.Max() + 1 : 1;

            Dictionary<string, List<Leaf>> byBook = new Dictionary<string, List<Leaf>>();
            foreach (Leaf leaf in leaves)
            {
                Match m = bookRegex.Match(Href(leaf.node));
                if (!m.Success)
                {
                    errors.Add("leaf " + Id(leaf.node) + " links to " + Href(leaf.node) + ", which is not a chapter file");
                    continue;
                }
                leaf.book = m.Groups[1].Value;
                if (!byBook.ContainsKey(leaf.book))
                    byBook[leaf.book] = new List<Leaf>();
                byBook[leaf.book].Add(leaf);
            }

            foreach (string book in byBook.Keys)
                if (!tocs.ContainsKey(book))
                    errors.Add("the tree has leaves for " + book + ", but there is no " + book + ".toc.xml");

            List<KeyValuePair<Leaf, TocEntry>> newLeaves = new List<KeyValuePair<Leaf, TocEntry>>();
            foreach (string book in tocs.Keys.OrderBy(b => b, StringComparer.Ordinal))
            {
                if (excludedBooks.Contains(book))
                {
                    if (byBook.ContainsKey(book))
                        errors.Add(book + " is excluded from the tree but has leaves in it");
                    skipped.Add(book);
                    continue;
                }
                if (!byBook.ContainsKey(book))
                {
                    errors.Add(book + " has no leaf in the tree; add its folder by hand first");
                    continue;
                }
                MatchBook(book, byBook[book], tocs[book], newLeaves);
            }
            if (errors.Count > 0)
                return errors;

            // new ids in tree order, above every existing id
            HashSet<JsonObject> isNew = new HashSet<JsonObject>(newLeaves.Select(p => p.Key.node));
            foreach (JsonObject node in AllNodes(tree))
            {
                if (!isNew.Contains(node))
                    continue;
                node.Set("id", nextId);
                added.Add(nextId + "  " + Href(node) + "  " + node.GetString("text"));
                nextId++;
            }

            Check(oldIds);
            return errors;
        }

        // Pairs a book's leaves with its toc entries, updates the leaves, and adds leaves
        // for the entries that have none.
        private void MatchBook(string book, List<Leaf> old, List<TocEntry> toc, List<KeyValuePair<Leaf, TocEntry>> newLeaves)
        {
            int[] match = Align(old.Select(l => Norm(l.node.GetString("text"))).ToList(), toc.Select(e => Norm(e.title)).ToList());
            // match[j] is the leaf index for toc entry j, or -1. Pair what is left between
            // matched pairs in order.
            int lastOld = -1;
            for (int j = 0; j < toc.Count; j++)
            {
                if (match[j] >= 0)
                {
                    lastOld = match[j];
                    continue;
                }
                int nextMatched = old.Count;
                for (int k = j + 1; k < toc.Count; k++)
                    if (match[k] >= 0) { nextMatched = match[k]; break; }
                int candidate = lastOld + 1;
                bool used = match.Contains(candidate);
                if (candidate < nextMatched && !used)
                {
                    match[j] = candidate;
                    lastOld = candidate;
                }
            }

            HashSet<int> usedOld = new HashSet<int>(match.Where(x => x >= 0));
            for (int i = 0; i < old.Count; i++)
                if (!usedOld.Contains(i))
                    errors.Add(book + ": leaf " + Id(old[i].node) + " (" + old[i].node.GetString("text") + ", " +
                        Href(old[i].node) + ") matches no chapter; decide by hand where it should point");

            Leaf previous = null;
            for (int j = 0; j < toc.Count; j++)
            {
                TocEntry e = toc[j];
                if (match[j] >= 0)
                {
                    Leaf leaf = old[match[j]];
                    string oldHref = Href(leaf.node);
                    string oldTitle = leaf.node.GetString("text");
                    if (oldHref != e.href)
                    {
                        ((JsonObject)leaf.node.Get("a_attr")).Set("href", e.href);
                        hrefChanges.Add(Id(leaf.node) + "  " + oldHref + " -> " + e.href + "  " + e.title);
                    }
                    if (oldTitle != e.title)
                    {
                        leaf.node.Set("text", e.title);
                        titleChanges.Add(Id(leaf.node) + "  " + e.href + "  " + oldTitle + " -> " + e.title);
                    }
                    previous = leaf;
                    continue;
                }

                JsonObject node = new JsonObject();
                node.Set("text", e.title);
                node.Set("id", 0L);
                node.Set("type", "leaf");
                JsonObject attr = new JsonObject();
                attr.Set("href", e.href);
                node.Set("a_attr", attr);
                Leaf added = new Leaf { node = node, book = book };
                if (previous != null)
                {
                    added.parent = previous.parent;
                    added.parent.Insert(added.parent.IndexOf(previous.node) + 1, node);
                }
                else
                {
                    // the book's first chapters: before the first matched leaf
                    Leaf first = null;
                    for (int k = j + 1; k < toc.Count && first == null; k++)
                        if (match[k] >= 0) first = old[match[k]];
                    added.parent = first.parent;
                    added.parent.Insert(added.parent.IndexOf(first.node), node);
                }
                newLeaves.Add(new KeyValuePair<Leaf, TocEntry>(added, e));
                previous = added;
            }
        }

        // Longest common subsequence of the two title lists: result[j] = index in a, or -1.
        private static int[] Align(List<string> a, List<string> b)
        {
            int[,] len = new int[a.Count + 1, b.Count + 1];
            for (int i = a.Count - 1; i >= 0; i--)
                for (int j = b.Count - 1; j >= 0; j--)
                    len[i, j] = a[i] == b[j] ? len[i + 1, j + 1] + 1 : Math.Max(len[i + 1, j], len[i, j + 1]);
            int[] result = Enumerable.Repeat(-1, b.Count).ToArray();
            int x = 0, y = 0;
            while (x < a.Count && y < b.Count)
            {
                if (a[x] == b[y]) { result[y] = x; x++; y++; }
                else if (len[x + 1, y] >= len[x, y + 1]) x++;
                else y++;
            }
            return result;
        }

        private static string Norm(string s)
        {
            return Regex.Replace(s.Replace("\u200D", "").Replace("\u200C", ""), "\\s+", " ").Trim();
        }

        private void Check(HashSet<long> oldIds)
        {
            HashSet<long> ids = new HashSet<long>();
            foreach (JsonObject node in AllNodes(tree))
            {
                if (!node.Has("id"))
                    continue;
                long id = (long)node.Get("id");
                if (!ids.Add(id))
                    errors.Add("id " + id + " is used twice");
            }
            foreach (long id in oldIds)
                if (!ids.Contains(id))
                    errors.Add("id " + id + " is gone");

            HashSet<string> files = new HashSet<string>();
            foreach (KeyValuePair<string, List<TocEntry>> t in tocs)
                if (!excludedBooks.Contains(t.Key))
                    foreach (TocEntry e in t.Value)
                        files.Add(e.href);
            HashSet<string> linked = new HashSet<string>();
            foreach (JsonObject node in AllNodes(tree))
                if (node.Has("a_attr"))
                    linked.Add(Href(node));
            foreach (string f in files)
                if (!linked.Contains(f))
                    errors.Add(f + " is not linked from the tree");
            foreach (string f in linked)
                if (!files.Contains(f))
                    errors.Add("the tree links to " + f + ", which is not a chapter file");
        }

        public void PrintReport()
        {
            Console.WriteLine("Leaves in the tree before: " + leaves.Count + "; ids before: " + oldIdCount);
            PrintList("Leaves that now open a different file (their chapter moved)", hrefChanges);
            PrintList("Leaves whose title changed to the title in deva master", titleChanges);
            PrintList("New leaves", added);
            PrintList("Books left out of the tree on purpose", skipped);
        }

        private static void PrintList(string heading, List<string> items)
        {
            Console.WriteLine();
            Console.WriteLine(heading + ": " + items.Count);
            foreach (string s in items)
                Console.WriteLine("  " + s);
        }

        private void CollectLeaves(object value, List<object> parent)
        {
            List<object> list = value as List<object>;
            if (list != null)
            {
                foreach (object item in list)
                    CollectLeaves(item, list);
                return;
            }
            JsonObject node = value as JsonObject;
            if (node == null)
                return;
            if (node.Has("a_attr"))
                leaves.Add(new Leaf { node = node, parent = parent });
            if (node.Has("children"))
                CollectLeaves(node.Get("children"), null);
        }

        private IEnumerable<long> AllIds()
        {
            foreach (JsonObject node in AllNodes(tree))
                if (node.Has("id"))
                    yield return (long)node.Get("id");
        }

        // every node, in tree order
        public static IEnumerable<JsonObject> AllNodes(object value)
        {
            List<object> list = value as List<object>;
            if (list != null)
            {
                foreach (object item in list)
                    foreach (JsonObject n in AllNodes(item))
                        yield return n;
                yield break;
            }
            JsonObject node = value as JsonObject;
            if (node == null)
                yield break;
            yield return node;
            if (node.Has("children"))
                foreach (JsonObject n in AllNodes(node.Get("children")))
                    yield return n;
        }

        private static string Href(JsonObject node)
        {
            JsonObject attr = node.Get("a_attr") as JsonObject;
            return attr == null ? null : attr.GetString("href");
        }

        private static string Id(JsonObject node)
        {
            return node.Has("id") ? node.Get("id").ToString() : "?";
        }
    }

    // A JSON object that keeps its keys in order, so the tree is written back as it was read.
    class JsonObject
    {
        private List<KeyValuePair<string, object>> items = new List<KeyValuePair<string, object>>();

        public bool Has(string key) { return items.Any(p => p.Key == key); }
        public object Get(string key) { return items.FirstOrDefault(p => p.Key == key).Value; }
        public string GetString(string key) { return Get(key) as string; }
        public IEnumerable<KeyValuePair<string, object>> Items { get { return items; } }

        public void Set(string key, object value)
        {
            int i = items.FindIndex(p => p.Key == key);
            if (i >= 0)
                items[i] = new KeyValuePair<string, object>(key, value);
            else
                items.Add(new KeyValuePair<string, object>(key, value));
        }
    }

    // Just enough JSON for tree.json: objects, arrays, strings, integers, true, false, null.
    static class Json
    {
        public static object Parse(string s)
        {
            int pos = 0;
            if (s.Length > 0 && s[0] == '\uFEFF')
                pos = 1;
            object value = ParseValue(s, ref pos);
            SkipSpace(s, ref pos);
            if (pos != s.Length)
                throw new Exception("tree.json: unexpected text at character " + pos);
            return value;
        }

        private static void SkipSpace(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos]))
                pos++;
        }

        private static object ParseValue(string s, ref int pos)
        {
            SkipSpace(s, ref pos);
            if (pos >= s.Length)
                throw new Exception("tree.json: unexpected end");
            char c = s[pos];
            if (c == '{')
            {
                JsonObject obj = new JsonObject();
                pos++;
                SkipSpace(s, ref pos);
                if (s[pos] == '}') { pos++; return obj; }
                while (true)
                {
                    SkipSpace(s, ref pos);
                    string key = ParseString(s, ref pos);
                    SkipSpace(s, ref pos);
                    Expect(s, ref pos, ':');
                    obj.Set(key, ParseValue(s, ref pos));
                    SkipSpace(s, ref pos);
                    if (s[pos] == ',') { pos++; continue; }
                    Expect(s, ref pos, '}');
                    return obj;
                }
            }
            if (c == '[')
            {
                List<object> list = new List<object>();
                pos++;
                SkipSpace(s, ref pos);
                if (s[pos] == ']') { pos++; return list; }
                while (true)
                {
                    list.Add(ParseValue(s, ref pos));
                    SkipSpace(s, ref pos);
                    if (s[pos] == ',') { pos++; continue; }
                    Expect(s, ref pos, ']');
                    return list;
                }
            }
            if (c == '"')
                return ParseString(s, ref pos);
            if (s.Substring(pos).StartsWith("true")) { pos += 4; return true; }
            if (s.Substring(pos).StartsWith("false")) { pos += 5; return false; }
            if (s.Substring(pos).StartsWith("null")) { pos += 4; return null; }
            int start = pos;
            if (s[pos] == '-')
                pos++;
            while (pos < s.Length && char.IsDigit(s[pos]))
                pos++;
            if (pos == start || (pos < s.Length && (s[pos] == '.' || s[pos] == 'e' || s[pos] == 'E')))
                throw new Exception("tree.json: only integers are expected, at character " + start);
            return long.Parse(s.Substring(start, pos - start), CultureInfo.InvariantCulture);
        }

        private static void Expect(string s, ref int pos, char c)
        {
            if (pos >= s.Length || s[pos] != c)
                throw new Exception("tree.json: expected '" + c + "' at character " + pos);
            pos++;
        }

        private static string ParseString(string s, ref int pos)
        {
            Expect(s, ref pos, '"');
            StringBuilder sb = new StringBuilder();
            while (s[pos] != '"')
            {
                char c = s[pos++];
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                char e = s[pos++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        sb.Append((char)int.Parse(s.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        pos += 4;
                        break;
                    default: throw new Exception("tree.json: bad escape at character " + pos);
                }
            }
            pos++;
            return sb.ToString();
        }

        // Two-space indent, as tree.json is written; text other than quotes, backslashes
        // and control characters is written as it is.
        public static string Write(object value)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, 0);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object value, int indent)
        {
            if (value == null) { sb.Append("null"); return; }
            if (value is bool) { sb.Append((bool)value ? "true" : "false"); return; }
            if (value is long) { sb.Append(((long)value).ToString(CultureInfo.InvariantCulture)); return; }
            string str = value as string;
            if (str != null) { WriteString(sb, str); return; }
            List<object> list = value as List<object>;
            if (list != null)
            {
                if (list.Count == 0) { sb.Append("[]"); return; }
                sb.Append("[\n");
                for (int i = 0; i < list.Count; i++)
                {
                    sb.Append(' ', indent + 2);
                    WriteValue(sb, list[i], indent + 2);
                    sb.Append(i < list.Count - 1 ? ",\n" : "\n");
                }
                sb.Append(' ', indent).Append(']');
                return;
            }
            JsonObject obj = (JsonObject)value;
            List<KeyValuePair<string, object>> items = obj.Items.ToList();
            if (items.Count == 0) { sb.Append("{}"); return; }
            sb.Append("{\n");
            for (int i = 0; i < items.Count; i++)
            {
                sb.Append(' ', indent + 2);
                WriteString(sb, items[i].Key);
                sb.Append(": ");
                WriteValue(sb, items[i].Value, indent + 2);
                sb.Append(i < items.Count - 1 ? ",\n" : "\n");
            }
            sb.Append(' ', indent).Append('}');
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            sb.Append('"');
        }
    }
}
