using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace spacecraft
{
    class NewConnection
    {
        static byte PROTOCOL_VERSION = 0x07;

        public delegate void PlayerSpawnHandler();
        public event PlayerSpawnHandler PlayerSpawn;

        public delegate void PlayerMoveHandler(Position dest, byte heading, byte pitch);
        public event PlayerMoveHandler PlayerMove;

        public delegate void UsernameHandler(string username);
        public event UsernameHandler ReceivedUsername;

        public delegate void MessageHandler(string msg);
        public event MessageHandler ReceivedMessage;

        public delegate void DisconnectHandler();
        public event DisconnectHandler Disconnect;

        bool Connected = true;
        Queue<ServerPacket> SendQueue; // Packets that are queued to be sent to the client.


        TcpClient _client;

        public NewConnection(TcpClient c)
        {
            SendQueue = new Queue<ServerPacket>();

            _client = c;

            Thread T = new Thread(Start);
            T.Start();
        }

        void Start()
        {
            while (Connected)
            {
                while (SendQueue.Count > 0)
                {
                    TransmitPacket(SendQueue.Dequeue());
                }
                HandleIncomingPacket();
            }

        }

        void HandleIncomingPacket()
        {
            ClientPacket IncomingPacket = ReceivePacket();

            switch (IncomingPacket.PacketID)
            {
                case (byte)Packet.PacketType.Ident:
                    HandlePlayerSpawn((PlayerIDPacket)IncomingPacket);
                    break;

                case (byte)Packet.PacketType.Message:
                    HandleMessage((ClientMessagePacket)IncomingPacket);
                    break;
                case (byte)Packet.PacketType.PlayerSetBlock:
                    HandleBlockSet((BlockUpdatePacket)IncomingPacket);
                    break;
                case (byte)Packet.PacketType.PositionUpdate:
                    HandlePositionUpdate((PositionUpdatePacket)IncomingPacket);
                    break;
                case (byte)Packet.PacketType.SpawnPlayer:
                    HandlePlayerSpawn((PlayerIDPacket)IncomingPacket);
                    break;
                default:
                    Spacecraft.LogError("Incoming packet does not match any known packet type!");
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
            throw new NotImplementedException();
        }

        private void HandleMessage(ClientMessagePacket messagePacket)
        {
            if (ReceivedMessage != null)
                ReceivedMessage(messagePacket.Message.ToString());
        }

        private void HandlePlayerSpawn(PlayerIDPacket IncomingPacket)
        {
            if (IncomingPacket.Version != PROTOCOL_VERSION)
            {
                SendKick("Wrong protocol version.");
            }
            bool success = IsHashCorrect(IncomingPacket.Username.ToString(), IncomingPacket.Key.ToString());

            if (ReceivedUsername != null)
                ReceivedUsername(IncomingPacket.Username.ToString());
            

            // Send response packet.
            ServerIdentPacket Ident = new ServerIdentPacket();

            Ident.MOTD = NewServer.theServ.motd;
            Ident.Name = NewServer.theServ.name;
            Ident.Version = PROTOCOL_VERSION;

            TransmitPacket(Ident);

            SendMap();

            if (PlayerSpawn != null)
            {
                PlayerSpawn();
            }
        }

        private void SendMap()
        {
            Map M = NewServer.theServ.map;
            SendPacket(new LevelInitPacket());

            byte[] compressedData; 
            using (MemoryStream memstr = new MemoryStream())
            {
                M.GetCompressedCopy(memstr, true);
                compressedData = memstr.ToArray();
            }

            int bytesSent = 0;
            while (compressedData.Length > bytesSent) //  While we still have data to transmit.
            {
                LevelChunkPacket P = new LevelChunkPacket(); // New packet.


                byte[] Chunk = new byte[NetworkByteArray.Size];
                        

                int remaining = compressedData.Length - bytesSent;
                remaining = Math.Min(remaining, NetworkByteArray.Size);

                Array.Copy(compressedData, bytesSent, Chunk, 0, remaining);
                bytesSent += remaining;
 
                P.ChunkData = new NetworkByteArray(Chunk);
                P.ChunkLength = (short) (Math.Min(NetworkByteArray.Size, compressedData.Length - bytesSent));
                P.PercentComplete = (byte)(100 * (bytesSent / compressedData.Length));

                SendPacket(P);
            }

            LevelEndPacket End = new LevelEndPacket();
            End.X = M.xdim;
            End.Y = M.ydim;
            End.Z = M.zdim;
            SendPacket(End);
        }


        private ClientPacket ReceivePacket()
        {
            byte[] buffer = new byte[2048]; // No packet is 2048 bytes long, so we shouldn't ever overflow.
            int buffsize = 0;

            do
            {
                try
                {
                    int bytesRead = _client.GetStream().Read(buffer, buffsize, buffer.Length - buffsize);
                    buffsize += bytesRead;
                }
                catch (Exception e)
                {
                    Spacecraft.LogError("Something went wrong while we were reading a packet!\n" + e.Message);
                }
            }
            while ((Packet.PacketFromLength(buffsize) == Packet.PacketType.UNKNOWN) && buffsize < buffer.Length - 100);

            ClientPacket P = ClientPacket.FromByteArray(buffer);
            
            return P;
        }


        private void TransmitPacket(ServerPacket packet)
        {
            try
            {
                byte[] bytes = packet;
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

        private void SendPacket(ServerPacket P)
        {
            SendQueue.Enqueue(P);
        }

        private void Quit()
        {
            _client.Close();
            Connected = false;
            if (Disconnect != null)
                Disconnect();
        }

        private bool IsHashCorrect(string name, string hash)
        {
            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();

            string salt = NewServer.theServ.salt.ToString(); //MinecraftServer.theServ.salt.ToString();
            string combined = salt + name;
            Byte[] combinedBytes = Encoding.ASCII.GetBytes(combined);
            string properHash = provider.ComputeHash(combinedBytes).ToString();


            return (hash == properHash);
        }



        public void DisplayMessage(string msg)
        {
            ServerMessagePacket P = new ServerMessagePacket();
            P.Message = msg;
            SendPacket(P);
        }

        public void SendKick(string reason)
        {
            DisconnectPacket P = new DisconnectPacket();
            P.Reason = reason;
            SendPacket(P);

            if (Disconnect != null)
                Disconnect();
        }


        public void SendPlayerMovement(NewPlayer player, Position dest, bool self)
        {
            PlayerMovePacket packet = new PlayerMovePacket();
            packet.PlayerID = player.playerID;
            if (self)
                packet.PlayerID = 255;
            packet.X = player.pos.x;
            packet.Y = player.pos.y;
            packet.Z = player.pos.z;

            SendPacket(packet);
        }

        public void HandlePlayerSpawn(NewPlayer Player)
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