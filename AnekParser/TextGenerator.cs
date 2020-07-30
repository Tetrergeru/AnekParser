using System.Collections.Generic;

namespace AnekParser
{
    public static class TextGenerator
    {
        public static string ContinuePhrase(
            Dictionary<string, string> nextWords,
            string phraseBeginning,
            int wordsCount)
        {
            var list = new List<string>(phraseBeginning.Split(' '));

            wordsCount += list.Count - 1;
            for (int i = list.Count; i <= wordsCount; i++)
                if (i > 1 && nextWords.ContainsKey(list[i - 2] + " " + list[i - 1]))
                    list.Add(nextWords[list[i - 2] + " " + list[i - 1]]);
                else if (nextWords.ContainsKey(list[i - 1]))
                    list.Add(nextWords[list[i - 1]]);
                else break;

            return string.Join(" ", list);
        }
    }
}