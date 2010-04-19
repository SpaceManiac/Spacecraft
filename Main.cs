using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

class Spacecraft
{
	public static Hashtable Config;
		
	public static void Main()
	{
		Log("Spacecraft is starting...");
		
		string[] files = new string[] { "banned.txt", "banned-ip.txt", "admins.txt",  };
		foreach(string file in files) {
			if(!File.Exists(file)) {
				Log("Note: " + file + " does not exist, creating.");
				File.Create(file);
			}
		}
		
		if(!File.Exists("server.properties")) {
			Log("Error: could not find server.properties!");
			return;
		} else {
			Config = new Hashtable();
			StreamReader input = new StreamReader("server.properties");
			string line = null;
			while((line = input.ReadLine()) != null) {
				int pos = line.IndexOf("=");
				string key = line.Substring(0, pos);
				string val = line.Substring(pos + 1);
				Config[key] = val;
			}
		}
		
		Block.MakeNames();

        InitializeRanks();

		Server serv = new Server();
		serv.Start();
		Spacecraft.Log("Bye!");
		Environment.Exit(0);
	}

    private static void InitializeRanks()
    {
        StreamReader Reader = new StreamReader("admins.txt");
        string[] Lines = Reader.ReadToEnd().Split(new string[] {System.Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in Lines)
        {
            string[] parts;
            string rank;
            parts = line.Split('=');

            rank = parts[0].Substring(0, 1).ToUpper() + parts[0].Substring(1, parts[0].Length - 1);

            Player.RankEnum assignedRank = (Player.RankEnum)Enum.Parse(typeof(Player.RankEnum), rank);

            if (!Player.RankedPlayers.ContainsKey(assignedRank) || Player.RankedPlayers[assignedRank] != null)
            {
                Player.RankedPlayers[assignedRank] = new List<string>();
            }

            string[] people = parts[1].Split(',');

            for (int i = 0; i < people.Length; i++)
            {
                string name = people[i];
                Player.RankedPlayers[assignedRank].Add(name);
            }

        }
    }
	
	public static void Log(string text)
	{
		if(!File.Exists("server.log")) {
			File.Create("server.log");
		}
		//StreamWriter sw = File.Open("server.log", FileMode.Append);
		//sw.WriteLine("      {0}  {1}", DateTime.Now.ToString("H:mm:ss"), text);
		Console.WriteLine("      {0}  {1}", DateTime.Now.ToString("H:mm:ss"), text);
	}
}
