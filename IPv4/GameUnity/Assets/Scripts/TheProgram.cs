using System;
using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;

// Lidgren Network example
// Made by: Riku Koskinen
// http://xnacoding.blogspot.com/
// Download LidgreNetwork at: http://code.google.com/p/lidgren-network-gen3/
//
// You can use this code in anyway you want
// Code is not perfect, but it works
// It's example of console based game, where new players can join and move
// Movement is updated to all clients.


// THIS IS VERY VERY VERY BASIC EXAMPLE OF NETWORKING IN GAMES
// NO PREDICTION, NO LAG COMPENSATION OF ANYKIND

namespace GameClient
{
	public class TheProgram : MonoBehaviour
	{
		// Client Object
		static NetClient Client;

		// Clients list of characters
		static List<Character> GameStateList;

		// Indicates if program is running
		static bool IsRunning = true;

		static string hostip;
		static int port;

		public static string ResultText = "GAME";
		public static string ErrorText = String.Empty;
		static MoveDirection MoveDir;

		// Use this for initialization
		private void Start()
		{
			//try
			//{
			string destPlatform = String.Empty;
#if UNITY_ANDROID
			destPlatform = "Android";
#endif
#if UNITY_IPHONE
			destPlatform = "iOS";
#endif
			string fileNameWithPath = String.Format("TextFiles/{0}.txt", destPlatform);

			// Get data from Resources.
			IResourceManager resourceManager = new ResourceManager();
			TextAsset asset = (TextAsset)resourceManager.LoadResourceImmediate(typeof(TextAsset), fileNameWithPath);
			String text = asset.text;
			hostip = resourceManager.GetInformationFromFile(text, "HOST");
			port = Convert.ToInt32(resourceManager.GetInformationFromFile(text, "PORT"));

			// Get data from Streaming Assets
			string fileRoot = Application.streamingAssetsPath;
			string fullPath = fileRoot + "/GameServer.txt";
			IConfigManager configManager = new ConfigManager();
			hostip = configManager.GetInformationFromFile(fullPath, hostip);

			// Create new instance of configs. Parameter is "application Id". It has to be same on client and server.
			NetPeerConfiguration Config = new NetPeerConfiguration("game");

			// Create new client, with previously created configs
			Client = new NetClient(Config);

			// Create new outgoing message
			NetOutgoingMessage outmsg = Client.CreateMessage();


			//LoginPacket lp = new LoginPacket("Katu");

			// Start client
			Client.Start();

			// Write byte ( first byte informs server about the message type ) ( This way we know, what kind of variables to read )
			outmsg.Write((byte)PacketTypes.LOGIN);

			// Write String "Name" . Not used, but just showing how to do it
			outmsg.Write("MyName");

			// Connect client, to ip previously requested from user 
			Client.Connect(hostip, port, outmsg);


			ResultText = "Client Started";

			// Create the list of characters
			GameStateList = new List<Character>();

			// Funtion that waits for connection approval info from server
			WaitForStartingInfo();
			//}
			//catch (Exception ex)
			//{
			//	ErrorText = ex.ToString();
			//}
		}

		private void Update()
		{
			// While..running
			//while (IsRunning)
			//{
			// Check if server sent new messages
			CheckServerMessages();

			// Just loop this like madman
			GetInputAndSendItToServer();
			//}
		}




		// Before main looping starts, we loop here and wait for approval message
		private static void WaitForStartingInfo()
		{
			// When this is set to true, we are approved and ready to go
			bool CanStart = false;

			// New incomgin message
			NetIncomingMessage inc;

			// Loop untill we are approved
			while (!CanStart)
			{

				// If new messages arrived
				if ((inc = Client.ReadMessage()) != null)
				{
					// Switch based on the message types
					switch (inc.MessageType)
					{

						// All manually sent messages are type of "Data"
						case NetIncomingMessageType.Data:

							// Read the first byte
							// This way we can separate packets from each others
							if (inc.ReadByte() == (byte)PacketTypes.WORLDSTATE)
							{
								// Worldstate packet structure
								//
								// int = count of players
								// character obj * count



								//Console.WriteLine("WorldState Update");

								// Empty the gamestatelist
								// new data is coming, so everything we knew on last frame, does not count here
								// Even if client would manipulate this list ( hack ), it wont matter, becouse server handles the real list
								GameStateList.Clear();

								// Declare count
								int count = 0;

								// Read int
								count = inc.ReadInt32();

								// Iterate all players
								for (int i = 0; i < count; i++)
								{

									// Create new character to hold the data
									Character ch = new Character();

									// Read all properties ( Server writes characters all props, so now we can read em here. Easy )
									inc.ReadAllProperties(ch);

									// Add it to list
									GameStateList.Add(ch);
								}

								// When all players are added to list, start the game
								CanStart = true;
							}
							break;

						default:
							// Should not happen and if happens, don't care
							Console.WriteLine(inc.ReadString() + " Strange message");
							break;
					}
				}
			}
		}


		/// <summary>
		/// Check for new incoming messages from server
		/// </summary>
		private static void CheckServerMessages()
		{
			// Create new incoming message holder
			NetIncomingMessage inc;

			// While theres new messages
			//
			// THIS is exactly the same as in WaitForStartingInfo() function
			// Check if its Data message
			// If its WorldState, read all the characters to list
			while ((inc = Client.ReadMessage()) != null)
			{
				if (inc.MessageType == NetIncomingMessageType.Data)
				{
					if (inc.ReadByte() == (byte)PacketTypes.WORLDSTATE)
					{
						//Console.WriteLine("World State uppaus");
						GameStateList.Clear();
						int jii = 0;
						jii = inc.ReadInt32();
						for (int i = 0; i < jii; i++)
						{
							Character ch = new Character();
							inc.ReadAllProperties(ch);
							GameStateList.Add(ch);
						}
					}
				}
			}
		}


		// Get input from player and send it to server
		private static void GetInputAndSendItToServer()
		{
			// If button was pressed and it was some of those movement keys
			if (MoveDir != MoveDirection.NONE)
			{
				// Create new message
				NetOutgoingMessage outmsg = Client.CreateMessage();

				// Write byte = Set "MOVE" as packet type
				outmsg.Write((byte)PacketTypes.MOVE);

				// Write byte = move direction
				outmsg.Write((byte)MoveDir);

				// Send it to server
				Client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered);

				// Reset movedir
				MoveDir = MoveDirection.NONE;
			}

		}

		// Move direction enumerator
		enum MoveDirection
		{
			NONE,
			UP,
			DOWN,
			LEFT,
			RIGHT,
		}

		// Drawing Gamescreen
		// First clear console
		// Then draw our 3D world ( Hope you brought your glasses with you ;) )
		private void OnGUI()
		{
			float yPos = 5.0f;
			float xPos = 5.0f;
			float width = ((Screen.width/2) - (Screen.width/15));
			float height = Screen.height/4;
			float heightPlus = height + 10.0f;

			GUI.skin.button.fontSize = 30;
			GUI.skin.button.fontStyle = FontStyle.Bold;

			if (GUI.Button(new Rect(xPos, yPos, width, height), "Start"))
			{
				MoveDir = MoveDirection.UP;
			}
			if (GUI.Button(new Rect(xPos + width, yPos, width, height), "Quit"))
			{
				// Disconnect and give the reason
				Client.Disconnect("bye bye");
				Application.Quit();
			}

			string conn = String.Format("Enter IP To Connect {0}:{1}", hostip, port);
			string stat = String.Empty;
			if (null != Client && null != Client.ServerConnection)
			{
				stat = "Connections status: " + (NetConnectionStatus) Client.ServerConnection.Status;
			}

			string text = String.Empty;
			for (int index = 0; index < GameStateList.Count; ++index)
			{
				text += String.Format("Player[{0}] = \"{1}\"", (index + 1), GameStateList[index].X);
				text += Environment.NewLine;
			}

			ResultText = !String.IsNullOrEmpty(ErrorText)
				? ErrorText
				: String.Format("{1}{0}{2}{0}{3}", Environment.NewLine, conn, stat, text);
			xPos = 5.0f;
			GUI.skin.label.fontSize = 50;
			GUI.Label(new Rect(xPos, yPos += heightPlus, Screen.width, Screen.height - yPos), ResultText);
		}
	}


	class Character
	{
		public int X { get; set; }
		public int Y { get; set; }
		public string Name { get; set; }
		public NetConnection Connection { get; set; }
		public Character(string name, int x, int y, NetConnection conn)
		{
			Name = name;
			X = x;
			Y = y;
			Connection = conn;
		}
		public Character()
		{
		}
	}
	enum PacketTypes
	{
		LOGIN,
		MOVE,
		WORLDSTATE
	}
}