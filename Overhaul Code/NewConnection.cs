using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace spacecraft
{
    class NewConnection
    {
        static int PROTOCOL_VERSION = 0x07;

        public delegate void AuthenticationHandler(bool sucess);
        public event AuthenticationHandler Authenticated;

        public delegate void UsernameHandler(string username);
        public event UsernameHandler ReceivedUsername;

        public delegate void MessageHandler(string msg);
        public event MessageHandler ReceivedMessage;

        public delegate void DisconnectHandler();
        public event DisconnectHandler Disconnect;



        string _username;
        byte[] _verificationHash = new byte[64];

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
                    HandleBlockSet(IncomingPacket);
                    break;
                case (byte) Packet.PacketType.PositionUpdate:
                    HandlePositionUpdate(IncomingPacket);
                    break;
                case (byte) Packet.PacketType.SpawnPlayer:
                    HandlePlayerSpawn((PlayerIDPacket)IncomingPacket);
                    break;
                default:
                    Spacecraft.LogError("Incoming packet does not match any known packet type!");
                    break;
            }
        }

        private void HandlePlayerSpawn(PlayerIDPacket IncomingPacket)
        {
            if (IncomingPacket.Version != PROTOCOL_VERSION)
            {
                SendKick("Wrong protocol version.");
            }
        }

        private void HandlePositionUpdate(ClientPacket IncomingPacket)
        {
            throw new NotImplementedException();
        }

        private void HandleBlockSet(ClientPacket IncomingPacket)
        {
            throw new NotImplementedException();
        }

        private void HandleMessage(MessagePacket IncomingPacket)
        {
            throw new NotImplementedException();
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
            Byte[] combinedBytes = new Byte[combined.Length];
            for (int i = 0; i < combined.Length; i++)
            {
                combinedBytes[i] = (Byte) combined[i];
            }
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