using System;
using System.Collections;
using System.IO;

public class Config
{
    private static Hashtable _Config;
	
	public static void Initialize() {
        _Config = new Hashtable();
        StreamReader input = new StreamReader("properties.txt");
        string line = null;
        while((line = input.ReadLine()) != null) {
			if(line[0] == '#' || line.Length < 0) continue;
            int pos = line.IndexOf("=");
            string key = line.Substring(0, pos).Trim();
            string val = line.Substring(pos + 1).Trim();
            _Config[key] = val;
			Spacecraft.Log("cfg " + key + "=" + val);
        }
		input.Close();
	}
	
	public static string Get(string key) {
		if(!Contains(key)) return null;
		return (string)(_Config[key]);
	}
	
	public static bool Contains(string key) {
		return _Config.ContainsKey(key);
	}
}