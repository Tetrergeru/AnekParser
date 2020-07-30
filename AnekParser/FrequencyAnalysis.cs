using System.Collections.Generic;
using System.Linq;

namespace AnekParser
{
    public static class FrequencyAnalysis
    {
        private static void UpdateDictionary(Dictionary<string, int> dict, string key)
        {
            if (dict.ContainsKey(key))
                dict[key]++;
            else
                dict[key] = 1;
        }

        private static string GetKey(string s)
        {
            var split = s.Split(' ');
            if (split.Length == 2)
                return split[0];
            return split[0] + " " + split[1];
        }

        private static KeyValuePair<string, int> MaxByValueMinByKey(IEnumerable<KeyValuePair<string, int>> seq)
        {
            var min = new KeyValuePair<string, int>("", int.MinValue);
            
            foreach (var x in seq)
                if (x.Value > min.Value)
                    min = x;
                else if (x.Value == min.Value && string.CompareOrdinal(x.Key, min.Key) < 0)
                    min = x;
            
            return min;
        }
        
        public static Dictionary<string, string> GetMostFrequentNextWords(IEnumerable<List<string>> text)
        {
            var counter = new Dictionary<string, int>();
            foreach (var s in text)
            {
                for (int i = 0; i < s.Count() - 2; i++)
                {
                    UpdateDictionary(counter, s[i]+ " " + s[i + 1]);
                    UpdateDictionary(counter, s[i] + " " + s[i + 1] + " " + s[i + 2]);
                }
                if (s.Count() > 1)
                    UpdateDictionary(counter, s[s.Count() - 2] + " " + s[s.Count() - 1]);
            }
            var sortedWords = counter.GroupBy(k => GetKey(k.Key), k => k).Select(MaxByValueMinByKey);
            return sortedWords.ToDictionary(k => GetKey(k.Key), k => k.Key.Split(' ').Last());
        }
    }
}