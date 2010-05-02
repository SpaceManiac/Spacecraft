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
			Spacecraft.Log("Configging: " + key + "=" + val);
        }
		
	}
	
	public static string Get(string key) {
		if(Contains(key))
            return (_Config[key]);
        else
            return null;
	}
	
	public static bool Contains(string key) {
		return _Config.ContainsKey(key);
	}
}