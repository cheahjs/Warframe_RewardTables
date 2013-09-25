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

        private static List<string> rewardList = new List<string>();
        private static Dictionary<int, string> rewardPos = new Dictionary<int, string>();
        private static List<int> sortedReward = new List<int>();
        private static int rewardTableIndex = -1;

        private static List<string> dropTableList = new List<string>(); 

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
            var bigfile = string.Join("", file).Replace("\0", "");
            for (int i = 0; i < file.Length; i++)
            {
                file[i] = file[i].Replace("\0", "").Trim();
            }
            var list = new List<string>();
            var write = "";
            var start = false;
            Console.WriteLine("Finding reward table types...");
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].StartsWith("randomizedItems=") && !file[i].Contains("\"\""))
                {
                    var parts = file[i].Split('=');
                    var nameparts = parts[1].Split('/');
                    var name = nameparts[nameparts.Length - 1];
                    if (!rewardList.Contains(parts[1])) rewardList.Add(name);
                }
                else if (file[i].Contains("/Lotus/Types/DropTables/") && file[i].Length < 2000)
                {
                    var parts = file[i].TrimEnd(',').Split('/');
                    var name = parts[parts.Length - 1];
                    if (!dropTableList.Contains(name)) dropTableList.Add(name);
                }
            }
            Console.WriteLine("Found {0} used reward table types.", rewardList.Count);
            //Manual overrides
            rewardList.Add("NightmareModeRewards");
            rewardList.Add("OrokinMissionRewards");
            rewardList.Add("OrokinMissionRewardsB");
            rewardList.Add("OrokinMissionRewardsC");
            rewardList.Add("OrokinMobDefenseRewards");
            rewardList.Add("OrokinMobDefenseRewardsB");
            rewardList.Add("OrokinMobDefenseRewardsC");
            rewardList.Add("RaidMissionRewardsA");

            Console.WriteLine("Searching for {0} reward table types.", rewardList.Count);
            Console.WriteLine("Found {0} item drop table types.", dropTableList.Count);

            foreach (var table in rewardList)
            {
                Console.WriteLine("Searching for " + table + "A");
                var positions = GetPositions(bigfile, table + "A" + "\u0001");
                Console.WriteLine("Found {0} instances.", positions.Count);
                if (positions.Count == 1)
                    rewardPos.Add(positions[0], table);
                else if (positions.Count == 2)
                {
                    rewardPos.Add(positions[0], table + "-1");
                    rewardPos.Add(positions[1], table + "-2");
                }
                if (table.Contains("Orokin"))
                {
                    Console.WriteLine("Searching for " + table);
                    var positions2 = GetPositions(bigfile, table + "\u0001");
                    Console.WriteLine("Found {0} instances.", positions2.Count);
                    switch (positions2.Count)
                    {
                        case 1:
                            if (!rewardPos.ContainsKey(positions2[0]))
                                rewardPos.Add(positions2[0], table + "?");
                            break;
                        case 2:
                            if (!rewardPos.ContainsKey(positions2[0]))
                                rewardPos.Add(positions2[0], table + "-11");
                            if (!rewardPos.ContainsKey(positions2[1]))
                                rewardPos.Add(positions2[1], table + "-12");
                            break;
                    }
                    if (!table.EndsWith("A")) continue;
                    var table2 = table.Substring(0, table.Length - 1);
                    Console.WriteLine("Searching for " + table2);
                    positions2 = GetPositions(bigfile, table2 + "\u0001");
                    Console.WriteLine("Found {0} instances.", positions2.Count);
                    switch (positions2.Count)
                    {
                        case 1:
                            if (!rewardPos.ContainsKey(positions2[0]))
                                rewardPos.Add(positions2[0], table2 + "?");
                            break;
                        case 2:
                            if (!rewardPos.ContainsKey(positions2[0]))
                                rewardPos.Add(positions2[0], table2 + "-11");
                            if (!rewardPos.ContainsKey(positions2[1]))
                                rewardPos.Add(positions2[1], table2 + "-12");
                            break;
                    }
                }
            }
            Console.WriteLine("Found {0} available reward table types.", rewardPos.Count);
            sortedReward = rewardPos.Keys.ToList();
            sortedReward.Sort();
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
            if (file.Length == 0) return "";
            var tier = int.Parse(file[0].Substring(4, 1)) + 1;
            if (tier == 1)
                rewardTableIndex++;
            var tablename = rewardPos[sortedReward[rewardTableIndex]];
            Console.WriteLine("Found new table. Name: {0} | Reward Tier: {1}", tablename, tier);
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
            var write = "Tier " + tier + " - " + tablename
                + (matherrors ? " (Chance calculation flawed)" : "") + ",Chance,Interval,Rarity";
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
        
        static List<int> GetPositions(string source, string searchString)
        {
            List<int> ret = new List<int>();
            int len = searchString.Length;
            int start = -len;
            while (true)
            {
                start = source.IndexOf(searchString, start + len, StringComparison.InvariantCultureIgnoreCase);
                if (start == -1)
                {
                    break;
                }
                else
                {
                    ret.Add(start);
                }
            }
            return ret;
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
