using System;
using System.Collections.Generic;
using System.Configuration;
using Lidgren.Network;

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
	class Program
	{
		// Client Object
		static NetClient Client;

		// Clients list of characters
		static List<Character> GameStateList;

		// Create timer that tells client, when to send update
		static System.Timers.Timer update;

		// Indicates if program is running
		static bool IsRunning = true;

		static string hostip;
		static int port;

		static void Main()
		{
			// Ask for IP
			// Read Ip to string
			hostip = ConfigurationManager.AppSettings["host"];
			port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
			Console.WriteLine("Enter IP To Connect - {0}:{1}", hostip, port);
			Console.Read();

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


			Console.WriteLine("Client Started");

			// Create the list of characters
			GameStateList = new List<Character>();

			// Set timer to tick every 50ms
			update = new System.Timers.Timer(50);

			// When time has elapsed ( 50ms in this case ), call "update_Elapsed" funtion
			update.Elapsed += new System.Timers.ElapsedEventHandler(update_Elapsed);

			// Funtion that waits for connection approval info from server
			WaitForStartingInfo();

			// Start the timer
			update.Start();

			// While..running
			while (IsRunning)
			{
				// Just loop this like madman
				GetInputAndSendItToServer();

			}
		}



		/// <summary>
		/// Every 50ms this is fired
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		static void update_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			// Check if server sent new messages
			CheckServerMessages();

			// Draw the world
			DrawGameState();
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
						Console.WriteLine("World State uppaus");
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

			// Enum object
			MoveDirection MoveDir = new MoveDirection();

			// Default movement is none
			MoveDir = MoveDirection.NONE;

			// Readkey ( NOTE: This normally stops the code flow. Thats why we have timer running, that gets updates)
			// ( Timers run in different threads, so that can be run, even thou we sit here and wait for input )
			ConsoleKeyInfo kinfo = Console.ReadKey();

			// This is wsad controlling system
			if (kinfo.KeyChar == 'w')
				MoveDir = MoveDirection.UP;
			if (kinfo.KeyChar == 's')
				MoveDir = MoveDirection.DOWN;
			if (kinfo.KeyChar == 'a')
				MoveDir = MoveDirection.LEFT;
			if (kinfo.KeyChar == 'd')
				MoveDir = MoveDirection.RIGHT;

			if (kinfo.KeyChar == 'q')
			{

				// Disconnect and give the reason
				Client.Disconnect("bye bye");

			}

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
		private static void DrawGameState()
		{
			Console.Clear();
			Console.WriteLine("Enter IP To Connect {0}:{1}", hostip, port);
			Console.WriteLine("Connections status: " + (NetConnectionStatus)Client.ServerConnection.Status);
			// Draw each player to their positions
			for (int index = 0; index < GameStateList.Count; ++index)
			{
				Console.WriteLine("Player[{0}] = \"{1}\"", (index + 1), GameStateList[index].X);
			}
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