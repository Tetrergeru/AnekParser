using System;
using System.Collections.Generic;
using System.Linq;

namespace AnekParser
{
    public class Node
    {
        private bool _compiled = false;
        
        public Dictionary<string, int> Count { get; set; } = new Dictionary<string, int>();

        private Dictionary<string, double> _frequencies = new Dictionary<string, double>();
        
        public void AddWord(string word)
        {
            Count[word] = Count.ContainsKey(word) ? Count[word] + 1 : 1;
        }

        public void Compile()
        {
            _compiled = true;
            _frequencies = new Dictionary<string, double>();
            var count = Count.Sum(kv => kv.Value);
            Count.ForEach(kv => _frequencies[kv.Key] = kv.Value * 1.0 / count);
        }

        public string GetNext(Random r)
        {
            if (!_compiled)
                Compile();
            var value = r.NextDouble();
            foreach (var kv in _frequencies)
            {
                value -= kv.Value;
                if (value <= 0)
                    return kv.Key;
            }

            return "";
        }
    }
}