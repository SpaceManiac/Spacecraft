using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace spacecraft
{
    class NewConnection
    {
        delegate void AuthenticationHandler(bool sucess);
        event AuthenticationHandler Authenticated;

        string username;
        byte[] verificationHash = new byte[1024];



        public NewConnection(TcpClient c) {
            
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
