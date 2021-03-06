using System;
using System.Collections.Generic;
using System.IO;

namespace spacecraft
{
	public class ChatCommandHandling
	{
		public static string RulesText;
		static Dictionary<String, ChatCommands.ChatCommandBase> Commands;

		static ChatCommandHandling()
		{
			Commands = new Dictionary<String, ChatCommands.ChatCommandBase>();
			
			// Guest basics
			Commands.Add("clear", new ChatCommands.ClearChat());
			Commands.Add("go", new ChatCommands.LandmarkGoto());
			Commands.Add("help", new ChatCommands.Help());
			Commands.Add("me", new ChatCommands.ThirdPerson());
			Commands.Add("staff", new ChatCommands.StaffList());
			
			// Builder basics
			Commands.Add("mark", new ChatCommands.LandmarkAdd());
			Commands.Add("paint", new ChatCommands.Paint());
			Commands.Add("tp", new ChatCommands.Teleport());
			Commands.Add("whois", new ChatCommands.WhoIs());
			
			// Rank manipulation
			Commands.Add("ban", new ChatCommands.RankBanned());
			Commands.Add("builder", new ChatCommands.RankBuilder());
			Commands.Add("guest", new ChatCommands.RankGuest());
			Commands.Add("mod", new ChatCommands.RankMod());
			Commands.Add("rerank", new ChatCommands.ReloadRanks());
			Commands.Add("unban", new ChatCommands.RankGuest());
			
			// Map manipulation tools
			Commands.Add("convert", new ChatCommands.Convert());
			Commands.Add("dehydrate", new ChatCommands.Dehydrate());
			Commands.Add("physics", new ChatCommands.Physics());
			Commands.Add("place", new ChatCommands.Place());
			
			// Other moderator tools
			Commands.Add("bring", new ChatCommands.Summon());
			Commands.Add("diag", new ChatCommands.Diagnostics());
			Commands.Add("kick", new ChatCommands.Kick());
			Commands.Add("rmmark", new ChatCommands.LandmarkRemove());
			Commands.Add("say", new ChatCommands.Broadcast());
			
			// Global admin tools
			Commands.Add("config", new ChatCommands.Configure());
			Commands.Add("exit", new ChatCommands.Exit());
			Commands.Add("save", new ChatCommands.SaveMap());
			Commands.Add("setspawn", new ChatCommands.SetSpawn());

			// Experimental commands
			Commands.Add("mob", new ChatCommands.SpawnMob());
			Commands.Add("resend", new ChatCommands.ResendMap());

			// Only add the /rules command if rules.txt exists.
			if(File.Exists("rules.txt")) {
				RulesText = File.ReadAllText("rules.txt").TrimEnd();
				if (RulesText != "")
					Commands.Add("rules", new ChatCommands.Rules());
			}
		}

		/// <summary>
		/// Lookup the command cmd, and execute it using args as the arguments. sender is used to post error messages back to the user.
		/// </summary>
		/// <param name="sender">The Player attempting to execute the command.</param>
		/// <param name="cmd">Command to execute, e.g. "me"</param>
		/// <param name="args">Argument passed to command, e.g. "uses /me sucessfully"</param>
		///
		static public void Execute(Player sender, string cmd, string args)
		{
			if (Commands.ContainsKey(cmd))
			{
				if (sender.rank >= Commands[cmd].RankNeeded) {
					try {
						Commands[cmd].Run(sender, cmd, args);
					}
					catch(NotImplementedException) {
						sender.PrintMessage(Color.CommandError + "That command's not implemented!");
					}
				} else {
					sender.PrintMessage(Color.CommandError + "You don't have permission to use that command.");
				}
			} else {
				sender.PrintMessage(Color.CommandError + "Unknown command " + cmd);
			}
		}

		static public void WrapMessage(Player sendto, string message, string prefix)
		{
			if(prefix.Length > 4)
				prefix = prefix.Substring(0, 4);
			
			while (message.Length > 60)
			{
				int i = message.LastIndexOf(' ', 60, 60);
				if(i == -1) i = 60;
				
				sendto.PrintMessage(prefix + message.Substring(0, i));
				message = message.Substring(i);
			}
			sendto.PrintMessage(prefix + message);
		}

		static public void WrapMessage(Player sendto, string message)
		{
			WrapMessage(sendto, message, Color.CommandResult);
		}

		static public string GetHelp(string cmd)
		{
			if (Commands.ContainsKey(cmd))
			{
				return Commands[cmd].HelpMsg + " (" + Commands[cmd].RankNeeded.ToString() + ")";
			}
			else
			{
				return "";
			}
		}

		static public string GetCommandList(Rank rank)
		{
			string result = "";
			foreach (KeyValuePair<string, ChatCommands.ChatCommandBase> kvp in Commands)
			{
				if (rank >= kvp.Value.RankNeeded)
				{
					result += " " + kvp.Key;
				}
			}
			return result;
		}
	}

	namespace ChatCommands
	{
		public abstract class ChatCommandBase
		{
			public abstract Rank RankNeeded { get; }
			public abstract string HelpMsg { get; }
			public abstract void Run(Player sender, string cmd, string arg);
		}
	}
}