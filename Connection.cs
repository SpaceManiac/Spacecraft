using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace spacecraft
{
	class Connection
	{
		static byte PROTOCOL_VERSION = 0x07;

		public delegate void UsernameHandler(string username);
		public event UsernameHandler ReceivedUsername;

		public delegate void PlayerSpawnHandler();
		public event PlayerSpawnHandler PlayerSpawn;

		public delegate void PlayerMoveHandler(Position dest, byte heading, byte pitch);
		public event PlayerMoveHandler PlayerMove;

		public delegate void BlockSetHandler(short X, short Y, short Z, byte Mode, byte Type);
		public event BlockSetHandler BlockSet;

		public delegate void MessageHandler(string msg);
		public event MessageHandler ReceivedMessage;

		public delegate void DisconnectHandler();
		public event DisconnectHandler Disconnect;

		bool Joined = false;
		bool Connected = true;
		private object SendQueueMutex = new object();
		Queue<ServerPacket> SendQueue; // Packets that are queued to be sent to the client.
		
		public string ipaddr = "";
		
		Player player;

		TcpClient _client;

		public Connection(TcpClient c, Player P)
		{
			this.player = P;
			
			SendQueue = new Queue<ServerPacket>();

			_client = c;
			ipaddr = "127.0.0.1:tty";
			if(c != null) {
				ipaddr = c.Client.RemoteEndPoint.ToString();
			}
		}

		public void Start() {
			Thread T = new Thread(ReadThread, Spacecraft.StackSize);
			T.Name = _client.GetHashCode().ToString() + " Read";
			T.Start();

			Thread T2 = new Thread(WriteThread, Spacecraft.StackSize);
			T2.Name = _client.GetHashCode().ToString() + " Write";
			T2.Start();
		}

		void ReadThread() {
			while (Connected) {
				HandleIncomingPacket();
				Thread.Sleep(10);
			}
		}

		void WriteThread() {
			while (Connected) {
				while (SendQueue.Count > 0) {
					lock(SendQueueMutex) {
				   	 	TransmitPacket(SendQueue.Dequeue());
				   	 }
				}
				Thread.Sleep(10);
			}
		}

		void HandleIncomingPacket() {
			ClientPacket IncomingPacket = ReceivePacket();
			if(IncomingPacket == null) return;

			switch (IncomingPacket.PacketID)
			{
				case (byte)PacketType.Ident:
					HandlePlayerIdent((PlayerIDPacket)IncomingPacket);
					break;

				case (byte)PacketType.Message:
					HandleMessage((ClientMessagePacket)IncomingPacket);
					break;
				case (byte)PacketType.PlayerSetBlock:
					HandleBlockSet((BlockUpdatePacket)IncomingPacket);
					break;
				case (byte)PacketType.PositionUpdate:
					HandlePositionUpdate((PositionUpdatePacket)IncomingPacket);
					break;
				default:
					try {
						throw new SpacecraftException("Incoming packet type of " + IncomingPacket.PacketID + " does not match any known type");
					}
					catch(SpacecraftException e) {
						Spacecraft.LogError("Error while reading a packet", e);
					}
					break;
			}
		}

		private void HandlePositionUpdate(PositionUpdatePacket positionUpdatePacket)
		{

			Position pos = new Position(positionUpdatePacket.X, positionUpdatePacket.Y, positionUpdatePacket.Z);

			byte heading = positionUpdatePacket.Heading;
			byte pitch = positionUpdatePacket.Pitch;

			if (PlayerMove != null)
				PlayerMove(pos, heading, pitch);

		}

		private void HandleBlockSet(BlockUpdatePacket blockUpdatePacket)
		{
			if (BlockSet != null)
				BlockSet(blockUpdatePacket.X, blockUpdatePacket.Y, blockUpdatePacket.Z, blockUpdatePacket.Mode, blockUpdatePacket.Type);
		}

		private void HandleMessage(ClientMessagePacket messagePacket)
		{
			if (messagePacket.Message.ToString() != "" && ReceivedMessage != null)
				ReceivedMessage(messagePacket.Message.ToString());
		}

		private void HandlePlayerIdent(PlayerIDPacket IncomingPacket)
		{
			if (Joined) {
				SendKick("You identified twice!");
				return;
			}

			if (IncomingPacket.Version != PROTOCOL_VERSION) {
				Spacecraft.Log("Hmm, got a protocol version of " + IncomingPacket.Version + " from /" + ipaddr + "(" + IncomingPacket.Username.ToString() + ")");
				SendKick("Wrong protocol version!");
				Server.theServ.RemovePlayer(player);
				return;
			}
			bool success = IsHashCorrect(IncomingPacket.Username.ToString(), IncomingPacket.Key.ToString());

			if (Config.GetBool("verify-names", true) && !success) {
				Spacecraft.Log("/" + ipaddr + " (" + IncomingPacket.Username.ToString() + ") attempted to join, but didn't verify");
				SendKick("Your name wasn't verified by minecraft.net!");
				Server.theServ.RemovePlayer(player);
				return;
		   	}

		   	Joined = true;
		   	
		   	string username = IncomingPacket.Username.ToString().Trim();

			if (ReceivedUsername != null)
				ReceivedUsername(username);

			Spawn(username);
		}
		
		public void Spawn(string username)
		{
			// Send response packet.
			ServerIdentPacket Ident = new ServerIdentPacket();
			Ident.MOTD = Server.theServ.motd;
			Ident.Name = Server.theServ.name;
			Ident.Type = (byte)(RankInfo.IsOperator(Player.RankOf(username)) ? 0x64 : 0x00);
			Ident.Version = PROTOCOL_VERSION;
			TransmitPacket(Ident);

			SendMap();

			if (PlayerSpawn != null) {
				PlayerSpawn();
			}
		}

		private void SendMap()
		{
			Map M = Server.theServ.map;
			SendPacket(new LevelInitPacket());

			byte[] compressedData;
			using (MemoryStream memstr = new MemoryStream())
			{
				M.GetCompressedCopy(memstr, true);
				compressedData = memstr.ToArray();
			}

			int bytesSent = 0;
			while (bytesSent < compressedData.Length) //  While we still have data to transmit.
			{
				LevelChunkPacket P = new LevelChunkPacket(); // New packet.

				byte[] Chunk = new byte[1024];

				int remaining = compressedData.Length - bytesSent;
				remaining = Math.Min(remaining, 1024);

				Array.Copy(compressedData, bytesSent, Chunk, 0, remaining);
				bytesSent += remaining;

				P.ChunkData = Chunk;
				P.ChunkLength = (short) remaining;
				P.PercentComplete = (byte) (100 * ((double)bytesSent / compressedData.Length));

				SendPacket(P);
				
				// yield to other threads
				// in case it takes someone a loooong time to load
				Thread.Sleep(10);
			}

			LevelEndPacket End = new LevelEndPacket();
			End.X = M.xdim;
			End.Y = M.ydim;
			End.Z = M.zdim;
			SendPacket(End);
		}

		byte[] buffer = new byte[2048]; // No packet is 2048 bytes long, so we shouldn't ever overflow.
		int buffsize = 0;

		private ClientPacket ReceivePacket() {
			do
			{
				try
				{
					int bytesRead = _client.GetStream().Read(buffer, buffsize, Math.Min(10, buffer.Length - buffsize));
					if(bytesRead == 0) {
						Quit();
						return null;
					}
					buffsize += bytesRead;
				}
				catch (IOException)
				{
					// they probably just disconnected
					Quit();
					return null;
				}
				catch (InvalidOperationException)
				{
					Quit();
					return null;
				}
			}
			while (buffsize == 0 || buffsize < PacketLengthInfo.Lookup((PacketType)(buffer[0])));

			ClientPacket P = ClientPacket.FromByteArray(buffer);

			int len = PacketLengthInfo.Lookup((PacketType)(buffer[0]));
			buffsize -= len;
			byte[] newbuf = new byte[2048];
			Array.Copy(buffer, len, newbuf, 0, buffsize);
			buffer = newbuf;

			return P;
		}

		private void TransmitPacket(ServerPacket packet)
		{
			try
			{
				byte[] bytes = packet.ToByteArray();
				_client.GetStream().Write(bytes, 0, bytes.Length);
			}
			catch (IOException)
			{
				Quit();
			}
			catch (InvalidOperationException)
			{
				Quit();
			}
		}

		/// <summary>
		/// Queue a packet for sending.
		/// </summary>
		/// <param name="P">The packet to queue.</param>
		private void SendPacket(ServerPacket P)
		{
			if(P == null) {
				throw new Exception("Tried to SendPacket(null)");
			}
			lock(SendQueueMutex) {
				SendQueue.Enqueue(P);
			}
		}

		private void Quit()
		{
			_client.Close();
			if (!Connected) return;
			Connected = false;
			if (!Joined) return;
			if (Disconnect != null)
				Disconnect();
		}

		private bool IsHashCorrect(string name, string hash)
		{
			// Minecraft is ridiculous!
			while(hash.Length < 32) {
				hash = "0" + hash;
			}
			
			string salt = Server.theServ.salt.ToString();
			string combined = salt + name;
			string properHash = Spacecraft.MD5sum(combined);

			return (hash == properHash);
		}

		public void DisplayMessage(string msg)
		{
			ServerMessagePacket P = new ServerMessagePacket();
			P.Message = msg;
			SendPacket(P);
		}

		public void SendBlockSet(short x, short y, short z, byte type)
		{
			SetBlockPacket P = new SetBlockPacket();
			P.X = x;
			P.Y = y;
			P.Z = z;
			P.Type = type;
			SendPacket(P);
		}
		
		public void SendOperator(bool isOperator)
		{
			RankUpdatePacket P = new RankUpdatePacket();
			P.UserType = (byte)(isOperator ? 0x64 : 0x00);
			SendPacket(P);
		}

		public void SendKick(string reason)
		{
			DisconnectPacket P = new DisconnectPacket();
			P.Reason = reason;
			TransmitPacket(P); // Send the packet immediatly, to free up bandwidth.
			Quit();
		}

		public void SendPlayerMovement(Player player, Position dest, byte heading, byte pitch, bool self)
		{
			PlayerMovePacket packet = new PlayerMovePacket();
			packet.PlayerID = player.playerID;
			if (self)
				packet.PlayerID = 255;
			packet.X = player.pos.x;
			packet.Y = player.pos.y;
			packet.Z = player.pos.z;
			packet.Heading = heading;
			packet.Pitch = pitch;

			SendPacket(packet);
		}

		public void SendPlayerMovement(Robot player, Position dest, byte heading, byte pitch, bool self)
		{
			PlayerMovePacket packet = new PlayerMovePacket();
			packet.PlayerID = player.playerID;
			if (self)
				packet.PlayerID = 255;
			packet.X = player.pos.x;
			packet.Y = player.pos.y;
			packet.Z = player.pos.z;
			packet.Heading = heading;
			packet.Pitch = pitch;

			SendPacket(packet);
		}

		internal void SendPlayerDisconnect(byte ID)
		{
			DespawnPacket packet = new DespawnPacket();
			packet.PlayerID = ID;
			SendPacket(packet);
		}

		public void HandlePlayerSpawn(Player Player, bool self)
		{
			PlayerSpawnPacket packet = new PlayerSpawnPacket();
			packet.PlayerID = Player.playerID;
			if(self)
				packet.PlayerID = 255;
			packet.Name = Player.name;
			packet.X = Player.pos.x;
			packet.Y = Player.pos.y;
			packet.Z = Player.pos.z;
			packet.Heading = Player.heading;
			packet.Pitch = Player.pitch;
			SendPacket(packet);
		}

		public void HandlePlayerSpawn(Robot Player)
		{
			PlayerSpawnPacket packet = new PlayerSpawnPacket();
			packet.PlayerID = Player.playerID;
			packet.Name = Player.name;
			packet.X = Player.pos.x;
			packet.Y = Player.pos.y;
			packet.Z = Player.pos.z;
			packet.Heading = Player.heading;
			packet.Pitch = Player.pitch;
			SendPacket(packet);
		}
	}
}