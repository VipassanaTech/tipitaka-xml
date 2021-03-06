using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VRI.Conversion
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2 || args.Length > 3)
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

                string inputFilePath = args[0];
                string outputFilePath = di.FullName + "\\" + fi.Name.Replace(".txt", "") + ".unicode.txt";

                StreamReader sr = new StreamReader(inputFilePath);
                string vriStr = sr.ReadToEnd();
                sr.Close();

                string uniStr = VriDevToUni.ConvertBook(vriStr);

                StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.Unicode);
                sw.Write(uniStr);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Transliterates VRI Devanagari to Unicode Devanagari");
            Console.WriteLine("syntax:");
            Console.WriteLine("VriDevToUni input [output directory]");
        }
        // end static methods
    }
}
