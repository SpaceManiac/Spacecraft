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
using System.Diagnostics;

namespace spacecraft
{
	public partial class Server
	{
		static public Server theServ;
		static public ManualResetEvent OnExit = new ManualResetEvent(false);
		static private bool Running = true;

		private bool Initialized = false;

		private TcpListener Listener;
        private HttpListener HTTPListener;

		public List<Player> Players { get; private set; }
		public Map map { get; protected set; }
		public int salt { get; protected set; }
		public int port { get; protected set; }
        public int HTTPport { get; protected set; }
		public int maxplayers { get; protected set; }
		public string name { get; protected set; }
		public string motd { get; protected set; }
		public string serverhash { get; protected set; }

		public Server()
		{
			if (theServ != null) return;
			theServ = this;

			salt = Spacecraft.random.Next(100000, 999999);

			port = Config.GetInt("port", 25565);
            HTTPport = Config.GetInt("http-port", port+1);
			maxplayers = Config.GetInt("max-players", 16);
			name = Config.Get("server-name", "Minecraft Server");
			motd = Config.Get("motd", "Powered by " + Color.Green + "Spacecraft");
		}

		public void Start()
		{
			// Initialize the map, using the saved one if it exists.
			if (File.Exists(Map.levelName)) {
					map = Map.Load(Map.levelName);
			} else if(File.Exists("server_level.dat")) {
					map = Map.Load("server_level.dat");
			}

			if (map == null) {
				map = new Map();
				map.Generate();
				map.Save(Map.levelName);
			}

			map.BlockChange += new Map.BlockChangeHandler(map_BlockChange);

			try
			{
				Players = new List<Player>();

				Listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
				Listener.Start();

                HTTPListener = new HttpListener();
                HTTPListener.Prefixes.Add("http://*:" + HTTPport + "/");
                HTTPListener.Start();

				Spacecraft.Log("Listening on port " + port.ToString());
				Spacecraft.Log("Server name is " + name);
				Spacecraft.Log("Server MOTD is " + motd);

				Running = true;

				Thread T = new Thread(AcceptClientThread);
				T.Start();

				Thread T2 = new Thread(TimerThread);
				T2.Start();

                Thread T3 = new Thread(HTTPMonitorThread);
                T3.Start();

				OnExit.WaitOne();
				Running = false;
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

		private void TimerThread()
		{
			Stopwatch clock = new Stopwatch();
			clock.Start();
			double lastHeartbeat = -30;
			double lastPhysics = -0.5;
			while(Running) {
				if(clock.Elapsed.TotalSeconds - lastHeartbeat >= 30) {
					Heartbeat();
					map.Save(Map.levelName);
					GC.Collect();
					lastHeartbeat = clock.Elapsed.TotalSeconds;
				}
				if(clock.Elapsed.TotalSeconds - lastPhysics >= 0.5) {
					map.DoPhysics();
					lastPhysics = clock.Elapsed.TotalSeconds;
				}
				Thread.Sleep(10);
			}
		}

		public void AcceptClientThread()
		{
			while(Running) {
				TcpClient Client = Listener.AcceptTcpClient();
				Player Player = new Player(Client, (byte) Players.Count);

				Player.Spawn += new Player.PlayerSpawnHandler(Player_Spawn);
				Player.Message += new Player.PlayerMsgHandler(Player_Message);
				Player.Move += new Player.PlayerMoveHandler(Player_Move);
				Player.BlockChange += new Player.PlayerBlockChangeHandler(Player_BlockChange);
				Player.Disconnect += new Player.PlayerDisconnectHandler(Player_Disconnect);

                Player.Spawn += new Player.PlayerSpawnHandler(UpdatePlayersList);
                Player.Disconnect += new Player.PlayerDisconnectHandler(UpdatePlayersList);

				Players.Add(Player);
				Player.Start();

				Thread.Sleep(10);
			}
		}

        String response = "WE GET SIGNAL!";

        public void HTTPMonitorThread()
        {
            while (Running)
            {
                HttpListenerContext Client = HTTPListener.GetContext();
                HttpListenerResponse Response = Client.Response;
                byte[] bytes = ASCIIEncoding.ASCII.GetBytes(response);

                Response.OutputStream.Write(bytes, 0, bytes.Length);
                Response.Close();
            }
        }

		public Player GetPlayer(string name)
		{
			name = name.ToLower();
			// TODO: implement abbreviations (i.e. 'Space' could become 'SpaceManiac')
			foreach(Player P in Players) {
				if(P.name.ToLower() == name) {
					return P;
				}
			}
			return null;
		}

		private void Heartbeat()
		{
			try {
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
				if (Config.GetBool("public", false)) {
					builder.Append("true");
				} else {
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
					if (data.Substring(0, 7) != "http://") {
						Spacecraft.Log("Error: unable to retreive external URL!");
					} else {
						int i = data.IndexOf('=');
						serverhash = data.Substring(i + 1);
	
						//Spacecraft.Log("Salt is " + salt);
						Spacecraft.Log("To connect directly, surf to: ");
						Spacecraft.Log(data);
						Spacecraft.Log("(This is also in externalurl.txt)");
	
						StreamWriter outfile = File.CreateText("externalurl.txt");
						outfile.Write(data);
						outfile.Close();
	
						Initialized = true;
					}
				}
			}
			catch(WebException e) {
				Spacecraft.LogError("Unable to heartbeat", e);
			}
		}

		void Player_Disconnect(Player Player)
		{
			byte ID = Player.playerID;
			Players.Remove(Player);
			foreach (Player P in Players)
			{
				P.PlayerDisconnects(ID);
			}
			MessageAll(Color.Yellow + Player.name + " has left");
		}

		void map_BlockChange(Map map, BlockPosition pos, Block BlockType)
		{
			foreach (Player P in Players)
			{
				P.BlockSet(pos, BlockType);
			}
		}

		void Player_BlockChange(BlockPosition pos, Block BlockType)
		{
			map.SetTile(pos.x, pos.y, pos.z, BlockType);
		}

		void Player_Move(Player sender, Position dest, byte heading, byte pitch)
		{
			foreach(Player P in Players) {
				if(P != sender) {
					P.PlayerMoves(sender, dest, heading, pitch);
				}
			}
		}

		void Player_Message(string msg)
		{
			MessageAll(msg);
		}

		void Player_Spawn(Player sender)
		{
			foreach (Player P in Players) {
				P.PlayerJoins(sender);
				sender.PlayerJoins(P);
			}

			MovePlayer(sender, map.spawn, map.spawnHeading, 0);
			MessageAll(Color.Yellow + sender.name + " has joined!");
		}

		public void MessageAll(string message)
		{
			foreach (Player P in Players) {
				P.PrintMessage(message);
			}
			Spacecraft.Log("[>] " + Spacecraft.StripColors(message));
		}

		public void MovePlayer(Player player, Position dest, byte heading, byte pitch)
		{
			foreach (Player P in Players) {
				P.PlayerMoves(player, dest, heading, pitch);
			}
		}

		public void ChangeBlock(BlockPosition pos, Block blockType)
		{
			foreach (Player P in Players) {
				P.BlockSet(pos, blockType);
			}
			map.SetTile(pos.x, pos.y, pos.z, blockType);
		}

		public void Shutdown()
		{
			Spacecraft.Log("Spacecraft is shutting down...");
			map.Save(Map.levelName);
		}

        // Added an external list of players, Atkins' request.
        private static object playersfile = new object();
        
        public void UpdatePlayersList(Player player)
        {
            // refresh players.txt so that it contains a list of current players
            lock (playersfile)
            {
                StreamWriter sw = new StreamWriter("players.txt", false);
                foreach (var P in Players)
                {
                    sw.WriteLine(P.name);
                }
                sw.Write(System.Environment.NewLine);
                sw.Close();
            }
        }



	}
}