using System;
using System.Collections.Generic;
using System.IO;

public class Config
{
    private static Dictionary<string, string> _Config;
	
	public static void Initialize() {
        _Config = new Dictionary<string, string>();
        StreamReader input = new StreamReader("properties.txt");
        string[] lines = input.ReadToEnd().Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        input.Close();
        foreach (var line in lines)
        {
			if(line[0] == '#') 
                continue;
            int pos = line.IndexOf("=");
            string key = line.Substring(0, pos).Trim();
            string val = line.Substring(pos + 1).Trim();
            _Config[key] = val;
			//Spacecraft.Log("Configging: " + key + "=" + val);
        }
		
	}
	
	public static string Get(string key, string def) {
		if(Contains(key))
            return (_Config[key]);
        else
            return def;
	}
	public static string Get(string key) {
		return Get(key, null);
	}
	
	public static bool GetBool(string key, bool def) {
		string val = Get(key);
		if(val == null)
			return def;
		else
			val = val.ToLower();
		return (val != null && val == "1" || val == "yes" || val == "true" || val == "on");
	}
	public static bool GetBool(string key) {
		return GetBool(key, false);
	}
	
	public static int GetInt(string key, int def) {
		string val = Get(key);
		if(val == null)
			return def;
		else
			return Convert.ToInt32(val);
	}
	public static int GetInt(string key) {
		return GetInt(key, 0);
	}
	
	public static bool Contains(string key) {
		return _Config.ContainsKey(key);
	}
}