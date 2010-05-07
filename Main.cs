using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Web;

class Spacecraft
{
    public static void Main()
    {
        Log("{0} is starting...", "Spacecraft");
        if(!File.Exists("admins.txt")) {
            Log("Note: admins.txt does not exist, creating.");
            File.Create("admins.txt");
        }
        
        if(!File.Exists("properties.txt")) {
            Log("Error: could not find properties.txt!");
            return;
        } else {
			Config.Initialize();
        }
        
        Block.MakeNames();

        LoadRanks();

        MinecraftServer serv = new MinecraftServer();
		
        serv.Start();
		
        Spacecraft.Log("Bye!");
		Spacecraft.Log("");
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
	
	private static object logfile = new object();
    
    public static void Log(string text)
    {
        if(!File.Exists("server.log")) {
            File.Create("server.log");
        }
		lock(logfile) {
	        StreamWriter sw = new StreamWriter("server.log", true);
			if(text == "") {
				sw.WriteLine();
				Console.WriteLine();
			} else {
		        sw.WriteLine("{0}\t{1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), text);
		        Console.WriteLine("{0}\t{1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), text);
			}
		    sw.Close();
		}
    }
	
    public static void Log(string format, params object[] args)
    {
        Log(String.Format(format, args));
    }
	
	/*public static string UrlEncode( string input ) {
	     StringBuilder output = new StringBuilder();
	     for( int i = 0; i < input.Length; i++ ) {
	         if( ( input[i] >= '0' && input[i] <= '9' ) ||
	             ( input[i] >= 'a' && input[i] <= 'z' ) ||
	             ( input[i] >= 'A' && input[i] <= 'Z' ) ||
	             input[i] == '-' || input[i] == '_' || input[i] == '.' || input[i] == '~' ) {
	             output.Append(input[i]);
	         } else if( Array.IndexOf<char>( reservedChars, input[i] ) != -1 ) {
	             output.Append('%').Append(((int)input[i]).ToString( "X" ));
	         }
	     }
	     return output.ToString();
	 }*/
}
