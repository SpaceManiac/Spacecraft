using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using System.Web;

namespace spacecraft
{
    public partial class NewServer
    {
        static public NewServer theServ;
        static public ManualResetEvent OnExit = new ManualResetEvent(false);

        private bool Initialized = false;
        private System.Timers.Timer HeartbeatTimer;
        private System.Timers.Timer PhysTimer;
        
        private TcpListener Listener;

        public List<NewPlayer> Players { get; private set; }
        public Map map { get; protected set; }
        public int salt { get; protected set; }
        public int port { get; protected set; }
        public int maxplayers { get; protected set; }
        public string name { get; protected set; }
        public string motd { get; protected set; }
        public string serverhash { get; protected set; }

        

        public NewServer()
        {
            if (theServ != null) return;
            theServ = this;

            salt = Spacecraft.random.Next(100000, 999999);

            port = Config.GetInt("port", 25565);
            maxplayers = Config.GetInt("max-players", 16);
            name = Config.Get("server-name", "Minecraft Server");
            motd = Config.Get("motd", "Powered by " + Color.Green + "Spacecraft");
        }

        public void Start()
        {
            // Initialize the map, using the saved one if it exists.
            if (File.Exists("level.fcm")) {
                try {
                    map = Map.Load("level.fcm");
                }
                catch {
					Spacecraft.Log("Could not load level.fcm");
                    map = null;
                }
            }
            
            if (map == null) {
                map = new Map();
                map.Generate();
                map.Save("level.fcm");
            }

            try
            {
                Players = new List<NewPlayer>();

                Listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                Listener.Start();
                Spacecraft.Log("Listening on port " + port.ToString());
                Spacecraft.Log("Server name is " + name);
                Spacecraft.Log("Server MOTD is " + motd);

                Listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient),null);
				

                HeartbeatTimer = new System.Timers.Timer(30000);
                HeartbeatTimer.Elapsed += new ElapsedEventHandler(BeatTick);
                HeartbeatTimer.Start();
                Heartbeat();

                PhysTimer = new System.Timers.Timer(500);
                PhysTimer.Elapsed += new ElapsedEventHandler(PhysTick);
                PhysTimer.Start();

                OnExit.WaitOne();
            }
            catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                Listener.Stop();
            }

            Shutdown();
        }


        private void BeatTick(object sender, ElapsedEventArgs y)
        {
            Heartbeat();
            map.Save("level.fcm");
            GC.Collect();
        }

        private void PhysTick(object sender, ElapsedEventArgs y)
        {
            //map.Physics(this);
            // TODO: Get map physics to work with NewServer.
        }

        private void Heartbeat()
        {
            if (!Config.GetBool("heartbeat", true))
            {
                return;
            }

            StringBuilder builder = new StringBuilder();

            builder.Append("port=");
            builder.Append(port.ToString());

            builder.Append("&users=");
            builder.Append(Players.Count);

            builder.Append("&max=");
            builder.Append(maxplayers);

            builder.Append("&name=");
            builder.Append(name);

            builder.Append("&public=");
            if (Config.GetBool("public", false))
            {
                builder.Append("true");
            }
            else
            {
                builder.Append("false");
            }

            builder.Append("&version=7");

            builder.Append("&salt=");
            builder.Append(salt.ToString());


            string postcontent = builder.ToString();
            byte[] post = Encoding.ASCII.GetBytes(postcontent);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://minecraft.net/heartbeat.jsp");
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            req.ContentLength = post.Length;
            Stream o = req.GetRequestStream();
            o.Write(post, 0, post.Length);
            o.Close();

            WebResponse resp = req.GetResponse();
            if (resp == null)
            {
                Spacecraft.Log("Error: unable to heartbeat!");
                return;
            }

            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string data = sr.ReadToEnd().Trim();
            if (!Initialized)
            {
                if (data.Substring(0, 7) != "http://")
                {
                    Spacecraft.Log("Error: unable to retreive external URL!");
                }
                else
                {
                    Spacecraft.Log("Salt is " + salt);
                    //Spacecraft.Log("To connect directly to this server, surf to: ");
                    //Spacecraft.Log(data);
                    Console.WriteLine("To connect directly, surf to:");
                    Console.WriteLine(data); 
                    StreamWriter outfile = File.CreateText("externalurl.txt");
                    int i = data.IndexOf('=');
                    serverhash = data.Substring(i + 1);
                    outfile.Write(data);
                    outfile.Close();
                    Spacecraft.Log("(This is also in externalurl.txt)");
                    Initialized = true;
                }
            }
        }


        public void AcceptClient(IAsyncResult Result)
        {
			Spacecraft.Log("NewServer.AcceptClient");
			
            TcpClient Client = Listener.EndAcceptTcpClient(Result);
            NewPlayer newPlayer = new NewPlayer(Client, (byte) Players.Count);

            newPlayer.Spawn += new NewPlayer.PlayerSpawnHandler(newPlayer_Spawn);
            newPlayer.Message += new NewPlayer.PlayerMsgHandler(newPlayer_Message);
            newPlayer.Move += new NewPlayer.PlayerMoveHandler(newPlayer_Move);
            newPlayer.BlockChange += new NewPlayer.PlayerBlockChangeHandler(newPlayer_BlockChange);
            newPlayer.Disconnect += new NewPlayer.PlayerDisconnectHandler(newPlayer_Disconnect);

            Players.Add(newPlayer);
			
			Listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient),null);
        }

        void newPlayer_Disconnect(NewPlayer Player)
        {
            byte ID = Player.playerID;
            Players.Remove(Player);
            foreach (NewPlayer P in Players)
            {
                P.PlayerDisconnects(ID);
            }
        }

        void newPlayer_BlockChange(BlockPosition pos, Block BlockType)
        {
            foreach (NewPlayer P in Players)
            {
                P.BlockSet(pos, BlockType);
            }
        }

        void newPlayer_Move(NewPlayer sender, Position dest, byte heading, byte pitch)
        {
			foreach(NewPlayer P in Players) {
				if(P != sender) {
					P.PlayerMoves(sender, dest, heading, pitch);
				}
			}
        }

        void newPlayer_Message(string msg)
        {
            foreach(NewPlayer P in Players) {
				P.PrintMessage(msg);
			}
        }

        void newPlayer_Spawn(NewPlayer sender)
        {
            foreach (NewPlayer P in Players) {
                P.PlayerJoins(sender);
				sender.PlayerJoins(P);
            }

            MovePlayer(sender, map.spawn, map.spawnHeading, 0);
        }

        void MovePlayer(NewPlayer player, Position dest, byte heading, byte pitch) {
            foreach (NewPlayer P in Players)
            {
                P.PlayerMoves(player, dest, heading, pitch);
            }
        }

        public void Shutdown()
        {
            Spacecraft.Log("Spacecraft is shutting down...");
            map.Save("level.fcm");
        }
    }
}