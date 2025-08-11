using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace Deva2Cyrl
{
    class Capitalizer
    {
        public Capitalizer(string[] paragraphElements, string[] ignoreElements, string capitalMarker)
        {
            this.paragraphElements = paragraphElements;
            paragraphElementsHash = new Hashtable();
            foreach (string str in paragraphElements)
            {
                paragraphElementsHash.Add(str, 1);
            }

            this.ignoreElements = ignoreElements;
            ignoreElementsHash = new Hashtable();
            foreach (string str in ignoreElements)
            {
                ignoreElementsHash.Add(str, 1);
            }

            this.capitalMarker = capitalMarker;
        }

        private string[] paragraphElements;
        private Hashtable paragraphElementsHash;
        private string[] ignoreElements;
        private Hashtable ignoreElementsHash;
        private string capitalMarker;

        // Marks Devanagari XML text for later capitalization
        public string MarkCapitals(string text)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(text);

            XmlNode node = xml.DocumentElement;

            bool nextIsCap = false;
            bool descend = true;

            while (true)
            {
                if (node.LocalName != null && node.LocalName.Length > 0 && 
                    paragraphElementsHash.ContainsKey(node.LocalName))
                    nextIsCap = true;
                else if (node.LocalName != null && node.LocalName.Length > 0 && 
                    ignoreElementsHash.ContainsKey(node.LocalName))
                    descend = false;
                else if (node.NodeType == XmlNodeType.Text)
                {
                    string str = node.Value;
                    string val = "";
                    foreach (char c in str.ToCharArray())
                    {
                        int ccode = Convert.ToInt32(c);
                        // if c is a Devanagari letter
                        if (nextIsCap && ccode >= 0x0901 && ccode <= 0x094B && nextIsCap)
                        {
                            // mark it for capitalization
                            val += this.capitalMarker;
                            nextIsCap = false;
                        }
                        else if (c == '\x0964' || c == '?' || c == '!')
                        {
                            nextIsCap = true;
                        }
                        val += c;
                    }

                    if (str.Equals(val) == false)
                        node.Value = val;
                }

                bool nodeFound = false;

                if (node.HasChildNodes && descend)
                {
                    node = node.FirstChild;
                    nodeFound = true;
                }
                else if (node.NextSibling != null)
                {
                    node = node.NextSibling;
                    nodeFound = true;
                }
                else
                {
                    while (node.ParentNode != null)
                    {
                        node = node.ParentNode;
                        if (node.NextSibling != null)
                        {
                            node = node.NextSibling;
                            nodeFound = true;
                            break;
                        }
                    }
                }
                if (nodeFound == false)
                    break;

                descend = true;
            }

            // OuterXml is a string without any hard returns for formatting.
            // The following formats the XML like Pitaka2Xml.
            string xmlStr = xml.OuterXml;
            xmlStr = xmlStr.Replace("<?xml-stylesheet", "\r\n<?xml-stylesheet");
            xmlStr = xmlStr.Replace("<text>", "\r\n<text>");
            xmlStr = xmlStr.Replace("</text>", "\r\n</text>");

            foreach (string paragraphElement in paragraphElements)
            {
                string openTag = "<" + paragraphElement + ">";
                xmlStr = xmlStr.Replace(openTag, "\r\n\r\n" + openTag);
            }

            return xmlStr;
        }

        public string Capitalize(string cyrl)
        {
            string cyrlPaliLowercase = "[\x0430-\x045F]";
            cyrl = Regex.Replace(cyrl, this.capitalMarker + cyrlPaliLowercase, new MatchEvaluator(this.CapitalReplacer));
            cyrl = cyrl.Replace(this.capitalMarker, "");
            return cyrl;
        }

        public string CapitalReplacer(Match m)
        {
            return m.Value.Substring(1).ToUpper();
        }

        public string DebugRegex(Match m)
        {
            string foo = m.Value;
            return foo;
        }
    }
}
