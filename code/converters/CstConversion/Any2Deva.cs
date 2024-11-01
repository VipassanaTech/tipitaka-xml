using System;
using System.Text;

namespace CST.Conversion
{
    public static class Any2Deva
    {
        public static string Convert(string str)
        {
            string deva = "";
            string run = "";
            Script lastScript = Script.Latin;
            
            foreach (char c in str.ToCharArray())
            {
                Script cScript = GetScript(c);
                if (cScript == lastScript)
                    run += c;
                else
                {
                    deva += Convert(run, lastScript);
                    run = "" + c;
                    lastScript = cScript;
                }
            }

            deva += Convert(run, lastScript);

            return deva;
        }

        public static string Convert(string str, Script script)
        {
            if (script == Script.Bengali)
                return Beng2Deva.Convert(str);
            else if (script == Script.Gujarati)
                return Gujr2Deva.Convert(str);
            else if (script == Script.Gurmukhi)
                return Guru2Deva.Convert(str);
            else if (script == Script.Kannada)
                return Knda2Deva.Convert(str);
            else if (script == Script.Latin)
                return Latn2Deva.Convert(str);
            else if (script == Script.Malayalam)
                return Mlym2Deva.Convert(str);
            else if (script == Script.Myanmar)
                return Mymr2Deva.Convert(str);
            else if (script == Script.Sinhala)
                return Sinh2Deva.Convert(str);
            else
                return str;
        }

        // TODO: add Cyrillic, Tamil(?)
        public static Script GetScript(char c)
        {
            int ccode = System.Convert.ToInt32(c);
            Script script;
            if (ccode >= 0x0900 && ccode <= 0x097F)
                script = Script.Devanagari;
            else if (ccode >= 0x0980 && ccode <= 0x09FF)
                script = Script.Bengali;
            else if (ccode >= 0x0A00 && ccode <= 0x0A7F)
                script = Script.Gurmukhi;
            else if (ccode >= 0x0A80 && ccode <= 0x0AFF)
                script = Script.Gujarati;
            else if (ccode >= 0x0C00 && ccode <= 0x0C7F)
                script = Script.Telugu;
            else if (ccode >= 0x0C80 && ccode <= 0x0CFF)
                script = Script.Kannada;
            else if (ccode >= 0x0D00 && ccode <= 0x0D7F)
                script = Script.Malayalam;
            else if (ccode >= 0x0D80 && ccode <= 0x0DFF)
                script = Script.Sinhala;
            else if (ccode >= 0x0E00 && ccode <= 0x0E7F)
                script = Script.Thai;
            else if (ccode >= 0x0F00 && ccode <= 0x0FFF)
                script = Script.Tibetan;
            else if (ccode >= 0x1000 && ccode <= 0x107F)
                script = Script.Myanmar;
            else if (ccode >= 0x1780 && ccode <= 0x17FF)
                script = Script.Khmer;
            else
                script = Script.Latin;

            return script;
        }
    }
}
