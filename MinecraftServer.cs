using System;
using System.IO;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using System.Timers;

namespace spacecraft
{
    public class MinecraftServer
    {
        private ArrayList connections;
        private ArrayList mobs;
        private bool firstbeat;
        private System.Timers.Timer beattimer;
        private System.Timers.Timer phystimer;
        private System.Timers.Timer mobtimer;
        private TcpListener srv;

        public int salt;
        public Map map;
        public int port;
        public int maxplayers;
        public string name;
        public string motd;
        public string serverhash;
        private bool justFlistBeated = false;

        static public MinecraftServer theServ;
        public static ManualResetEvent OnExit = new ManualResetEvent(false);

        public MinecraftServer()
        {
            if (theServ != null) return;
            theServ = this;

            mobs = new ArrayList();
            salt = Spacecraft.random.Next(100000, 999999);
            srv = null;
            firstbeat = true;

            port = Config.GetInt("port", 25565);
            maxplayers = Config.GetInt("max-players", 16);
            name = Config.Get("server-name", "Minecraft Server");
            motd = Config.Get("motd", "Powered by " + Color.Green + "Spacecraft");
        }

        public void Start()
        {
            map = new Map();
            if (File.Exists("level.fcm"))
            {
                map = Map.Load("level.fcm");
            }
            else
            {
                map.Generate();
                map.Save("level.fcm");
            }

            try
            {
                connections = new ArrayList();

                srv = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                srv.Start();
                Spacecraft.Log("Listening on port " + port.ToString());
                Spacecraft.Log("Server name is " + name);
                Spacecraft.Log("Server MOTD is " + motd);

                srv.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), srv);

                beattimer = new System.Timers.Timer(30000);
                beattimer.Elapsed += new ElapsedEventHandler(BeatTimer);
                beattimer.Start();
                Heartbeat();

                phystimer = new System.Timers.Timer(500);
                phystimer.Elapsed += new ElapsedEventHandler(PhysTimer);
                phystimer.Start();

                mobtimer = new System.Timers.Timer(1000.0 / 30);
                mobtimer.Elapsed += new ElapsedEventHandler(MobTimer);
                mobtimer.Start();

                OnExit.WaitOne();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                srv.Stop();
            }

            Shutdown();
        }


        private void BeatTimer(object x, ElapsedEventArgs y)
        {
            Heartbeat();
            map.Save("level.fcm");
            GC.Collect();
        }

        private void PhysTimer(object x, ElapsedEventArgs y)
        {
            map.Physics(this);
        }

        private void MobTimer(object x, ElapsedEventArgs y)
        {
            for (int i = 0; i < mobs.Count; ++i)
            {
                ((Robot)(mobs[i])).Update();
            }
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
            builder.Append(connections.Count);

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
            if (firstbeat)
            {
                if (data.Substring(0, 7) != "http://")
                {
                    Spacecraft.Log("Error: unable to retreive external URL!");
                }
                else
                {
                    Spacecraft.Log("Salt is " + salt);
                    Spacecraft.Log("To connect directly to this server, surf to: ");
                    Spacecraft.Log(data);
                    StreamWriter outfile = File.CreateText("externalurl.txt");
                    int i = data.IndexOf('=');
                    serverhash = data.Substring(i + 1);
                    outfile.Write(data);
                    outfile.Close();
                    Spacecraft.Log("(This is also in externalurl.txt)");
                    firstbeat = false;
                }
            }
        }

        public void AcceptClient(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
            connections.Add(new Connection(client));
            srv.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), srv);
        }

        public void SpawnMob(Player at)
        {
            SpawnMob(at, "Mob");
        }

        public void SpawnMob(Player at, string name)
        {
            Robot m = new Robot(name);
            m.x = at.x;
            m.y = at.y;
            m.z = at.z;
            mobs.Add(m);
            SendAll(Connection.PacketSpawnPlayer(m));
            Spacecraft.Log("Spawning mob " + name);
        }

        public void Shutdown()
        {
            Spacecraft.Log("Spacecraft is shutting down...");
            map.Save("level.fcm");
        }

        public void SendAll(byte[] data)
        {
            for (int i = 0; i < connections.Count; ++i)
            {
                Connection c = (Connection)(connections[i]);
                if (!c.connected)
                {
                    connections.Remove(c);
                    --i;
                    continue;
                }
                c.Send(data);
            }
        }

        public void SendAllExcept(byte[] data, Connection nosend)
        {
            for (int i = 0; i < connections.Count; ++i)
            {
                Connection c = (Connection)(connections[i]);
                if (!c.connected)
                {
                    connections.Remove(c);
                    --i;
                    continue;
                }
                if (c == nosend)
                {
                    continue;
                }

                c.Send(data);
            }
        }

        public ArrayList GetAllPlayers()
        {
            return GetAllPlayers(true);
        }

        public ArrayList GetAllPlayers(bool m)
        {
            ArrayList r = new ArrayList();
            for (int i = 0; i < connections.Count; ++i)
            {
                Connection c = (Connection)(connections[i]);
                if (!c.connected)
                {
                    connections.Remove(c);
                    --i;
                    continue;
                }
                r.Add(c.player);
            }
            if (m)
            {
                for (int i = 0; i < mobs.Count; ++i)
                {
                    r.Add(mobs[i]);
                }
            }
            return r;
        }

        public Player GetPlayer(string name)
        {
            name = name.ToLower();
            for (int i = 0; i < connections.Count; ++i)
            {
                Connection c = (Connection)(connections[i]);
                if (!c.connected)
                {
                    connections.Remove(c);
                    --i;
                    continue;
                }
                if (c.player.name.ToLower() == name)
                {
                    return c.player;
                }
            }
            return null;
        }

        public Connection GetConnection(string name)
        {
            name = name.ToLower();
            for (int i = 0; i < connections.Count; ++i)
            {
                Connection c = (Connection)(connections[i]);
                if (!c.connected)
                {
                    connections.Remove(c);
                    --i;
                    continue;
                }
                if (c.player.name.ToLower() == name)
                {
                    return c;
                }
            }
            return null;
        }
    }
}