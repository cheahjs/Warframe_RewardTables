using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Warframe_RewardTables
{
    class Program
    {
        private const double CommonTotal = 75.87;
        private const double UncommonTotal = 22.12;
        private const double RareTotal = 2.01;
        private static int TotalTables;
        private static int BadTables;

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Syntax: <file> [dump/extract]");
                return;
            }
            if (args.Length == 2 && args[1] == "extract")
            {
                ParseExtract(args[0]);
            }
            else
            {
                ParseDump(args[0]);
            }
            Console.WriteLine("Total tables parsed: {0}\r\nBad tables: {1}", TotalTables, BadTables);
            Console.ReadKey();
        }

        static void ParseExtract(string filename)
        {
            Console.WriteLine("Starting parsing extracted file.");
            var file = File.ReadAllLines(filename);
            for (int i = 0; i < file.Length; i++)
            {
                file[i] = file[i].Replace("\0", "").Trim();
            }
            var list = new List<string>();
            var write = "";
            var start = false;
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].Trim().StartsWith("Tier") && file[i].Trim().EndsWith("{"))
                {
                    list.Clear();
                    start = true;
                    list.Add(file[i]);
                    continue;
                }
                if (start)
                {
                    if ((!file[i].StartsWith("StoreItem") && !file[i].StartsWith("Rarity") && !file[i].StartsWith("UpgradeLevel") && !file[i].StartsWith("{") && !file[i].StartsWith("},"))
                        || (file[i].Trim() == "}" && file[i+1].Trim() == "}"))
                    {
                        write += ParseExt(list.ToArray()) + "\n , , , \n";
                        start = false;
                        continue;
                    }
                    list.Add(file[i]);
                }
            }
            Console.WriteLine("Writing to file.");
            File.WriteAllText(filename + ".csv", write);
            Console.WriteLine("Done.");
        }

        static void ParseDump(string filename)
        {
            Console.WriteLine("Starting parsing dump file.");
            var file = File.ReadAllLines(filename);
            var list = new List<string>();
            var write = "";
            var start = false;
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].Trim().StartsWith("Tier") && file[i].Trim().EndsWith("{"))
                {
                    if (list.Count != 0)
                    {
                        write += Parse(list.ToArray()) + "\n , , , \n";
                        list.Clear();
                    }
                    start = true;
                    list.Add(file[i]);
                    continue;
                }
                if (start)
                {
                    if (!file[i].StartsWith("StoreItem") && !file[i].StartsWith("Rarity") && !file[i].StartsWith("UpgradeLevel"))
                    {
                        write += Parse(list.ToArray()) + "\n , , , \n";
                        list.Clear();
                        start = false;
                        continue;
                    }
                    list.Add(file[i]);
                }
            }
            Console.WriteLine("Writing to file.");
            File.WriteAllText(filename + ".csv", write);
            Console.WriteLine("Done.");
        }

        static string ParseExt(string[] file)
        {
            var tier = int.Parse(file[0].Substring(4, 1)) + 1;
            Console.WriteLine("New table found, parsing. Tier {0}", tier);
            var list = new List<Item>();
            for (int i = 2; i < file.Length; i = i + 5)
            {
                var nameparts = file[i].Split('/');
                var name = nameparts[nameparts.Length - 1];
                name = name.Substring(0, name.Length - 9);
                var rarity = file[i + 1].Split('=')[1];
                list.Add(new Item { Rarity = rarity, StoreName = name });
            }
            var rare = 0;
            var common = 0;
            var uncommon = 0;
            foreach (var item in list)
            {
                switch (item.Rarity.ToLower())
                {
                    case "rare":
                        rare++;
                        break;
                    case "common":
                        common++;
                        break;
                    case "uncommon":
                        uncommon++;
                        break;
                }
            }
            var rarechance = RareTotal / rare / 100;
            var commonchance = CommonTotal / common / 100;
            var uncommonchance = UncommonTotal / uncommon / 100;
            double interval = 0;
            foreach (var item in list)
            {
                switch (item.Rarity.ToLower())
                {
                    case "rare":
                        item.Chance = rarechance;
                        break;
                    case "common":
                        item.Chance = commonchance;
                        break;
                    case "uncommon":
                        item.Chance = uncommonchance;
                        break;
                }
                interval += item.Chance;
                item.Interval = interval;
            }
            TotalTables++;
            var matherrors = Math.Abs(1 - interval) > 0.1;
            if (matherrors) BadTables++;
            var write = "Tier " + tier + (matherrors ? " (Chance calculation flawed)" : "") + ",Chance,Interval,Rarity";
            write = list.Aggregate(write, (current, item) => current + ("\n" + item.Name + "," + item.Chance + "," + item.Interval + "," + item.Rarity));
            return write;
        }

        static string Parse(string[] file)
        {
            if (file.Length == 0) return "";
            var tier = int.Parse(file[0].Substring(4, 1)) + 1;
            Console.WriteLine("New table found, parsing. Tier {0}", tier);
            var list = new List<Item>();
            for (int i = 1; i < file.Length; i = i + 3)
            {
                var nameparts = file[i].Split('/');
                var name = nameparts[nameparts.Length - 1];
                name = name.Substring(0, name.Length - 9);
                var rarity = file[i + 1].Split('=')[1];
                list.Add(new Item { Rarity = rarity, StoreName = name });
            }
            var rare = 0;
            var common = 0;
            var uncommon = 0;
            foreach (var item in list)
            {
                switch (item.Rarity.ToLower())
                {
                    case "rare":
                        rare++;
                        break;
                    case "common":
                        common++;
                        break;
                    case "uncommon":
                        uncommon++;
                        break;
                }
            }
            var rarechance = RareTotal / rare / 100;
            var commonchance = CommonTotal / common / 100;
            var uncommonchance = UncommonTotal / uncommon / 100;
            double interval = 0;
            foreach (var item in list)
            {
                switch (item.Rarity.ToLower())
                {
                    case "rare":
                        item.Chance = rarechance;
                        break;
                    case "common":
                        item.Chance = commonchance;
                        break;
                    case "uncommon":
                        item.Chance = uncommonchance;
                        break;
                }
                interval += item.Chance;
                item.Interval = interval;
            }
            TotalTables++;
            var matherrors = Math.Abs(1 - interval) > 0.1;
            if (matherrors) BadTables++;
            var write = "Tier " + tier + (matherrors ? " (Chance calculation flawed)" : "") + ",Chance,Interval,Rarity";
            write = list.Aggregate(write, (current, item) => current + ("\n" + item.Name + "," + item.Chance + "," + item.Interval + "," + item.Rarity));
            return write;
        }
    }

    public class Item
    {
        public string StoreName;

        public string Name
        {
            get
            {
                return GetEnglishString(StoreName);
            }
        }

        public string Rarity;
        public double Chance;
        public double Interval;

        public static string GetEnglishString(string storename)
        {
            var name = storename;
            switch (storename)
            {
                case "OrokinKeyA":
                    name = "OrokinT1Exterminate";
                    break;
                case "OrokinKeyB":
                    name = "OrokinT1Survival";
                    break;
                case "OrokinKeyC":
                    name = "OrokinT2Exterminate";
                    break;
                case "OrokinKeyD":
                    name = "OrokinT2Survival";
                    break;
                case "OrokinKeyE":
                    name = "OrokinT3Exterminate";
                    break;
            }
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
            return r.Replace(name, " ");
        }
    }
}
