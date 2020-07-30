using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AnekParser
{
    public class NodeChain
    {
        public Dictionary<string, Node> Nodes { get; set; } = new Dictionary<string, Node>();
        
        private void ParseCombination(string from1, string from2, string to)
        {
            if (!Nodes.ContainsKey($"{from1} {from2}"))
                Nodes[$"{from1} {from2}"] = new Node();
            Nodes[$"{from1} {from2}"].AddWord(to);
        }
        
        private void ParseCombination(string from, string to)
        {
            if (!Nodes.ContainsKey(from))
                Nodes[from] = new Node();
            Nodes[from].AddWord(to);
        }

        public void ParseSentence(List<string> sentence)
        {
            if (sentence.Count <= 1)
                return;
            ParseCombination("", sentence[0]);
            ParseCombination("", sentence[0], sentence[1]);
            for (var i = 0; i < sentence.Count - 2; i++)
            {
                ParseCombination(sentence[i], sentence[i + 1]);
                ParseCombination(sentence[i],sentence[i + 1], sentence[i + 2]);
            }

            ParseCombination(sentence[^2], sentence[^1]);
            ParseCombination(sentence[^1],"");
        }

        public void ParseText(List<List<string>> text, List<string> lemmas)
        {
            text.ForEach(l => l.Add("."));
            int i = 0;
            var sentence = text.SelectMany(x => x).Select(x =>
            {
                return x;
                //if (x == ".") return ".";
                //return $"{x}${lemmas[i++]}";
            }).ToList();
            ParseSentence(sentence);
        }

        private string MakeSentence(Random r)
        {
            var list = new List<string>();
            var word0 = "";
            var word1 = Nodes[word0].GetNext(r);
            while (true)
            {
                //Console.WriteLine($"{word0} {word1}");
                string t;
                t = !Nodes.ContainsKey($"{word0} {word1}") ? Nodes[word1].GetNext(r) : Nodes[$"{word0} {word1}"].GetNext(r);
                word0 = word1;//Nodes[word].GetNext(r);
                word1 = t;
                if (word1 == "")
                    break;
                list.Add(word0);
            }

            var sentence = string.Join(" ", list.Select(x => x.Split("$")[0]));
            return $"{sentence[0].ToString().ToUpper()}{sentence.Substring(1)}";
        }

        public string MakeText(int number)
        {
            var r = new Random();
            return string.Join("", Enumerable.Range(1, number).Select(x => MakeSentence(r)));
        }
    }
}