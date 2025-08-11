using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace Dev2Latin
{
    class Capitalizer
    {
        public Capitalizer(string[] paragraphElements, string capitalMarker)
        {
            this.paragraphElements = paragraphElements;
            this.capitalMarker = capitalMarker;
        }

        private string[] paragraphElements;
        private string[] ignoreElements;
        private string capitalMarker;

        // Marks Devanagari XML text for later capitalization
        public string MarkCapitals(string text)
        {
            string devLetter = "[\x0901-\x094B]";
            string notDevLetter = "[^\x0901-\x094B]";
            string devDanda = "\x0964";

            foreach (string element in paragraphElements)
            {
                text = Regex.Replace(text, "(<" + element + ">" + notDevLetter + "*)(" + devLetter + ")",
                  //  new MatchEvaluator(this.DebugRegex)); 
                "$1" + capitalMarker + "$2");
            }

            //text = Regex.Match(text, devDanda,
                //new MatchEvaluator(this.DebugRegex)); 

            /*
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(text);

            XmlElement doc = xml.DocumentElement;
            
            XPathNavigator navigator = xml.CreateNavigator();
            foreach (XPathNavigator nav in navigator.Select("//*"))
            {
                string localName = nav.LocalName;
                string val = nav.Value;
                bool hasChildren = nav.HasChildren;
                
                int i = 0;
            }
            */
       

            
            return text;
        }

        public string Capitalize(string latin)
        {
            string latinPaliLowercase = "[a-zñ\x1E45\x1E6D\x1E0D\x1E47\x1E37\x0101\x012B\x016B\x1E43]";
            latin = Regex.Replace(latin, this.capitalMarker + latinPaliLowercase, new MatchEvaluator(this.CapitalReplacer));
            latin = latin.Replace(this.capitalMarker, "");
            return latin;
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
