using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace TreeCapitalizer
{
    class TreeCapitalizer
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                PrintUsage();
                return;
            }

            // validate args
            FileInfo fi = new FileInfo(args[0]);
            if (fi.Exists == false)
            {
                Console.WriteLine("Input file does not exist");
                return;
            }

            DirectoryInfo di = new DirectoryInfo(args[1]);
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

            TreeCapitalizer tc = new TreeCapitalizer();
            tc.InputFilePath = args[0];
            tc.OutputFilePath = di.FullName + "\\" + fi.Name;
            tc.Convert();
        }

        static void PrintUsage()
        {
            Console.WriteLine("Capitalizes first letter of titles in Latin-script tree TOC XML");
            Console.WriteLine("syntax:");
            Console.WriteLine("treecap input [output]");
        }

        public string InputFilePath
        {
            get { return inputFilePath; }
            set { inputFilePath = value; }
        }
        private string inputFilePath;

        public string OutputFilePath
        {
            get { return outputFilePath; }
            set { outputFilePath = value; }
        }
        private string outputFilePath;

        public void Convert()
        {
            StreamReader sr = new StreamReader(InputFilePath);
            string text = sr.ReadToEnd();
            sr.Close();

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(text);

            XmlNode node = xml.DocumentElement;

            while (true)
            {
                if (node.LocalName != null && node.LocalName.Length > 0 &&
                    node.LocalName == "tree" && node.Attributes["text"] != null && 
                    node.Attributes["text"].Value.Length > 0)
                {
                    node.Attributes["text"].Value = Capitalize(node.Attributes["text"].Value);
                }

                bool nodeFound = false;

                if (node.HasChildNodes)
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
            }

            // OuterXml is a string without any hard returns for formatting.
            // The following formats the XML like Pitaka2Xml.
            string xmlStr = xml.OuterXml;
            xmlStr = xmlStr.Replace("<tree", "\r\n<tree");

            StreamWriter sw = new StreamWriter(OutputFilePath, false, Encoding.Unicode);
            sw.Write(xmlStr);
            sw.Flush();
            sw.Close();
        }

        public string Capitalize(string latin)
        {
            return latin.Substring(0, 1).ToUpper() + latin.Substring(1);
        }
    }
}
