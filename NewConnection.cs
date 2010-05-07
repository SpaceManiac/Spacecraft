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
        byte[] _verificationHash = new byte[1024];
        TcpClient _client;
     
        public NewConnection(TcpClient c) {
            _client = c;
        }

        private byte[] ReceivePacket()
        {
            byte[] buffer = new byte[1024]; // No packet is 2048 bytes long, so we shouldn't ever overflow.
            int buffsize = 0;
            do
            {
                int bytesRead = _client.GetStream().Read(buffer, 0, 1024);
            }
            while (PacketLen.Lookup(buffer[0]) == 0);



            return new byte[] { };
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


    }
}
