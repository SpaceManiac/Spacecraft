using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

class NewConnection
{
    delegate void AuthenticationHandler(bool success);
    public event AuthenticationHandler Authenticated;

    string _username;
    byte[] _verificationHash = new byte[64];
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
            int bytesRead = _client.GetStream().Read(buffer, buffsize, 1024-buffsize);
			buffsize += bytesRead;
        }
        while (buffsize == 0 || buffsize < PacketLengthInfo.Lookup((PacketType) buffer[0]));



        return new byte[] { };
    }

    private void Authenticate()
    {
        bool authorized = false;
        
		

        if (Authenticated != null)
            Authenticated(authorized);
    }

    public void DisplayMessage(string msg)
    {
		// ...
    }
}