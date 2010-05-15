using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace spacecraft
{
    class NewConnection
    {
        static byte PROTOCOL_VERSION = 0x07;

        public delegate void PlayerSpawnHandler(string username);
        public event PlayerSpawnHandler PlayerSpawn;

        public delegate void AuthenticationHandler(bool sucess);
        public event AuthenticationHandler Authenticated;

        public delegate void UsernameHandler(string username);
        public event UsernameHandler ReceivedUsername;

        public delegate void MessageHandler(string msg);
        public event MessageHandler ReceivedMessage;

        public delegate void DisconnectHandler();
        public event DisconnectHandler Disconnect;

        bool Connected = true;

        TcpClient _client;
     
        public NewConnection(TcpClient c) 
        {
            _client = c;
            while (Connected)
                HandlePacket();
        }

        void HandlePacket()
        {
            ClientPacket IncomingPacket = ReceivePacket();

            switch (IncomingPacket.PacketID)
            {
                case (byte) Packet.PacketType.Message:
                    HandleMessage((MessagePacket) IncomingPacket);
                    break;
                case (byte) Packet.PacketType.PlayerSetBlock:
                    HandleBlockSet((BlockUpdatePacket)IncomingPacket);
                    break;
                case (byte) Packet.PacketType.PositionUpdate:
                    HandlePositionUpdate((PositionUpdatePacket)IncomingPacket);
                    break;
                case (byte) Packet.PacketType.SpawnPlayer:
                    HandlePlayerSpawn((PlayerIDPacket)IncomingPacket);
                    break;
                default:
                    Spacecraft.LogError("Incoming packet does not match any known packet type!");
                    break;
            }
        }

        private void HandlePositionUpdate(PositionUpdatePacket positionUpdatePacket)
        {
            throw new NotImplementedException();
        }

        private void HandleBlockSet(BlockUpdatePacket blockUpdatePacket)
        {
            throw new NotImplementedException();
        }

        private void HandleMessage(MessagePacket messagePacket)
        {
            throw new NotImplementedException();
        }

        private void HandlePlayerSpawn(PlayerIDPacket IncomingPacket)
        {
            if (IncomingPacket.Version != PROTOCOL_VERSION)
            {
                SendKick("Wrong protocol version.");
            }
            bool success = IsHashCorrect(IncomingPacket.Username.ToString().Trim(), IncomingPacket.Key.ToString().Trim());

            if (Authenticated != null)
                Authenticated(success);


            if (PlayerSpawn != null)
            {
                PlayerSpawn(IncomingPacket.Username.ToString().Trim());
            }

            // Send response packet.
            PlayerInPacket outPacket = new PlayerInPacket();
            
            string motd = MinecraftServer.theServ.motd.PadRight(64, (char)20);
            outPacket.MOTD = Encoding.ASCII.GetBytes(motd);
            string name = MinecraftServer.theServ.name.PadRight(64, (char)20);
            outPacket.Name = Encoding.ASCII.GetBytes(name);
            outPacket.Version = PROTOCOL_VERSION;


            SendPacket(outPacket);
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
                    Spacecraft.LogError("Something went wrong while we were reading a packet!\n" +  e.Message);
                }
            }
            while (false && buffsize < 1300);

            return null;
        }


        private void SendPacket(ServerPacket packet)
        {
            try
            {
                byte[] bytes = packet;
                _client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch (IOException e)
            {
                Quit();
            }
            catch (InvalidOperationException)
            {
                Quit();
            }
        }

        private void Quit()
        {
            if (Disconnect != null)
                Disconnect();
        }


        private void Authenticate()
        {
            bool authorized = false;
			

            if (Authenticated != null)
                Authenticated(authorized);
        }

        private bool IsHashCorrect(string name, string hash)
        {
            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();

            string salt = MinecraftServer.theServ.salt.ToString();
            string combined = salt + name;
            Byte[] combinedBytes = Encoding.ASCII.GetBytes(combined);
            string properHash = provider.ComputeHash(combinedBytes).ToString();


            return (hash == properHash);
        }



        public void DisplayMessage(string msg)
        {

        }

        public void SendKick(string reason)
        {

            if (Disconnect != null)
                Disconnect();
        }

    }
}