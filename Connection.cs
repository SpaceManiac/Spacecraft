using System;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Collections;
using System.Threading;

public class Connection
{
    private TcpClient client;
    private MinecraftServer serv { get { return MinecraftServer.theServ; } }
	
	// temporary until stuff becomes saner!
    public Player _player;
    private bool _connected;
    
    private byte[] _netbuffer;
    private byte[] buffer;
    private int bufsize;
    private string _name;
    
    public bool connected { get { return _connected; } }
    
    public Player player { get { return _player; } }
    public string name { get { if(_player == null) { return _name; } else { return _player.name; } } }
    public string addr { get { return ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString(); } }
    
    public Connection(TcpClient conn)
    {
        _connected = true;
        client = conn;
        _name = addr;
        _player = null;
        _netbuffer = new byte[256];
        buffer = new byte[512];
        bufsize = 0;
        
        client.GetStream().BeginRead(_netbuffer, 0, _netbuffer.Length, new AsyncCallback(ReadCallback), this);
    }
    
    public void ReadCallback(IAsyncResult ar)
    {
        int bytesRead;
        try {
            bytesRead = client.GetStream().EndRead(ar);
        }
        catch(IOException) {
            _Disconnect();
            return;
        }
        catch(InvalidOperationException) {
            _Disconnect();
            return;
        }
        if(bytesRead == 0) {
            _Disconnect();
            return;
        }
        
        // append the new _netbuffer stuff to the buffer
        Array.Copy(_netbuffer, 0, buffer, bufsize, bytesRead);
        bufsize += bytesRead;
        //Spacecraft.Log("Read " + bytesRead + " bytes. Total = " + bufsize + ". First = " + buffer[0]);
        
        // check to see if we have a full packet
        while(bufsize > 0) {
            int packsize = PacketLen.Lookup(buffer[0]);
            if(packsize == 0) {
                break;
            } else if(bufsize >= packsize) {
                // extract packet
                byte[] packet = new byte[packsize];
                Array.Copy(buffer, packet, packsize);
                
                // shift buffer
                bufsize -= packsize;
                byte[] temp = new byte[buffer.Length];
                Array.Copy(buffer, packsize, temp, 0, bufsize);
                buffer = temp;
                
                if(packet[0] == Packet.Ident) {
                    HandleJoin(packet);
                } else if(packet[0] == Packet.PlayerSetBlock) {
                    HandleBlock(packet);
                } else if(packet[0] == Packet.PositionUpdate) {
                    HandlePosition(packet);
                } else if(packet[0] == Packet.Message) {
                    HandleMessage(packet);
                }
            } else {
                break;
            }
        }
        
        // read again
        try {
            client.GetStream().BeginRead(_netbuffer, 0, _netbuffer.Length, new AsyncCallback(ReadCallback), this);
        }
        catch(IOException) {
            _Disconnect();
            return;
        }
        catch(InvalidOperationException) {
            _Disconnect();
            return;
        }
    }
    
    // ===================================================================
    // static helpers
    
    private static short Host2Net(short x)
    {
        return IPAddress.HostToNetworkOrder(x);
    }
    private static short Net2Host(short x)
    {
        return IPAddress.NetworkToHostOrder(x);
    }
    
    private static string GetStrArray(byte[] data, int max)
    {
        string ret = "", sep = "";
        for(int i = 0; i < data.Length && i < max; ++i) {
            ret += sep + data[i].ToString("x2").ToUpper();
            sep = " ";
        }
        return ret;
    }
    
    private static string MD5sum(string Value)
    {
        MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
        byte[] data = Encoding.ASCII.GetBytes(Value);
        data = x.ComputeHash(data);
        StringBuilder ret = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
            ret.Append(data[i].ToString("x2").ToLower());
        return ret.ToString();
    }
    
    
    private static void InsertString(byte[] packet, int i, string str)
    {
        str = (str + new string(' ', 64)).Substring(0, 64);
        Array.Copy(Encoding.ASCII.GetBytes(str), 0, packet, i, 64);
    }
    private static string ExtractString(byte[] packet, int i)
    {
        return Encoding.ASCII.GetString(packet, i, 64).TrimEnd();
    }
    
    public static void MsgAll(string msg) {
        MinecraftServer.theServ.SendAll(PacketMessage(msg));
    }
    
    // ===================================================================
    // nonstatic helpers
    
    private void _Disconnect()
    {
        if(_connected) {
            Spacecraft.Log(name + " disconnected");
            _connected = false;
            if(_player != null) {
                MsgAll(Color.Yellow + name + " has quit.");
                serv.SendAll(PacketDespawnPlayer(_player));
            }
        }
    }
    
    public void Send(byte[] data)
    {
        try {
            client.GetStream().Write(data, 0, data.Length);
        }
        catch(InvalidOperationException) {
            _Disconnect();
        }
        catch(IOException) {
            _Disconnect();
        }
    }
    
    private void SendMap()
    {
        byte[] levelInit = new byte[] { 0x02 };
        Send(levelInit);
        
        using(MemoryStream memstr = new MemoryStream()) {
            using(GZipStream compress = new GZipStream(memstr, CompressionMode.Compress)) {
                compress.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(serv.map.Length)), 0, sizeof(int));
                compress.Write(serv.map.data, 0, serv.map.Length);
                memstr.Seek(0, SeekOrigin.Begin);
                
				int len = serv.map.Length;
                for(int i = 0; i < len; i += 1024) {
                    byte[] packet = new byte[1028];
                    packet[0] = 0x03;
                    
                    int remaining = len - i;
                    if(remaining > 1024) remaining = 1024;
                    byte[] lenb = BitConverter.GetBytes(Host2Net((short)remaining));
                    
                    packet[1] = lenb[0];
                    packet[2] = lenb[1];
                    memstr.Read(packet, 3, remaining);
                    
                    packet[1027] = (byte)(100.0 * i / len);
                    Send(packet);
                }
            }
        }
        
        byte[] xdim = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(serv.map.xdim));
        byte[] ydim = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(serv.map.ydim));
        byte[] zdim = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(serv.map.zdim));
        
        byte[] levelFin = new byte[PacketLen.LevelFinish];
        levelFin[0] = Packet.LevelFinish;
        levelFin[1] = xdim[0]; levelFin[2] = xdim[1];
        levelFin[3] = ydim[0]; levelFin[4] = ydim[1];
        levelFin[5] = zdim[0]; levelFin[6] = zdim[1];
        Send(levelFin);
    }
    
    private void SendServerInfo() {
        byte[] packet = new byte[131];
        packet[0] = 0x00;
        packet[1] = 0x07; // protocol version
        InsertString(packet, 2, serv.name);
        InsertString(packet, 66, serv.motd);
        packet[130] = 0x00;
        if(_player.rank >= Rank.Mod) {
            packet[130] = 0x64;
        }
        Send(packet);
    }
    
    public void Kick(string reason) {
        Spacecraft.Log(name + " was kicked: " + reason);
        MsgAll(Color.Red + name + " was kicked!");
        _connected = false;
        Send(PacketKick(reason));
    }
    
    public void Message(string msg) {
        Send(PacketMessage(msg));
    }
    
    // ===================================================================
    // nonstatic Handle* functions
    
    private void HandleJoin(byte[] packet)
    {
        byte protocol = packet[1];
        if(protocol != 0x07) {
            Spacecraft.Log("Warning: " + name + " (" + addr + ") had a protcol version of 0x" + String.Format("{0:X}", protocol));
            Kick("Wrong protocol version 0x" + String.Format("{0:X}", protocol) + ": contact server admin");
            return;
        }
        string username = Encoding.ASCII.GetString(packet, 2, 64).Trim();
        string key = Encoding.ASCII.GetString(packet, 66, 64).Trim();
        byte unused = packet[130];
        _name = username;
        
        if(Config.GetBool("verify-names", true) && MD5sum(serv.salt + username) != key) {
            Spacecraft.Log(name + " (" + addr + ") wasn't verified");
            Kick("The name wasn't verified by minecraft.net!");
            return;
        }
		
        _player = new Player(username);
        
        if(_player.rank == Rank.Banned) {
            Spacecraft.Log(name + " (" + addr + ") tried to connect, but is banned.");
            Kick("Sorry, you're not allowed on this server!");
            return;
        }
        
        Spacecraft.Log(name + " (" + addr + ") has joined! pid = " + player.pid);
        SendServerInfo();
        SendMap();
        Thread.Sleep(100);
        serv.SendAllExcept(PacketSpawnPlayer(_player), this);
        ArrayList players = serv.GetAllPlayers();
        for(int i = 0; i < players.Count; ++i) {
            Player p = (Player)(players[i]);
            if(p == _player || p == null) continue;
            Send(PacketSpawnPlayer((Player)(players[i])));
        }
        Send(PacketTeleportSelf(serv.map.spawn.x, serv.map.spawn.y, serv.map.spawn.z, serv.map.spawnHeading, 0));
        Send(PacketSpawnSelf(serv.map.spawn.x, serv.map.spawn.y, serv.map.spawn.z, serv.map.spawnHeading, 0));
		MsgAll(Color.Yellow + name + " has joined!");
        Message(Color.Yellow + "You're a " + Player.RankColor(_player.rank) + player.rank.ToString() + Color.Yellow + ". See /help for info");
    }
    
    private void HandleBlock(byte[] packet)
    {
        short x = Net2Host(BitConverter.ToInt16(packet, 1));
        short y = Net2Host(BitConverter.ToInt16(packet, 3));
        short z = Net2Host(BitConverter.ToInt16(packet, 5));
        byte mode = packet[7];
        byte type = packet[8];
        
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(serv.map.Length));
        
        if(mode == 0x01) { // place
            if(type == Block.Obsidian && _player.placing) {
                type = _player.placeType;
            }
            if(type == Block.Stair && y > 0 && serv.map.GetTile(x, (short)(y-1), z) == Block.Stair) {
                Send(PacketSetBlock(x, y, z, Block.Air));
                type = Block.DoubleStair;
                y = (short)(y-1);
            }
            serv.map.SetTile(x, y, z, type);
            serv.SendAll(PacketSetBlock(x, y, z, type));
        } else { // delete
            serv.map.SetTile(x, y, z, Block.Air);
            serv.SendAll(PacketSetBlock(x, y, z, Block.Air));
        }
    }
    
    private void HandlePosition(byte[] packet)
    {
        byte pid = packet[1]; // always 255
        short x = Net2Host(BitConverter.ToInt16(packet, 2));
        short y = Net2Host(BitConverter.ToInt16(packet, 4));
        short z = Net2Host(BitConverter.ToInt16(packet, 6));
        byte heading = packet[8];
        byte pitch = packet[9];
        if(player.PositionUpdate(x, y, z, heading, pitch)) {
            serv.SendAll(PacketPositionUpdate(player));
        }
    }
        
    
    private void HandleMessage(byte[] packet)
    {
        string msg = ExtractString(packet, 2);
		if(msg[0] == '@') {
			// private messages
			int i = msg.IndexOf(' ');
			string username = msg.Substring(1);
			string message = "";
			if(i > 0) {
				username = msg.Substring(1, i - 1);
				if(i != msg.Length - 1) {
					message = msg.Substring(i + 1);
				}
			}
			if(message != "") {
				Connection c = MinecraftServer.theServ.GetConnection(username);
				if(c == null) {
					Message(Color.DarkRed + "No such user " + username);
				} else {
					Message(Color.Purple + ">" + username + "> " + message);
					c.Message(Color.Purple + "[" + name + "] " + message);
				}
			}
		} else if(msg[0] == '/') {
            int i = msg.IndexOf(' ');
            string cmd = msg.Substring(1);
            string args = "";
            if(i > 0) {
                cmd = msg.Substring(1, i - 1);
                if(i != msg.Length-1) {
                    args = msg.Substring(i + 1);
                }
            }
            ChatCommandHandling.Execute(this, cmd, args);
        } else {
            MsgAll(name + ": " + msg);
            Spacecraft.Log(name + ": " + msg);
        }
    }
	
	public void ResendMap() {
        SendServerInfo();
        SendMap();
	}
    
    // ===================================================================
    // static Packet* functions
    
    public static byte[] PacketSetBlock(short x, short y, short z, byte block)
    {
        byte[] packet = new byte[PacketLen.ServerSetBlock];
        packet[0] = Packet.ServerSetBlock;
        byte[] bytex = BitConverter.GetBytes(Host2Net(x));
        byte[] bytey = BitConverter.GetBytes(Host2Net(y));
        byte[] bytez = BitConverter.GetBytes(Host2Net(z));
        packet[1] = bytex[0]; packet[2] = bytex[1];
        packet[3] = bytey[0]; packet[4] = bytey[1];
        packet[5] = bytez[0]; packet[6] = bytez[1];
        packet[7] = block;
        return packet;
    }
    
    public static byte[] PacketPositionUpdate(Player player)
    {
        byte[] packet = new byte[PacketLen.PositionUpdate];
        packet[0] = Packet.PositionUpdate;
        byte[] bytex = BitConverter.GetBytes(Host2Net(player.x));
        byte[] bytey = BitConverter.GetBytes(Host2Net(player.y));
        byte[] bytez = BitConverter.GetBytes(Host2Net(player.z));
        packet[1] = player.pid;
        packet[2] = bytex[0]; packet[3] = bytex[1];
        packet[4] = bytey[0]; packet[5] = bytey[1];
        packet[6] = bytez[0]; packet[7] = bytez[1];
        packet[8] = player.heading;
        packet[9] = player.pitch;
        return packet;
    }
    
    public static byte[] PacketTeleportSelf(short x, short y, short z, byte heading, byte pitch)
    {
        byte[] packet = new byte[PacketLen.PositionUpdate];
        packet[0] = Packet.PositionUpdate;
        byte[] bytex = BitConverter.GetBytes(Host2Net(x));
        byte[] bytey = BitConverter.GetBytes(Host2Net(y));
        byte[] bytez = BitConverter.GetBytes(Host2Net(z));
        packet[1] = 255;
        packet[2] = bytex[0]; packet[3] = bytex[1];
        packet[4] = bytey[0]; packet[5] = bytey[1];
        packet[6] = bytez[0]; packet[7] = bytez[1];
        packet[8] = heading;
        packet[9] = pitch;
        return packet;
    }
    
    public static byte[] PacketSpawnPlayer(Player player)
    {
        byte[] packet = new byte[PacketLen.SpawnPlayer];
        packet[0] = Packet.SpawnPlayer;
        packet[1] = player.pid;
        InsertString(packet, 2, player.name);
        byte[] bytex = BitConverter.GetBytes(Host2Net(player.x));
        byte[] bytey = BitConverter.GetBytes(Host2Net(player.y));
        byte[] bytez = BitConverter.GetBytes(Host2Net(player.z));
        packet[66] = bytex[0]; packet[67] = bytex[1];
        packet[68] = bytey[0]; packet[69] = bytey[1];
        packet[70] = bytez[0]; packet[71] = bytez[1];
        packet[72] = player.heading;
        packet[73] = player.pitch;
        return packet;
    }
    
    public static byte[] PacketSpawnSelf(short x, short y, short z, byte heading, byte pitch)
    {
        byte[] packet = new byte[PacketLen.SpawnPlayer];
        packet[0] = Packet.SpawnPlayer;
        packet[1] = 255;
        InsertString(packet, 2, "MYNAME");
        byte[] bytex = BitConverter.GetBytes(Host2Net(x));
        byte[] bytey = BitConverter.GetBytes(Host2Net(y));
        byte[] bytez = BitConverter.GetBytes(Host2Net(z));
        packet[66] = bytex[0]; packet[67] = bytex[1];
        packet[68] = bytey[0]; packet[69] = bytey[1];
        packet[70] = bytez[0]; packet[71] = bytez[1];
        packet[72] = heading;
        packet[73] = pitch;
        return packet;
    }
    
    public static byte[] PacketDespawnPlayer(Player player)
    {
        byte[] packet = new byte[PacketLen.DespawnPlayer];
        packet[0] = Packet.DespawnPlayer;
        packet[1] = player.pid;
        return packet;
    }
    
    public static byte[] PacketMessage(string msg)
    {
        byte[] packet = new byte[66];
        packet[0] = 0x0d;
        packet[1] = 0;
        InsertString(packet, 2, msg);
        return packet;
    }
    
    public static byte[] PacketKick(string reason)
    {
        byte[] packet = new byte[65];
        packet[0] = 0x0e;
        InsertString(packet, 1, reason);
        return packet;
    }
}
