using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace spacecraft {
	static class HttpMonitor {
		private static bool Running;
		private static int Port;
		private static HttpListener Listener;
		
		public static int port { get { return Port; } }
		
		public static void Start(int port)
		{
			// Port of 0 equals disabled.
			if(port == 0) return;
			
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
				
				bool authenticated = false;
				
				foreach (string header in Client.Request.Headers.AllKeys) {
					if (header == "Authorization") {
						string auth = Client.Request.Headers[header];
						int i = auth.IndexOf(' ');
						string key = auth.Substring(i + 1);
						authenticated = KeyIsCorrect(key);
						break;
					}
				}
				
				if(!authenticated) {
					PleaseAuthenticate(Client.Response);
				} else {
					ProcessRequest(Client);
				}

				Client.Response.Close();
			}
		}
		
		static void ProcessRequest(HttpListenerContext Client)
		{
			HttpListenerRequest Request = Client.Request;
			HttpListenerResponse Response = Client.Response;
			
			byte[] bar = new byte[2048];
			
			string commandResult = "";
			
			try 
			{
				int length = Request.InputStream.Read(bar, 0, 2048);
				
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
            catch(IOException) {}

            StringBuilder builder = new StringBuilder();
			builder.Append("<html><body>\n");
			builder.Append( "<p>You've reached ");
            builder.Append(Server.theServ.name);
            builder.Append("<br />\n");
            builder.Append(Server.theServ.motd);
            builder.Append("</p>\n");
			builder.Append("<p>Players online: " );
            builder.Append(Server.theServ.Players.Count );
            builder.Append("<br />\n");
			builder.Append( "Please leave a message after the tone. Thank you.</p>\n");
			builder.Append( "<form method=POST action=#><input type='textbox' name='script' /><button type='submit>Submit</button></form>");

            string response = builder.ToString();

			if(commandResult != "") {
				response += "<pre><b>Command result</b>:\n" + commandResult + "</pre>";
			}
			response += "</body></html>";

			byte[] bytes = ASCIIEncoding.ASCII.GetBytes(response);
			Response.OutputStream.Write(bytes, 0, bytes.Length);
		}
		
		static void PleaseAuthenticate(HttpListenerResponse Response)
		{
			Response.StatusCode = 401;
			Response.AddHeader("WWW-Authenticate", "Basic realm=\"Spacecraft Server\"");
			
			string response =
			@"<!DOCTYPE HTML PUBLIC {0}-//W3C//DTD HTML 4.01 Transitional//EN{0}
			 {0}http://www.w3.org/TR/1999/REC-html401-19991224/loose.dtd{0}>
			<HTML>
			  <HEAD>
			    <TITLE>Error</TITLE>
			    <META HTTP-EQUIV={0}Content-Type{0} CONTENT={0}text/html; charset=ISO-8859-1{0}>
			  </HEAD>
			  <BODY><H1>401 Unauthorized.</H1></BODY>
			</HTML>";
			response = String.Format(response, "\"");
			byte[] bytes = ASCIIEncoding.ASCII.GetBytes(response);
			Response.OutputStream.Write(bytes, 0, bytes.Length);
		}
		
		static bool KeyIsCorrect(string key)
		{
			string username = Config.Get("http-username", "admin");
			string password = Config.Get("http-password", "beans");
			
			return (key == Spacecraft.Base64Encode(username + ":" + password));
		}
	}
}