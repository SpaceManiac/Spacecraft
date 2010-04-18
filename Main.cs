using System;
using System.IO;
using System.Collections;

class Spacecraft
{
	public static Hashtable Config;
		
	public static void Main()
	{
		Log("Spacecraft is starting...");
		
		string[] files = new string[] { "banned.txt", "banned-ip.txt", "admins.txt" };
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
		
		Server serv = new Server();
		serv.Start();
		Spacecraft.Log("Bye!");
		Environment.Exit(0);
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
