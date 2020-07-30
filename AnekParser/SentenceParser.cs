using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnekParser
{
    public static class SentenceParser
    {
        static char[] delim = { '.', '!', '?', ';', ':', '(', ')' };

        private static List<string> ParseWords(string sentence)
            => Regex.Matches(sentence, @"[\p{L}|']+").Select(m => m.Value).ToList();

        public static List<List<string>> ParseSentences(string text)
            => text.Split(delim).Select(s => ParseWords(s)).Where(l => l.Count != 0).ToList();

        public static List<string> ParseText(string text)
            => Regex.Matches(text, @"[,\.!?:;\(\)\-" + "\n" + @"]|(\w+)").Select(m => m.Value).ToList();
    }
}