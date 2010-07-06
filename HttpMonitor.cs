using System;
using System.Threading;
using System.Net;
using System.Text;
using System.IO;

namespace spacecraft {
	static class HttpMonitor {
		private static bool Running;
		private static int Port;
		private static HttpListener Listener;
		
		public static void Start(int port)
		{
			Port = port;
			
			Listener = new HttpListener();
			Listener.Prefixes.Add("http://*:" + Port + "/");
			Listener.Start();
			
			Running = true;
			
			Thread T = new Thread(HttpMonitorThread);
			T.Name = "HttpMonitor Thread";
			T.Start();
		}
		
		public static void Stop()
		{
			Running = false;
		}

		static void HttpMonitorThread()
		{
			while (Running) {
				HttpListenerContext Client = Listener.GetContext();
				HttpListenerResponse Response = Client.Response;

                byte[] bar = new byte[2048];
                
                string commandResult = "";

				try 
				{
					int length = Client.Request.InputStream.Read(bar, 0, 2048);
					
					if(length > 0) {
	                    string Script = ASCIIEncoding.ASCII.GetString(bar).Substring(0, length);
	                    Script = Script.Replace("+", " ");
	                    Script = Script.Replace("%2B", "+");
	                    
	                    int i = Script.IndexOf('=');
	                    Script = Script.Substring(i + 1);
	
	                    int status = Scripting.Interpreter.EvalScript(Script);
	                    commandResult = Scripting.Interpreter.Result;
	                    if (!Scripting.IsOk(status)) {
	                    	commandResult = "<font color='red'>" + commandResult + "</font>";
	                    }
	                }
				}
				catch (IOException) {}
				
				string response = "<html><body>\n";
				response += "<p>You've reached " + Server.theServ.name + "<br />\n";
				response += Server.theServ.motd + "</p>\n";
				response += "<p>Players online: " + Server.theServ.Players.Count + "<br />\n";
                response += "Please leave a message after the tone. Thank you.</p>\n";
                response += "<form method=POST action=#><input type='textbox' name='script' /><button type='submit'></form>";
                if(commandResult != "") {
                	response += "<pre><b>Command result</b>:\n" + commandResult + "</pre>";
                }
                response += "</body></html>";

				byte[] bytes = ASCIIEncoding.ASCII.GetBytes(response);
                Response.OutputStream.Write(bytes, 0, bytes.Length);

				Response.Close();
			}
		}
	}
}