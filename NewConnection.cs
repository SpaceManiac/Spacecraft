using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace spacecraft
{
    class NewConnection
    {
        delegate void AuthenticationHandler(bool sucess);
        event AuthenticationHandler Authenticated;

        string _username;
        byte[] _verificationHash = new byte[64];
        TcpClient _client;
     
        public NewConnection(TcpClient c) {
            _client = c;
        }

        private Packet ReceivePacket()
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
                    Spacecraft.LogError("Something went wrong while we were reading a packet!");
                }
            }
            while (false && buffsize < 1300);
            
            

            return null;
        }


        private void Authenticate()
        {
            bool authorized = false;
            throw new NotImplementedException();

            if (Authenticated != null)
                Authenticated(authorized);
        }

        public void DisplayMessage(string msg)
        {

        }

        public void SendKick(string reason)
        {

        }

    }
}