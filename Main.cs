using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Web;

class Spacecraft
{
    public static Hashtable Config;
        
    public static void Main()
    {
        Log("Spacecraft is starting...");
        if(!File.Exists("admins.txt")) {
            Log("Note: admins.txt does not exist, creating.");
            File.Create("admins.txt");
        }
        
        if(!File.Exists("properties.txt")) {
            Log("Error: could not find properties.txt!");
            return;
        } else {
            Config = new Hashtable();
            StreamReader input = new StreamReader("properties.txt");
            string line = null;
            while((line = input.ReadLine()) != null) {
                int pos = line.IndexOf("=");
                string key = line.Substring(0, pos);
                string val = line.Substring(pos + 1);
                Config[key] = val;
            }
			input.Close();
        }
        
        Block.MakeNames();

        LoadRanks();

        MinecraftServer serv = new MinecraftServer();
        serv.Start();
        Spacecraft.Log("Bye!");
        Environment.Exit(0);
    }

    public static void LoadRanks()
    {
		Player.RankedPlayers.Clear();
        StreamReader Reader = new StreamReader("admins.txt");
        string[] Lines = Reader.ReadToEnd().Split(new string[] {System.Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
        Reader.Close();

        foreach (var line in Lines)
        {
            string[] parts;
            string rank;
            parts = line.Split('=');

            rank = parts[0].Substring(0, 1).ToUpper() + parts[0].Substring(1, parts[0].Length - 1);

            Rank assignedRank = (Rank)Enum.Parse(typeof(Rank), rank);

            if (!Player.RankedPlayers.ContainsKey(assignedRank) || Player.RankedPlayers[assignedRank] == null)
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
        /*if(!File.Exists("server.log")) {
            File.Create("server.log");
        }*/
        StreamWriter sw = new StreamWriter("server.log", true);
        sw.WriteLine("{0}\t{1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), text);
        sw.Close();
        Console.WriteLine("{0}\t{1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), text);
    }
	
}
