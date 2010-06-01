using System;
using System.Collections.Generic;
using System.IO;

namespace spacecraft
{
    public class Config
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
            string[] lines = input.ReadToEnd().Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
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
            fh.WriteLine("max-players = 16");
            fh.WriteLine("server-name = Minecraft Server");
            fh.WriteLine("motd = Powered by " + Color.Green + "Spacecraft");
            fh.Close();
        }

        public static string GetDefinedList()
        {
            string r = "";
            foreach (KeyValuePair<string, string> kvp in _Config)
            {
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
        public static string Get(string key)
        {
            return Get(key, null);
        }

        public static bool GetBool(string key, bool def)
        {
            string val = Get(key);
            if (val == null)
                return def;
            else
                val = val.ToLower();
            return (val != null && val == "1" || val == "yes" || val == "true" || val == "on");
        }
        public static bool GetBool(string key)
        {
            return GetBool(key, false);
        }

        public static int GetInt(string key, int def)
        {
            string val = Get(key);
            if (val == null)
                return def;
            else
                return Convert.ToInt32(val);
        }
        public static int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        public static bool Contains(string key)
        {
            return _Config.ContainsKey(key);
        }
    }
}