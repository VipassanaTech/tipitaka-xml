using System.Text;

namespace CST.Conversion
{
    public static class Latn2Deva
    {
        static Latn2Deva()
        {
            paliChars = new HashSet<char>();
            paliChars.Add('\u1E43');  // m underdot
            paliChars.Add('a');
            paliChars.Add('\u0101');  // a macron
            paliChars.Add('i');
            paliChars.Add('\u012B');  // i macron
            paliChars.Add('u');
            paliChars.Add('\u016B');  // u macron
            paliChars.Add('e');
            paliChars.Add('o');
            paliChars.Add('k');
            paliChars.Add('g');
            paliChars.Add('\u1E45');  // n overdot
            paliChars.Add('c');
            paliChars.Add('j');
            paliChars.Add('\u00F1');  // n tilde
            paliChars.Add('\u1E6D');  // t underdot
            paliChars.Add('\u1E0D');  // d underdot
            paliChars.Add('\u1E47');  // n underdot
            paliChars.Add('t');
            paliChars.Add('d');
            paliChars.Add('n');
            paliChars.Add('p');
            paliChars.Add('b');
            paliChars.Add('m');
            paliChars.Add('y');
            paliChars.Add('r');
            paliChars.Add('l');
            paliChars.Add('\u1E37');  // l underdot
            paliChars.Add('v');
            paliChars.Add('s');
            paliChars.Add('h');

            paliVowels = new HashSet<char>();
            paliVowels.Add('a');
            paliVowels.Add('\u0101');  // a macron
            paliVowels.Add('i');
            paliVowels.Add('\u012B');  // i macron
            paliVowels.Add('u');
            paliVowels.Add('\u016B');  // u macron
            paliVowels.Add('e');
            paliVowels.Add('o');

            devInitialVowels = new Dictionary<char, char>();
            devInitialVowels['a'] = '\u0905';
            devInitialVowels['\u0101'] = '\u0906';  // a macron
            devInitialVowels['i'] = '\u0907';
            devInitialVowels['\u012B'] = '\u0908';  // i macron
            devInitialVowels['u'] = '\u0909';
            devInitialVowels['\u016B'] = '\u090A';  // u macron
            devInitialVowels['e'] = '\u090F';
            devInitialVowels['o'] = '\u0913';

            devVowels = new Dictionary<char, object>();
            devVowels['a'] = "";
            devVowels['\u0101'] = '\u093E';  // a macron
            devVowels['i'] = '\u093F';
            devVowels['\u012B'] = '\u0940';  // i macron
            devVowels['u'] = '\u0941';
            devVowels['\u016B'] = '\u0942';  // u macron
            devVowels['e'] = '\u0947';
            devVowels['o'] = '\u094B';

            devConsonants = new Dictionary<string, string>();
            devConsonants["k"] = "\u0915";
            devConsonants["kh"] = "\u0916";
            devConsonants["g"] = "\u0917";
            devConsonants["gh"] = "\u0918";
            devConsonants["\u1E45"] = "\u0919"; // n overdot
            devConsonants["c"] = "\u091A";
            devConsonants["ch"] = "\u091B";
            devConsonants["j"] = "\u091C";
            devConsonants["jh"] = "\u091D";
            devConsonants["\u00F1"] = "\u091E"; // n tilde
            devConsonants["\u1E6D"] = "\u091F"; // t underdot 
            devConsonants["\u1E6Dh"] = "\u0920"; // t underdot h
            devConsonants["\u1E0D"] = "\u0921"; // d underdot
            devConsonants["\u1E0Dh"] = "\u0922"; // d underdot h
            devConsonants["\u1E47"] = "\u0923"; // n underdot
            devConsonants["t"] = "\u0924";
            devConsonants["th"] = "\u0925";
            devConsonants["d"] = "\u0926";
            devConsonants["dh"] = "\u0927";
            devConsonants["n"] = "\u0928";
            devConsonants["p"] = "\u092A";
            devConsonants["ph"] = "\u092B";
            devConsonants["b"] = "\u092C";
            devConsonants["bh"] = "\u092D";
            devConsonants["m"] = "\u092E";
            devConsonants["y"] = "\u092F";
            devConsonants["r"] = "\u0930";
            devConsonants["l"] = "\u0932";
            devConsonants["\u1E37"] = "\u0933"; // l underdot
            devConsonants["v"] = "\u0935";
            devConsonants["s"] = "\u0938";
            devConsonants["h"] = "\u0939";

            paliAspiratables = new HashSet<char>();
            paliAspiratables.Add('k');
            paliAspiratables.Add('g');
            paliAspiratables.Add('c');
            paliAspiratables.Add('j');
            paliAspiratables.Add('\u1E6D'); // t underdot
            paliAspiratables.Add('\u1E0D'); // d underdot
            paliAspiratables.Add('t');
            paliAspiratables.Add('d');
            paliAspiratables.Add('p');
            paliAspiratables.Add('b');
        }

        private static ISet<char> paliChars;
        private static ISet<char> paliVowels;
        private static ISet<char> paliAspiratables;

        private static IDictionary<char, char> devInitialVowels;
        private static IDictionary<char, object> devVowels;
        private static IDictionary<string, string> devConsonants;

        public static string Convert(string latin)
		{
			StringBuilder book = new StringBuilder();
			StringBuilder word = new StringBuilder();

            char scriptZero = '\u0966';

			foreach (char c in latin.ToCharArray())
			{
				if (word.Length > 0 && IsPaliChar(c) == false)
				{
					book.Append(ToDevanagari(word.ToString()));
                    book.Append(c); // punctuation
					word.Length = 0;
				}
				else if (IsDigit(c))
				{
					char scriptNumber = (char)(c - '0' + scriptZero);
					book.Append(scriptNumber);
				}
				else if (IsPaliChar(c))
					word.Append(c);
				else
					book.Append(c);
			}

			if (word.Length > 0)
			{
				book.Append(ToDevanagari(word.ToString()));
				word.Length = 0;
			}

			// insert ZWJ in some Devanagari conjuncts
			book = book.Replace("\u0915\u094D\u0915", "\u0915\u094D\u200D\u0915"); // ka + ka
			book = book.Replace("\u0915\u094D\u0932", "\u0915\u094D\u200D\u0932"); // ka + la
			book = book.Replace("\u0915\u094D\u0935", "\u0915\u094D\u200D\u0935"); // ka + va
			book = book.Replace("\u091A\u094D\u091A", "\u091A\u094D\u200D\u091A"); // ca + ca
			book = book.Replace("\u091C\u094D\u091C", "\u091C\u094D\u200D\u091C"); // ja + ja
			book = book.Replace("\u091E\u094D\u091A", "\u091E\u094D\u200D\u091A"); // n(tilde)a + ca
			book = book.Replace("\u091E\u094D\u091C", "\u091E\u094D\u200D\u091C"); // n(tilde)a + ja
            book = book.Replace("\u091E\u094D\u091E", "\u091E\u094D\u200D\u091E"); // n(tilde)a + �a
            book = book.Replace("\u0928\u094D\u0928", "\u0928\u094D\u200D\u0928"); // na + na
			book = book.Replace("\u092A\u094D\u0932", "\u092A\u094D\u200D\u0932"); // pa + la
			book = book.Replace("\u0932\u094D\u0932", "\u0932\u094D\u200D\u0932"); // la + la

			return book.ToString();
		}

        public static bool IsDigit(char c)
		{
			if (c >= '0' && c <= '9')
				return true;
			else
				return false;
		}

        public static bool IsPaliChar(char c)
		{
			return paliChars.Contains(c);
		}

        public static bool IsPaliVowel(char c)
		{
			return paliVowels.Contains(c);
		}

		// Is a Latin-script letter that when followed by 'h' is a single Pali
		// aspirated stop, e.g. t -> th
        public static bool IsPaliAspiratable(char c)
		{
			return paliAspiratables.Contains(c);
		}

        public static string GetDevConsonants(string consonants)
		{
			if (devConsonants[consonants] == null)
				return "";
			else
				return (string)devConsonants[consonants];
		}

        public static string ToDevanagari(string latin)
		{
			string dev = "";
			LetterType last = LetterType.Vowel;
			
			for (int i = 0; i < latin.Length; i++)
			{
				char c = latin[i];
				char c2 = '\u0000';
				if (i < latin.Length - 1)
					c2 = latin[i + 1];

				if (IsPaliVowel(c))
				{
					if (last == LetterType.Vowel)
					{
						dev = String.Concat(dev, devInitialVowels[c]);
					}
					else
					{
						dev = String.Concat(dev, devVowels[c]);
					}

					last = LetterType.Vowel;
				}
				else if (c.Equals('\u1E43'))  // m underdot
				{
					last = LetterType.Nasal;
					dev = String.Concat(dev, '\u0902');  // anusvara
				}
				else
				{
					if (last == LetterType.Consonant)
						dev = String.Concat(dev, '\u094D'); // halant after last consonant

					if (IsPaliAspiratable(c) && c2.Equals('h'))
					{
						dev = String.Concat(dev, devConsonants[String.Concat(c, c2)]);
						i++;
					}
					else
						dev = String.Concat(dev, devConsonants[String.Concat(c)]);

					last = LetterType.Consonant;
				}
			}

            if (devConsonants.ContainsKey(latin.Substring(latin.Length - 1)))
                dev += '\u094D';

			return dev;
		}
	}

	public enum LetterType
	{
		Vowel,
		Consonant,
		Nasal
	}


}
