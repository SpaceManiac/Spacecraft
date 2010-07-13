using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace spacecraft
{
	class Spacecraft
	{
		public const bool DEBUG = true;
		public static Random random;
		public static string dateString;
		
		// For testing purposes.
		public const int StackSize = 1 * 1024 * 1024;

#if WIN32 
		public static PerformanceCounter cpuCounter; // This is apparently not implemented on Mono, so debug constants time!
#endif

		public static void Main(string[] args)
		{
			try {				
				Log("");
				Log("Spacecraft is starting...");
				if (!File.Exists("admins.txt")) {
					Log("Note: admins.txt does not exist, creating a blank one. Be sure to add yourself!");
					File.Create("admins.txt").Close();
				}

#if WIN32
				cpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
				cpuCounter.NextValue();
#endif

				CalculateFilenames();

				// allow an explicit seed
				int seed = Config.GetInt("random-seed", -1);
				if (seed != -1) {
					random = new Random(seed);
				} else {
					random = new Random();
				}
				Player.LoadRanks();

				Server serv = new Server();

				serv.Start();

				Log("Bye!");
				Environment.Exit(0);
			}
			catch (Exception e) {
				// Something went wrong and wasn't caught
				Spacecraft.LogError("fatal uncaught exception", e);
			}
		}

		static void CalculateFilenames()
		{
			StringBuilder b = new StringBuilder();
			b.Append(DateTime.Now.Year);
			b.Append("-");
			b.Append(DateTime.Now.Month);
			b.Append("-");
			b.Append(DateTime.Now.Day);

			dateString = b.ToString();
		}

		private static object logfileMutex = new object();

		public static void Log(string text)
		{
            if ( dateString == "")
                CalculateFilenames();
			lock (logfileMutex) {
				StreamWriter sw = new StreamWriter("server-" + dateString + ".log", true);
				if (text == "") {
					sw.WriteLine();
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

		public static void LogError(string text, Exception e)
		{
			if(e == null) {
				Log("Error: " + text);
				return;
			}
			CalculateFilenames();
			lock (errorfileMutex)
			{
				StreamWriter sw = new StreamWriter("error-" + dateString + ".log", true);

				sw.Write("==== ");
				sw.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
				sw.Write(" ====");
				sw.WriteLine();
				sw.WriteLine(text);
				sw.WriteLine(e.ToString());
				sw.WriteLine();
				sw.Close();
				Log("Error: " + text + ": See error.log for details");
			}
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
		
		public static string Base64Encode(string str)
		{
		   byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(str);
		   return Convert.ToBase64String(encbuff);
		}
		
		public static string Base64Decode(string str)
		{
		   byte[] decbuff = Convert.FromBase64String(str);
		   return System.Text.Encoding.UTF8.GetString(decbuff);
		}
	}
	
	public class SpacecraftException : Exception
	{
		public SpacecraftException(string message) : base(message)
		{
		}
	}
}