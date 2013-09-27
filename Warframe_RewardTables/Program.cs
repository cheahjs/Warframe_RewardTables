using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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
        private static Dictionary<int, string> dropPos = new Dictionary<int, string>();
        private static List<int> sortedDrop = new List<int>();
        private static int dropTableIndex = -2;

        private static Dictionary<string, int> dropOrder = new Dictionary<string, int>();

        private static NameList nameList;

        private static bool Dev = false;

        static void Main(string[] args)
        {
            dropOrder.Add("AMMO", 0);
            dropOrder.Add("BLUEPRINT", 1);
            dropOrder.Add("MISC_ITEM", 2);
            dropOrder.Add("MOD", 3);
            dropOrder.Add("POWERUP", 4);
            if (args.Length < 1)
            {
                Console.WriteLine("Syntax: <file> [dump/extract] [dev]");
                return;
            }
            nameList = NameList.Deserialize();
            if ((args.Length == 2 || args.Length == 3) && args[1] == "extract")
            {
                if (args.Length == 3)
                {
                    Console.WriteLine("Dev mode is on.");
                    Dev = true;
                }
                StartExtract(args[0]);
            }
            else
            {
                StartDump(args[0]);
            }
            nameList.Serialize();
            Console.WriteLine("Total tables parsed: {0}\r\nBad tables: {1}", TotalTables, BadTables);
            Console.ReadKey();
        }

        static void StartExtract(string filename)
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

            #region Search Table Labels
            Console.WriteLine("Finding reward table types...");
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].StartsWith("randomizedItems=") && !file[i].Contains("\"\""))
                {
                    var parts = file[i].Split('=');
                    var nameparts = parts[1].Split('/');
                    var name = nameparts[nameparts.Length - 1];
                    if (name.Trim().Length == 0) continue;
                    if (!rewardList.Contains(parts[1])) rewardList.Add(name);
                }
                else if (file[i].Contains("/Lotus/Types/DropTables/") && file[i].Length < 2000) //char limit, else we start parsing the TOC
                {
                    if (file[i].Contains("Default")) continue;
                    var parts = file[i].TrimEnd(',').Split('/');
                    var name = parts[parts.Length - 1];
                    if (name.Trim().Length == 0) continue;
                    if (!dropTableList.Contains(name)) dropTableList.Add(name);
                }
            }
            Console.WriteLine("Found {0} used reward table types.", rewardList.Count);
            Console.WriteLine("Found {0} item drop table types.", dropTableList.Count);
            //Manual overrides
            rewardList.Add("NightmareModeRewards");
            rewardList.Add("OrokinDefenseRewardsAA");
            if (!Dev)
            {
                //These are the old tables that have yet to be taken out. Remove for U11
                rewardList.Add("RaidMissionRewardsA");
                rewardList.Add("OrokinMissionRewards"); //U11 remove
                rewardList.Add("OrokinMissionRewardsB"); //U11 remove    
                rewardList.Add("OrokinMissionRewardsC"); //U11 remove
                rewardList.Add("OrokinMobDefenseRewards"); //U11 remove
                rewardList.Add("OrokinMobDefenseRewardsB"); //U11 remove
                rewardList.Add("OrokinMobDefenseRewardsC"); //U11 remove
            }
            #endregion

            #region Reward Table Search
            Console.WriteLine("Searching for {0} reward table types.", rewardList.Count);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Determining reward table names and arrangement.");
            foreach (var table in rewardList)
            {
                Console.WriteLine("Searching for " + table + "A");
                var positions = GetPositions(bigfile, table + "A" + "\u0001");
                if (positions.Count == 0)
                    Console.WriteLine("Found {0} instances.", positions.Count);
                if (positions.Count == 1)
                    rewardPos.Add(positions[0], table);
                else if (positions.Count == 2)
                {
                    rewardPos.Add(positions[0], table + "-1");
                    rewardPos.Add(positions[1], table + "-2");
                }
                if (table == "OrokinCaptureRewardsA" && Dev)
                {
                    Console.WriteLine("Searching for " + table);
                    var positions2 = GetPositions(bigfile, table + "A\u0008");
                    if (positions2.Count == 0)
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
                }
                else if (table.Contains("Orokin") && !Dev)
                {
                    Console.WriteLine("Searching for " + table);
                    var positions2 = GetPositions(bigfile, table + "\u0001");
                    if (positions2.Count == 0)
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
                }
            }
            sortedReward = rewardPos.Keys.ToList();
            sortedReward.Sort();
            Console.WriteLine("Found {0} available reward table types.", rewardPos.Count);
            #endregion

            #region Drop Table Search
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Determining drop table name and arrangement.");
            foreach (var table in dropTableList)
            {
                var searchstring = table + "A" + "\u0001";
                Console.WriteLine("Searching for {0}", searchstring);
                var positions = GetPositions(bigfile, searchstring);
                if (!dropPos.ContainsKey(positions[0])) dropPos.Add(positions[0], table);
            }
            sortedDrop = dropPos.Keys.ToList();
            sortedDrop.Sort();
            Console.WriteLine("Found {0} available drop table types.", dropPos.Count);
            #endregion

            #region Search for Reward Tables
            Console.WriteLine("--------------------------------------");
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].StartsWith("Tier") && file[i].EndsWith("{"))
                {
                    list.Clear();
                    start = true;
                    list.Add(file[i]);
                    continue;
                }
                if (start)
                {
                    if ((!file[i].StartsWith("StoreItem") && !file[i].StartsWith("Rarity") && !file[i].StartsWith("UpgradeLevel") && !file[i].StartsWith("{")
                        && !file[i].StartsWith("},") && !file[i].StartsWith("Bias") && !file[i].StartsWith("Atten")
                        && !file[i].StartsWith("Items")) 
                        || (file[i].Trim() == "}" && file[i+1].Trim() == "}"))
                    {
                        write += ParseRewardsExtracted(list.ToArray()) + "\n , , , \n";
                        start = false;
                        continue;
                    }
                    list.Add(file[i]);
                }
            }
            #endregion

            #region Search for Drop Tables
            start = false;
            var previousdrop = 0;
            list.Clear();
            var write2 = new List<string>();
            write2.Add(" ");
            write2.Add("Mod Table Drop Chance");
            for (int i = 0; i < 12; i++)
                write2.Add("Mod " + i);
            write2.Add("BP Table Drop Chance");
            for (int i = 0; i < 7; i++)
                write2.Add("BP " + i);
            for (int i = 0; i < file.Length; i++)
            {
                if ((file[i].StartsWith("DROP_AMMO") || file[i].StartsWith("DROP_BLUEPRINT") ||
                     file[i].StartsWith("DROP_MISC_ITEM")
                     || file[i].StartsWith("DROP_MOD") || file[i].StartsWith("DROP_POWERUP")) && file[i].EndsWith("{"))
                {
                    var curorder = dropOrder[file[i].Split(new[] {'_'}, 2)[1].TrimEnd(new[] {'=', '{'})];
                    if (curorder <= previousdrop)
                    {
                        var lst = ParseDropsExtracted(list.ToArray());
                        if (lst != null)
                            for (int h = 0; h < lst.Length; h++)
                                write2[h] += "," + lst[h];
                        list.Clear();
                        start = true;
                        list.Add(file[i]);
                        previousdrop = curorder;
                        continue;
                    }
                    else
                    {
                        previousdrop = curorder;
                        list.Add(file[i]);
                        continue;
                    }
                }
                if (!start) continue;
                if (file[i].StartsWith("DropChance") || file[i].StartsWith("GearTables") || file[i].StartsWith("OverrideLevelAdjustedBiasAtten")
                    || file[i].StartsWith("LevelRange") || file[i].StartsWith("Gear") || file[i].StartsWith("SpecifyEntity") || file[i].StartsWith("ItemType")
                    || file[i].StartsWith("Bias") || file[i].StartsWith("Rarity Constant") || file[i].StartsWith("Probability") || file[i].StartsWith("{")
                    || file[i].StartsWith("}") || file[i].StartsWith("EntityType"))
                    list.Add(file[i]);
                else
                {
                    var lst = ParseDropsExtracted(list.ToArray());
                    if (lst != null)
                        for (int h = 0; h < lst.Length; h++)
                            write2[h] += "," + lst[h];
                    list.Clear();
                    start = false;
                }
            }
            #endregion

            Console.WriteLine("Writing to file.");
            File.WriteAllText(filename + ".rewards.csv", write);
            File.WriteAllText(filename + ".drops.csv", string.Join("\n", write2));
            Console.WriteLine("Done.");
        }

        static void StartDump(string filename)
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
                        write += ParseRewardsDumped(list.ToArray()) + "\n , , , \n";
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
                        write += ParseRewardsDumped(list.ToArray()) + "\n , , , \n";
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

        static string ParseRewardsExtracted(string[] file)
        {
            if (file.Length == 0) return "";
            var tier = int.Parse(file[0].Substring(4, 1)) + 1;
            if (tier == 1)
                rewardTableIndex++;
            var tablename = "???";
            try
            {
                tablename = rewardPos[sortedReward[rewardTableIndex]];
                tablename = nameList.GetName(tablename);
            }
            catch   //Since there may be a mismatch of tables available and tables being used, fail gracefully.
            {
            }
            Console.WriteLine("Found new reward table. Name: {0} | Reward Tier: {1}", tablename, tier);
            var list = new List<Item>();
            for (int i = (Dev ? 3 : 2); i < file.Length; 
                i = i + ((Dev && string.Join("", file).Contains("Bias=")) ? 7 : 5))
            {
                /*var nameparts = file[i].Split('/');
                var name = nameparts[nameparts.Length - 1];
                name = name.Substring(0, name.Length - 9);
                name = nameList.GetName(name);*/
                var name = nameList.GetName(file[i].Split('=')[1]);
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
            write = list.Aggregate(write, (current, item) => current + ("\n" + item.StoreName + "," + item.Chance + "," + item.Interval + "," + item.Rarity));
            return write;
        }

        static string ParseRewardsDumped(string[] file)
        {
            if (file.Length == 0) return "";
            var tier = int.Parse(file[0].Substring(4, 1)) + 1;
            Console.WriteLine("New table found, parsing. Tier {0}", tier);
            var list = new List<Item>();
            for (int i = 1; i < file.Length; i = i + 3)
            {
                /*var nameparts = file[i].Split('/');
                var name = nameparts[nameparts.Length - 1];
                name = name.Substring(0, name.Length - 9);
                name = nameList.GetName(name);*/
                var name = nameList.GetName(file[i].Split('=')[1]);
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
            write = list.Aggregate(write, (current, item) => current + ("\n" + item.StoreName + "," + item.Chance + "," + item.Interval + "," + item.Rarity));
            return write;
        }

        static string[] ParseDropsExtracted(string[] file)
        {
            if (file.Length == 0) return null;
            var tablename = "???";
            if (dropTableIndex == -2)
            {
                tablename = "DefaultPickups";
                dropTableIndex++;
            }
            else
            {
                dropTableIndex++;
                try
                {
                    tablename = dropPos[sortedDrop[dropTableIndex]];
                    tablename = tablename.Substring(0, tablename.Length - 9);
                }
                catch //Since there may be a mismatch of tables available and tables being used, fail gracefully.
                {
                }
            }
            tablename = nameList.GetName(tablename);
            Console.WriteLine("Found new drop table. Name: {0}", tablename);
            var start = false;
            var currentdrop = "";
            var name = "";
            var mod = new List<DropItem>();
            var modchance = "?";
            var bp = new List<DropItem>();
            var bpchance = "?";
            for (int i = 0; i < file.Length; i++)
            {
                if ((file[i].StartsWith("DROP_AMMO") || file[i].StartsWith("DROP_BLUEPRINT") ||
                     file[i].StartsWith("DROP_MISC_ITEM")
                     || file[i].StartsWith("DROP_MOD") || file[i].StartsWith("DROP_POWERUP")) && file[i].EndsWith("{"))
                {
                    if (file[i].StartsWith("DROP_BLUEPRINT"))
                    {
                        start = true;
                        currentdrop = "bp";
                    }
                    else if (file[i].StartsWith("DROP_MOD"))
                    {
                        start = true;
                        currentdrop = "mod";
                    }
                    else
                    {
                        start = false;
                    }
                }
                if (start)
                {
                    if (file[i].StartsWith("ItemType") || file[i].StartsWith("EntityType"))
                    {
                        var item = file[i].Split('=')[1];
                        /*var parts = item.Split('/');
                        item = parts[parts.Length - 1];
                        item = nameList.GetName(item);*/
                        item = nameList.GetName(item);
                        switch (currentdrop)
                        {
                            case "bp":
                                bp.Add(new DropItem {Name = item});
                                break;
                            case "mod":
                                mod.Add(new DropItem { Name = item });
                                break;
                        }
                    }
                    else if (file[i].StartsWith("DropChance"))
                    {
                        var chance = file[i].Split('=')[1];
                        switch (currentdrop)
                        {
                            case "bp":
                                bpchance = chance;
                                break;
                            case "mod":
                                modchance = chance;
                                break;
                        }
                    }
                }

            }
            var rtn = new List<string>();
            rtn.Add(tablename);
            rtn.Add(modchance);
            for (int i = 0; i < 12; i++)
            {
                rtn.Add(mod.Count > i ? mod[i].Name : "");
            }
            rtn.Add(bpchance);
            for (int i = 0; i < 7; i++)
            {
                rtn.Add(bp.Count > i ? bp[i].Name : "");
            }
            return rtn.ToArray();
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
        public string Rarity;
        public double Chance;
        public double Interval;
    }

    public class DropItem
    {
        public string Name;
        public string Bias;
    }

    public class NameList
    {
        public Dictionary<string, string> Names;

        public static NameList Deserialize()
        {
            if (File.Exists("names.json"))
                return JsonConvert.DeserializeObject<NameList>(File.ReadAllText("names.json"));
            return new NameList {Names = new Dictionary<string, string>()};
        }

        public void Serialize()
        {
            File.WriteAllText("names.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public string GetName(string name)
        {
            if (Names.ContainsKey(name))
            {
                if (Names[name] != "")
                    return Names[name];
            }
            else
                Names.Add(name, "");
            return name;
        }
    }
}
