using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
//using System.Threading;
using System.Web;
using System.Security.Cryptography;
using System.Text;

namespace spacecraft
{
	class Spacecraft
	{
        const bool DEBUG = true;
		public static Random random;

		public static void Main()
		{
			try {
				Log("Spacecraft is starting...");
				if (!File.Exists("admins.txt")) {
					Log("Note: admins.txt does not exist, creating.");
					File.Create("admins.txt");
				}

				// allow an explicit seed
				int seed = Config.GetInt("random-seed", -1);
				if (seed != -1) {
					random = new Random(seed);
				} else {
					random = new Random();
				}
				LoadRanks();

				Server serv = new Server();

				serv.Start();

				Spacecraft.Log("Bye!");
				Spacecraft.Log("");
				Environment.Exit(0);
			}
			catch (Exception e) {
				// Something went wrong and wasn't caught
				Console.WriteLine("===FATAL ERROR===");
				Console.WriteLine(e.Message);
				Console.WriteLine(e.Source);
				Console.WriteLine();
				Console.Write(e.StackTrace);
				Console.Read();
			}
		}

		public static void LoadRanks()
		{
			Player.PlayerRanks.Clear();
			StreamReader Reader = new StreamReader("admins.txt");
			string[] Lines = Reader.ReadToEnd().Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			Reader.Close();

			foreach (string line in Lines) {
				string[] parts = line.Split('=');

				string rankStr = parts[0].Trim();
				rankStr = rankStr.Substring(0, 1).ToUpper() + rankStr.Substring(1, parts[0].Length - 1);
				Rank rank = (Rank) Enum.Parse(typeof(Rank), rankStr);

				string[] people = parts[1].Split(',');
				foreach(string name in people) {
					Player.PlayerRanks[name.Trim().ToLower()] = rank;
				}
			}
		}

		private static object logfileMutex = new object();

		public static void Log(string text)
		{
			if (!File.Exists("server.log")) {
				File.Create("server.log");
			}

			lock (logfileMutex) {
				StreamWriter sw = new StreamWriter("server.log", true);
				if (text == "") {
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

		private static object errorfileMutex = new object();

		public static void LogError(string text)
		{
			lock (errorfileMutex)
			{
				StreamWriter sw = new StreamWriter("error.log", true);
				sw.Write("==== ");
				sw.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
				sw.Write(" ====");
				sw.WriteLine();
				sw.WriteLine(text);
				sw.Close();
				Log("ERROR! Check error.log for details!");
			}
		}

		public static void LogError(string format, params object[] args)
		{
			LogError(String.Format(format, args));
		}

        public static void Debug(string format, params object[] args)
        {
            Debug(string.Format(format, args));
        }

        public static void Debug(string text)
        {
            System.Diagnostics.Debug.WriteLineIf(DEBUG, text);
        }

		public static string StripColors(string s)
		{
			if(s.IndexOf("&") == -1) return s;
			string r = "";
			for(int i = 0; i < s.Length; ++i) {
				if(s[i] == '&' && i != s.Length - 1) {
					++i;
				} else {
					r += s[i];
				}
			}
			return r;
		}

		public static string MD5sum(string Value)
		{
			MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
			byte[] data = Encoding.ASCII.GetBytes(Value);
			data = x.ComputeHash(data);
			StringBuilder ret = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
				ret.Append(data[i].ToString("x2").ToLower());
			return ret.ToString();
		}
	}
}