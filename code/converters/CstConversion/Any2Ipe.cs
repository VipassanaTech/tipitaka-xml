using System;
using System.Text;

namespace CST.Conversion
{
    public static class Any2Ipe
    {
        public static string Convert(string str)
        {
            string deva = "";
            string run = "";
            Script lastScript = Script.Latin;
            
            foreach (char c in str.ToCharArray())
            {
                Script cScript = GetScript(c);
                if (cScript == lastScript || cScript == Script.Unknown)
                    run += c;
                else
                {
                    if (run.Length > 0)
                        deva += Convert(run, lastScript);

                    run = "" + c;
                    lastScript = cScript;
                }
            }

            deva += Convert(run, lastScript);

            return deva;
        }

        private static string Convert(string str, Script script)
        {
            if (script == Script.Bengali)
                return Deva2Ipe.Convert(Beng2Deva.Convert(str));
            else if (script == Script.Devanagari)
                return Deva2Ipe.Convert(str);
            else if (script == Script.Gujarati)
                return Deva2Ipe.Convert(Gujr2Deva.Convert(str));
            else if (script == Script.Gurmukhi)
                return Deva2Ipe.Convert(Guru2Deva.Convert(str));
            else if (script == Script.Kannada)
                return Deva2Ipe.Convert(Knda2Deva.Convert(str));
            else if (script == Script.Latin)
                return Latn2Ipe.Convert(str);
            else if (script == Script.Malayalam)
                return Deva2Ipe.Convert(Mlym2Deva.Convert(str));
            else if (script == Script.Myanmar)
                return Deva2Ipe.Convert(Mymr2Deva.Convert(str));
            else if (script == Script.Sinhala)
                return Deva2Ipe.Convert(Sinh2Deva.Convert(str));
            else
                return str;
        }

        private static Script GetScript(char c)
        {
            int ccode = System.Convert.ToInt32(c);
            Script script;
            if (ccode == 0x200C || ccode == 0x200D) // ZWJ and ZWNJ
                script = Script.Unknown;
            else if (ccode >= 0x0900 && ccode <= 0x097F)
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
