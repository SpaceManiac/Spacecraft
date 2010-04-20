using System;
using System.IO;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using System.Timers;

public class Server
{
    private ArrayList connections;
    private bool firstbeat;
    private System.Timers.Timer beattimer;
    private System.Timers.Timer phystimer;
    private TcpListener srv;
    
    public static Random rnd;
    public int salt;
    public Map map;
    public Int32 port;
    public int maxplayers;
    public string name;
    public string motd;
    
    public static ManualResetEvent OnExit = new ManualResetEvent(false);
    
    public Server()
    {
        rnd = new Random();
        salt = rnd.Next(100000, 999999);
        srv = null;
        port = 13000;
        maxplayers = 16;
        firstbeat = true;
        name = "Spacecraft Alpha";
        motd = "Haldo there!";
    }
    
    public void Start()
    {
        map = new Map();
        if(File.Exists("level.dat")) {
            map.Load("level.dat");
        } else {
            map.Generate();
            map.SetTile(0, 0, 0, Block.Books);
            map.SetTile(0, 1, 0, Block.Brick);
            map.SetTile(1, 0, 0, Block.MossyCobble);
            map.SetTile(0, 0, 1, Block.Grass);
            map.Save("level.dat");
        }
        
        try {
            if(Spacecraft.Config["port"] != null) {
                //port = Convert.ToInt32(Spacecraft.Config["port"]);
            }
            
            //int maxplayers = 16;
            if(Spacecraft.Config["max-players"] != null) {
                //maxplayers = Convert.ToInt32(Spacecraft.Config["max-players"]);
            }
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
            
            OnExit.WaitOne();
        }
        catch(SocketException e)
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
        map.Save("level.dat");
        GC.Collect();
    }
    
    private void PhysTimer(object x, ElapsedEventArgs y)
    {
        map.Physics(this);
    }
    
    private void Heartbeat()
    {
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
        builder.Append("false");
        
        builder.Append("&version=7");
        
        builder.Append("&salt=");
        builder.Append(salt.ToString());
        
    
        string postcontent = builder.ToString(); /*
            "port=" + port +
            "&users=" + connections.Count +
            "&max=" + maxplayers +
            "&name=" + name +
            "&public=" + "false" +
            "&version=7" +
            "&salt=" + salt;
                       */
        byte[] post = Encoding.ASCII.GetBytes(postcontent);
        
        HttpWebRequest req = (HttpWebRequest) WebRequest.Create("http://minecraft.net/heartbeat.jsp");
        req.ContentType = "application/x-www-form-urlencoded";
        req.Method = "POST";
        req.ContentLength = post.Length;
        Stream o = req.GetRequestStream();
        o.Write(post, 0, post.Length);
        o.Close();
        
        WebResponse resp = req.GetResponse();
        if(resp == null) {
            Spacecraft.Log("Error: unable to heartbeat!");
            return;
        }
        
        StreamReader sr = new StreamReader(resp.GetResponseStream());
        string data = sr.ReadToEnd().Trim();
        if(firstbeat) {
            if(data.Substring(0, 7) != "http://") {
                Spacecraft.Log("Error: unable to retreive external URL!");
            } else {
                Spacecraft.Log("Salt is " + salt);
                Spacecraft.Log("To connect directly to this server, surf to: " + data);
                StreamWriter outfile = File.CreateText("externalurl.txt");
                outfile.Write(data);
                outfile.Close();
                Spacecraft.Log("(This is also in externalurl.txt)");
                firstbeat = false;
            }
        }
    }
        
    public void AcceptClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener) ar.AsyncState;
        TcpClient client = listener.EndAcceptTcpClient(ar);
        connections.Add(new Connection(client, this));
        srv.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), srv);
    }
    
    public void LoadMap()
    {
        // ...
    }
    
    public void Shutdown()
    {
        Spacecraft.Log("Spacecraft is shutting down...");
        map.Save("level.dat");
    }
    
    public void SendAll(byte[] data)
    {
        for(int i = 0; i < connections.Count; ++i) {
            Connection c = (Connection)(connections[i]);
            if(!c.connected) {
                connections.Remove(c);
                --i;
                continue;
            }
            c.Send(data);
        }
    }
    
    public void SendAllExcept(byte[] data, Connection nosend)
    {
        for(int i = 0; i < connections.Count; ++i) {
            Connection c = (Connection)(connections[i]);
            if(!c.connected) {
                connections.Remove(c);
                --i;
                continue;
            }
            if(c == nosend) {
                continue;
            }
            
            c.Send(data);
        }
    }
    
    public ArrayList GetAllPlayers()
    {
        ArrayList r = new ArrayList();
        for(int i = 0; i < connections.Count; ++i) {
            Connection c = (Connection)(connections[i]);
            if(!c.connected) {
                connections.Remove(c);
                --i;
                continue;
            }
            r.Add(c.player);
        }
        return r;
    }
    
    public Player GetPlayer(string name)
    {
        name = name.ToLower();
        for(int i = 0; i < connections.Count; ++i) {
            Connection c = (Connection)(connections[i]);
            if(!c.connected) {
                connections.Remove(c);
                --i;
                continue;
            }
            if(c.player.name.ToLower() == name) {
                return c.player;
            }
        }
        return null;
    }
    
    public Connection GetConnection(string name)
    {
        name = name.ToLower();
        for(int i = 0; i < connections.Count; ++i) {
            Connection c = (Connection)(connections[i]);
            if(!c.connected) {
                connections.Remove(c);
                --i;
                continue;
            }
            if(c.player.name.ToLower() == name) {
                return c;
            }
        }
        return null;
    }
}