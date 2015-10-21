using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityServer;

namespace UnityServer
{
	public class MainClass
	{
		private static byte[] _buffer = new byte[1024]; //buffer when getting data from the users
		private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //Socket creation

		//private static List<Socket> _clientSocket = new List<Socket> ();//this is only temparory way to hold users connecting
		private static UserList _clientSocket = new UserList(); //this holds all user data 

		//private static Log Debug = new Log(); //This is for the time stamp logging
		private static Commander commandObject = new Commander(); //This contains all the functions for commands sent by the clients

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		public static void Main (string[] args)
		{
			Console.Title = "Altimti Server - MMO";
			SetupServer (); //start the server

			Console.ReadLine ();
		}

		/// <summary>
		/// Setups the server.
		/// </summary>
		private static void SetupServer()
		{
			Console.WriteLine("Setting up server..."); //Tell the admin you are starting
			_serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1024));
			_serverSocket.Listen(50);
			Console.WriteLine("==================================== \n" +
							  "=====    ALTIMIT SERVER - MMO  ===== \n" +
							  "==================================== \n"); //lets just do something fancy to show its ready
			_serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null); //Lets accept users to connect
			Console.WriteLine("Ready for clients...");
		}

		/// <summary>
		/// Accepts the callback.
		/// </summary>
		/// <param name="AR">A.</param>
		private static void AcceptCallback(IAsyncResult AR)
		{
			Socket socket = _serverSocket.EndAccept(AR); 

			Debug.Log("Client Connected..."); //tell the admin someone connected
			_clientSocket.Add(socket, "", "");
			Debug.Log ("Clients connected: " + _clientSocket.Length);
			socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
			_serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
		}

		/// <summary>
		/// Receives the callback.
		/// </summary>
		/// <param name="AR">A.</param>
		private static void ReceiveCallback(IAsyncResult AR)
		{
			Socket socket = (Socket)AR.AsyncState;

			int received = 0;

			try{
				received = socket.EndReceive(AR);
			}
			catch{
				DisconnectUser (socket);
				return;
			}
				
			if (socket.Connected) {
				byte[] dataBuf = new byte[received];

				Array.Copy (_buffer, dataBuf, received);

				string text = Encoding.ASCII.GetString (dataBuf);

				if (text != "") {
					Debug.Log ("Client has sent message: " + text);

					string[] commands = text.Split ('/');

					for (int i = 0; i < commands.Length; i++) {
						if (commands [i] != "") {	
							commands [i] = commands [i].Replace ("/", "");
							DoCommand (commands [i], socket);
						}
					}

				} else {
					DisconnectUser (socket);
					return;
				}

			} else {
				DisconnectUser (socket);
				return;
			}

			socket.BeginReceive (_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback (ReceiveCallback), socket);
		}

		private static void DoCommand(string msg, Socket soc){
			string[] temp = msg.Split (' ');
			string command = temp [0];

			Type type = commandObject.GetType ();
			MethodBase commandFunction = type.GetMethod (command);

			if (commandFunction != null) {
				object[] pars = new object[temp.Length - 1];
				for (int i = 0; i < pars.Length; i++) {
					pars [i] = temp [i + 1];
				}
					
				if ((string)pars [pars.Length - 1] == "") {
					pars [pars.Length - 1] = soc;
				}


				ParameterInfo[] paramaters = commandFunction.GetParameters ();
				object[] endParamaters = AltimitConverter.ConvertParams (pars, paramaters);

				if (commandFunction != null) {
					try {
						commandFunction.Invoke (commandObject, endParamaters);
					} catch (Exception e) {
						Debug.Log (e);
					}
				} else {
					Debug.Log ("commandFunction is null");
				}
			}
		}

		/// <summary>
		/// Sends information back to the users.
		/// </summary>
		public static void SendCallBack(IAsyncResult AR)
		{
			Socket socket = (Socket)AR.AsyncState;

			try
			{
				socket.EndSend(AR);
			}

			catch (ArgumentException e)
			{
				Debug.Log(e);
			}
		}

		public static void ChangeUserRoom(Socket user, String roomName)
		{
			_clientSocket.SetUserRoom(user, roomName);
		}

		public static String GetPlayerRoom(Socket user)
		{
			String playersRoom = _clientSocket.GetUserRoom(user);

			return playersRoom;
		}

		public static int GetClientId(Socket soc)
		{
			return _clientSocket.GetUserId(soc);
		}

		/// <summary>
		/// Disconnects the user.
		/// </summary>
		private static void DisconnectUser(Socket soc)
		{
			string roomToLeave = _clientSocket.GetUserRoom (soc);
			if (roomToLeave != "" || roomToLeave != null) {
				commandObject.LeaveRoom(roomToLeave, soc);
			}

			_clientSocket.Remove (soc);
			soc.Close ();

			Debug.Log ("Client disconnected...");
			Debug.Log ("Clients still connected: " + _clientSocket.Length);
		}
	}
}
