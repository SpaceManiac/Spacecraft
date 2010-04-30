using System;
using System.Collections.Generic;
using System.Text;

    class ChatCommandHandling
    {
        static Dictionary<String, ChatCommands.ChatCommandBase> Commands;

        static ChatCommandHandling()
        {
            Commands = new Dictionary<String, ChatCommands.ChatCommandBase>();

            //Commands.Add("me", new CommandTemplate(ThirdPerson));
            Commands.Add("help", new ChatCommands.Help());
            Commands.Add("me", new ChatCommands.ThirdPerson());
        }
        /// <summary>
        /// Lookup the command cmd, and execute it using args as the arguments. sender is used to post error messages back to the user.
        /// </summary>
        /// <param name="sender">The Connection attempting to execute the command.</param>
        /// <param name="cmd">Command to execute, e.g. "me"</param>
        /// <param name="args">Argument passed to command, e.g. "uses /me sucessfully"</param>
        /// 
        static public void Execute(Connection sender, string cmd, string args)
        {
            if (Commands.ContainsKey(cmd))
            {
                if (sender.player.rank >= Commands[cmd].RankNeeded)
                    Commands[cmd].Run(sender, cmd, args);
                else
                    sender.Message(Color.Red + "You don't have permission to do that.");
            }
            else
                sender.Message("Command " + cmd + " does not exist.");
        }
        // Chat command implementations begin here!

        
    }




    namespace ChatCommands
    {
        public abstract class ChatCommandBase
        {
            public abstract Player.RankEnum RankNeeded { get; }
            public abstract string HelpMsg { get; }
            public abstract void Run(Connection sender, string cmd, string arg);
        }

        public class Help : ChatCommandBase
        {
            public override Player.RankEnum RankNeeded
            {
                get { return Player.RankEnum.Banned; }
            }

            public override string HelpMsg
            {
                get { return "/help: displays help information"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.Message(Color.Teal + "You are a " + Player.RankColor(sender.player.rank) + sender.player.rank.ToString());
                    string commands = "You can use:";
                    commands += " /help /me /status";
                    if (sender.player.rank >= Player.RankEnum.Builder)
                    {
                        commands += " /teleport /tp";
                    }
                    if (sender.player.rank >= Player.RankEnum.Mod)
                    {
                        commands += " /dehydrate /bring /broadcast /k /kick /place /say";
                    }
                    if (sender.player.rank == Player.RankEnum.Admin)
                    {
                        commands += " /exit /setspawn";
                    }
                    sender.Message(Color.Teal + commands);
                }
                else
                {
                    if (args[0] == '/') args = args.Substring(1);
                    string help = HelpText.Lookup(args);
                    if (help == "")
                    {
                        sender.Message(Color.DarkRed + "No help text on /" + args);
                    }
                    else
                    {
                        sender.Message(Color.Teal + help);
                    }
                }
            }


        }


        public class ThirdPerson : ChatCommandBase
        {
            public override Player.RankEnum RankNeeded
            {
                get { return Player.RankEnum.Banned; }
            }

            public override string HelpMsg
            {
                get { return "/me: third-person roleplay-like actions"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                /* /me /me Easter egg. */
                if (args == "/me")
                {
                    sender.Message(Color.Teal + "Red alert, /me /me found, PMing all players!");
                    sender.Message(Color.Teal + "Easter egg get!");
                }
                else
                {
                    if (args == "")
                    {
                        sender.Message(Color.DarkRed + "No /me message specified");
                    }
                    else
                    {
                        sender.MsgAll(" * " + sender.player.name + " " + args);
                    }
                }

            }
        }

        public class Bring : ChatCommandBase
        {
            public override Player.RankEnum RankNeeded
            {
                get { return Player.RankEnum.Mod; }
            }

            public override string HelpMsg
            {
                get { return "/bring: teleports a player to you (mod+)"; }
            }

            public override void Run(Connection sender, string cmd, string args)
            {
                if (args == "")
                {
                    sender.Message(Color.DarkRed + "No player specified");
                }
                else
                {
                    Connection c = Server.theServ.GetConnection(args);
                    if (c == null)
                    {
                        sender.Message(Color.DarkRed + "No such player " + args);
                    }
                    else
                    {
                        Player _player = sender.player;
                        c.Send(Connection.PacketTeleportSelf(_player.x, _player.y, _player.z, _player.heading, _player.pitch));
                    }
                }
            }

        }

    }