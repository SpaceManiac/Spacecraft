using System;
using System.Collections.Generic;
using System.IO;

namespace spacecraft
{
	public static class Config
	{
		private static Dictionary<string, string> _Config;

		private const string CONFIG_FILENAME = "properties.txt";

		static Config()
		{
			_Config = new Dictionary<string, string>();

			if (!File.Exists(CONFIG_FILENAME))
			{
				WriteDefaultConfig();
			}
			StreamReader input = new StreamReader(CONFIG_FILENAME);
			string raw = input.ReadToEnd();
			raw = raw.Replace("\r\n", "\n"); // Just in case we have to deal with silly Windows/UNIX line-endings.
			string[] lines = raw.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			input.Close();
			foreach (var line in lines)
			{
				if (line[0] == '#')
					continue;
				int pos = line.IndexOf("=");
				string key = line.Substring(0, pos).Trim();
				string val = line.Substring(pos + 1).Trim();
				_Config[key] = val;
				//Spacecraft.Log("Configging: " + key + "=" + val);
			}

		}

		private static void WriteDefaultConfig()
		{
			StreamWriter fh = new StreamWriter(CONFIG_FILENAME);
			fh.WriteLine("# Spacecraft default configuration file");
			fh.WriteLine("# This file was auto-generated");
			fh.WriteLine();
			fh.WriteLine("port = 25565");
			fh.WriteLine("server-name = Minecraft Server");
			fh.WriteLine("motd = Powered by " + Color.Green + "Spacecraft");
			fh.WriteLine("max-players = 16");
			fh.WriteLine("verify-names = true");
			fh.Close();
		}

		public static string DefinedList()
		{
			string r = "";
			foreach (KeyValuePair<string, string> kvp in _Config) {
				r += " " + kvp.Key;
			}
			return r;
		}

		public static string Get(string key, string def)
		{
			if (Contains(key))
				return (_Config[key]);
			else
				return def;
		}

		public static bool GetBool(string key, bool def)
		{
			string val = Get(key, null);
			if (val == null)
				return def;
			else
				return StrIsTrue(val);
		}

		public static int GetInt(string key, int def)
		{
			string val = Get(key, null);
			if (val == null)
				return def;
			else
				return Convert.ToInt32(val);
		}

		public static bool Contains(string key)
		{
			return _Config.ContainsKey(key);
		}
		
		// helpers
		public static bool StrIsTrue(string val)
		{
			val = val.ToLower().Trim();
			return (val == "1" || val == "yes" || val == "true" || val == "on");
		}
		
		public static string OnOffStr(bool val)
		{
			return (val ? "On" : "Off");
		}
	}
}