// See https://aka.ms/new-console-template for more information
using CST.Conversion;

using System.Text;
using Newtonsoft.Json.Linq;

string filePath = "./tree.json";
string outputDir = "TreeConverted/";

FileInfo fi = new FileInfo(filePath);
if (fi.Exists == false)
{
    Console.WriteLine("Input file does not exist");
    return;
}

DirectoryInfo di = new DirectoryInfo(outputDir);
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

StreamReader sr = new StreamReader(filePath);
string jsonStr = sr.ReadToEnd();
sr.Close();

JArray jsonArray = JArray.Parse(jsonStr);

ISet<Script> scripts = new HashSet<Script>();
scripts.Add(Script.Bengali);
scripts.Add(Script.Cyrillic);
//scripts.Add(Script.Devanagari);
scripts.Add(Script.Gujarati);
scripts.Add(Script.Gurmukhi);
scripts.Add(Script.Kannada);
scripts.Add(Script.Khmer);
scripts.Add(Script.Latin);
scripts.Add(Script.Malayalam);
scripts.Add(Script.Myanmar);
scripts.Add(Script.Sinhala);
scripts.Add(Script.Telugu);
scripts.Add(Script.Thai);
scripts.Add(Script.Tibetan);

foreach (Script script in scripts)
{
    JArray jsonArrayCopy = (JArray)jsonArray.DeepClone();
    TransliterateJSON(jsonArrayCopy, script);
    string outputJson = jsonArrayCopy.ToString();

    string scriptCode = ScriptConverter.Iso15924Code(script);
    string outputFilePath = outputDir + "tree_" + scriptCode + ".json";
    StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.Unicode);
    sw.Write(outputJson);
    sw.Flush();
    sw.Close();
    Console.WriteLine(scriptCode);
}

void TransliterateJSON(JToken token, Script script)
{
    if (token.Type == JTokenType.Object)
    {
        JObject obj = (JObject)token;
        if (obj.ContainsKey("text"))
        {
            string text = (string)obj["text"];
            text = text.ToLower();
            string otherText = ScriptConverter.Convert(text, Script.Devanagari, script);
            obj["text"] = otherText;
        }

        if (obj.ContainsKey("children"))
        {
            JArray children = (JArray)obj["children"];
            TransliterateJSON(children, script);
        }
    }
    else if (token.Type == JTokenType.Array)
    {
        foreach (JToken child in token.Children())
        {
            TransliterateJSON(child, script);
        }
    }
}
