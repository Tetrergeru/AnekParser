using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AnekParser
{
    static class Ext
    {
        public static void Print(this IEnumerable<Anek> aneks, string fname = "")
        {
            var text = string.Join("\n---\n", aneks.Select(a => a.ToString()));
            if (fname == "")
                Console.WriteLine(text);
            else
                File.WriteAllText(fname, text);
        }
        
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> func)
            => enumerable.Select(x =>
            {
                func(x);
                return 0;
            }).ToList();

        public static List<T> Load<T>(this string fname)
            => JsonSerializer.Deserialize<List<T>>(File.ReadAllText(fname));

        public static List<Anek> LoadAneks(this string fname)
            => fname.Load<Anek>();
        
        public static List<AnekWithLemma> LoadAneksWithLemmas(this string fname)
            => fname.Load<AnekWithLemma>();

        public static IEnumerable<Anek> HasTag(this IEnumerable<Anek> aneks, params string[] tags)
            => aneks.Where(a => tags.Any(t => a.Tags.Contains(t)));
        
        public static IEnumerable<Anek> NoTag(this IEnumerable<Anek> aneks, string tag)
            => aneks.Where(a => !a.Tags.Contains(tag));
        
        public static IEnumerable<Anek> HasText(this IEnumerable<Anek> aneks, string text)
            => aneks.Where(a => a.Text.ToLower().Contains(text.ToLower()));
        
        public static IEnumerable<Anek> HasText(this IEnumerable<Anek> aneks, Regex regex)
            => aneks.Where(a => regex.IsMatch(a.Text));

        public static IEnumerable<Anek> NoText(this IEnumerable<Anek> aneks, string text)
            => aneks.Where(a => !a.Text.ToLower().Contains(text.ToLower()));
        
        public static IEnumerable<Anek> NoText(this IEnumerable<Anek> aneks, Regex regex)
            => aneks.Where(a => !regex.IsMatch(a.Text));

        public static IEnumerable<Anek> Best(this IEnumerable<Anek> aneks, int number)
            => aneks.OrderByDescending(a => a.Like).Take(number);
    }

    class Anek
    {
        public string Text { get; set; }
        public int Like { get; set; }
        public List<string> Tags { get; set; }

        public Anek()
        {
        }

        public Anek(string text, List<string> tags, int like)
        {
            Text = text;
            Tags = tags;
            Like = like;
        }

        public override string ToString()
        {
            return $"{Text}\n{Like}[{string.Join(", ", Tags)}]\n";
        }
    }

    class AnekWithLemma : Anek
    {
        public List<string> Lemmas { get; set; } = new List<string>();

        public AnekWithLemma() : base()
        {
        }
    }

    static class Program
    {
        private static string html;

        static string GetHtml(string url)
        {
            GetResponse(url).GetAwaiter().GetResult();
            return html;
        }

        static async Task GetResponse(string uri)
        {
            var h = new HttpClientHandler() {AllowAutoRedirect = true};
            var c = new HttpClient(h);
            html = await c.GetStringAsync(uri);
        }

        static Anek ParseAnek(Match match, Regex typeRegex)
        {
            var text = match.Groups["anek"].ToString().Replace("<br>", "\n");
            var tags = match.Groups["type"].ToString()
                .Split("</a>")
                .TakeWhile(x => x != "")
                .Select(m => typeRegex.Match(m).Groups[1].ToString()).ToList();
            var like = int.Parse(match.Groups["like"].ToString());
            return new Anek(text, tags, like);
        }

        static IEnumerable<Anek> ParseAneks(string html, Regex mainRegex, Regex typeRegex)
        {
            var matches = mainRegex.Matches(html);
            return matches.Select(m => ParseAnek(m, typeRegex));
        }

        private static List<string> log = new List<string>();
        static void Log(string data, bool force = false)
        {
            log.Add(data);
            if (log.Count > 100 || force)
            {
                File.AppendAllLines("log.txt", log);
                log = new List<string>();
            }
        }

        static List<Anek> ParseAllAneks(int from = 0, int to = 4099)
        {
            var mainRegex = new Regex(File.ReadAllText("regex.txt").Replace("\n","").Replace("\t",""));
            var typeRegex = new Regex(File.ReadAllText("regex-2.txt").Replace("\n","").Replace("\t",""));

            var allAneks = new List<Anek>();
            for (int i = from; i <= to; i++)
            {
                var html = GetHtml($"https://nekdo.ru/page/{i}");
                var list = ParseAneks(html, mainRegex, typeRegex).ToList();
                allAneks.AddRange(list);
                Console.WriteLine($"{i}: {list.Count}");
                Log($"{i}: {list.Count}");
            }
            Log("end", true);
            return allAneks;
        }

        static void GenFreq(string from, string to)
        {
            var list = from
                .LoadAneks()
                .SelectMany(a => SentenceParser.ParseSentences(a.Text));

            var freq = FrequencyAnalysis.GetMostFrequentNextWords(list);

            File.WriteAllText(to, JsonSerializer.Serialize(freq));
        }

        static void GenMarkov(string from, string to)
        {
            var markov = new NodeChain();

            int x = 0;
            
            from
                .LoadAneksWithLemmas()
                .ForEach(a =>
                {
                    Console.WriteLine(x++);
                    for (var i = 0; i < a.Like / 10 + 1; i++)
                        markov.ParseSentence(SentenceParser.ParseText(a.Text));
                });
            
            File.WriteAllText(to, JsonSerializer.Serialize(markov));
        }

        private static Random _generator = new Random();

        private static string GenSentence(Dictionary<string,string> freq, string start, int length)
        {
            var sentence = TextGenerator.ContinuePhrase(freq, start, length);
            return $"{sentence[0].ToString().ToUpper()}{sentence.Substring(1)}. ";
        }

        private static void GenAnek(string fname)
        {
            var freq = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fname));
            var keys = freq.Keys.ToList();
            for (var i = 0; i < 5; i++)
            {
                var start = keys[_generator.Next(0, keys.Count)];
                var sentence = GenSentence(freq, start, _generator.Next(0, 30));
                Console.Write(sentence);
            }
        }

        static void Main(string[] args)
        {
            var r = new Random();
            var markov2 = JsonConvert.DeserializeObject<NodeChainOptimized>(File.ReadAllText("markov-o-nrs.json"));
            for (var i = 0;; i++)
            {
                Console.WriteLine(markov2.MakeText(r.Next(2, 6)) + "\n");
                Console.ReadKey();
            }
        }
        /*
        "jokes_with_lemmas.json"
            .LoadAneksWithLemmas()
            .Select(a => a.Text)
            .Select(x => SentenceParser.ParseText(x)
                .Select(a => $"\"{a}\""))
            .Select(p => string.Join(", ", p))
            .Take(100)
            .ForEach(Console.WriteLine);
        */
            //File.WriteAllText("all-aneks-1.json", JsonSerializer.Serialize(ParseAllAneks()));
            //GenFreq("all-aneks-1.json", "anek-word-freq-1.json");
            //GenMarkov("jokes_with_lemmas.json", "markov-nrs.json");

            //Console.WriteLine("READY");

            //var markov = JsonConvert.DeserializeObject<NodeChain>(File.ReadAllText("markov-wp.json"));

            //var markov2 = new NodeChainOptimized(markov);

            //File.WriteAllText("markov-o-nrs.json", JsonSerializer.Serialize(markov2));

            //Console.WriteLine("READY");



            /*
            File.ReadAllLines("war_peace_2.txt")
                .Select(SentenceParser.ParseSentences)
                .ForEach(t => markov.ParseText(t, new List<string>()));
            */
            //File.WriteAllText("markov-wp.json", JsonSerializer.Serialize(markov));


            //Console.WriteLine(File.ReadAllText("all-aneks-1.json").Substring(0,100));

            //var res = string.Join("\n---\n", "jokes_with_lemmas.json".LoadAneksWithLemmas().Take(10).Select(a => string.Join(" ", a.Lemmas) + "\n" + a.Text));
            //Console.WriteLine(res);

            //"all-aneks-1.json".LoadAneks().HasText("иррациональных").Print();
        //}
    }
}