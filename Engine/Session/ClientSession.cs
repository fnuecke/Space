using System;
using System.Net;
using Engine.Network;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// Used for joining sessions.
    /// </summary>
    sealed class ClientSession : AbstractSession, IClientSession
    {

        public event EventHandler<EventArgs> GameInfoReceived;
        public event EventHandler<EventArgs> JoinResponse;

        /// <summary>
        /// Current state of this session.
        /// </summary>
        public ClientState ConnectionState { get; set; }

        /// <summary>
        /// The actual host address of the game we're in / trying to join.
        /// </summary>
        private IPEndPoint host;

        public ClientSession(IProtocol protocol)
            : base(protocol)
        {
            ConnectionState = ClientState.Unconnected;
        }

        public void Search()
        {
            Packet packet = new Packet(1);
            packet.Write((byte)SessionMessage.GameInfoRequest);
            // Send as a multicast / broadcast.
            protocol.Send(packet, new IPEndPoint(DefaultMulticastAddress, DefaultMulticastPort));
        }

        public void Join(IPEndPoint remote, string playerName, byte[] data)
        {
            if (ConnectionState == ClientState.Unconnected)
            {
                host = remote;
                Packet packet = new Packet();
                packet.Write(playerName);
                packet.Write(data);
                Send(host, SessionMessage.JoinRequest, packet, 100);
                ConnectionState = ClientState.Connecting;
            }
            else
            {
                throw new InvalidOperationException("Must leave the current game first.");
            }
        }

        public void Leave()
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                Send(host, SessionMessage.Leave, null);
            }

            ConnectionState = ClientState.Unconnected;
            playerAddresses = null;
            host = null;
            LocalPlayer = 0;
            NumPlayers = 0;
            MaxPlayers = 0;
        }

        protected override void HandlePlayerTimeout(object sender, EventArgs e)
        {
            var args = (ProtocolEventArgs)e;

            if (ConnectionState != ClientState.Unconnected)
            {
                if (args.Remote.Equals(host))
                {
                    // Lost connection to host :(
                    if (ConnectionState == ClientState.Connecting)
                    {
                        Leave();
                        OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.ConnectionFailed, null));
                    }
                    else
                    {
                        Player player = players[LocalPlayer];
                        Leave();
                        OnPlayerLeft(new PlayerEventArgs(player));
                    }
                } // else could not send to other client -> not so bad
            }
        }

        protected override void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (ProtocolDataEventArgs)e;

            // Get the message type.
            if (!args.Data.HasByte())
            {
                Console.WriteLine("Received invalid packet, no SessionMessage type.");
                return;
            }
            SessionMessage type = (SessionMessage)args.Data.ReadByte();

            // Get additional data.
            Packet data = null;
            if (args.Data.HasPacket())
            {
                data = args.Data.ReadPacket();
            }

            switch (type)
            {
                case SessionMessage.GameInfoResponse:
                    {
                        // Get number of max players.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid GameInfoResponse, no maxPlayers.");
                            return;
                        }
                        int maxPlayers = data.ReadInt32();

                        // Get number of current players.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid GameInfoResponse, no numPlayers.");
                            return;
                        }
                        int numPlayers = data.ReadInt32();

                        // Get additional data.
                        if (!data.HasPacket())
                        {
                            Console.WriteLine("Received invalid GameInfoResponse, no customData.");
                            return;
                        }
                        Packet customData = data.ReadPacket();

                        // Propagate to local program.
                        OnGameInfoReceived(new GameInfoReceivedEventArgs(args.Remote, numPlayers, maxPlayers, customData));
                    }
                    break;
                case SessionMessage.JoinResponse:
                    if (ConnectionState == ClientState.Connecting && args.Remote.Equals(host))
                    {
                        // Success or not?
                        if (!data.HasBoolean())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no success info.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }
                        bool success = data.ReadBoolean();

                        if (!success)
                        {
                            // Joining failed :(
                            Leave();
                            // Do we at least know why?
                            if (data.HasByte())
                            {
                                OnJoinResponse(new JoinResponseEventArgs(false, (JoinResponseReason)data.ReadByte(), null));
                            }
                            else
                            {
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            }
                            return;
                        }

                        // Get our number.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no player number.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }
                        LocalPlayer = data.ReadInt32();

                        // Sanity check.
                        if (LocalPlayer < 0)
                        {
                            Console.WriteLine("Received invalid JoinResponse, invalid local player number.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }

                        // Get info about other players in the session.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no number of other players.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }
                        NumPlayers = data.ReadInt32();

                        // Sanity check.
                        if (NumPlayers < 0)
                        {
                            Console.WriteLine("Received invalid JoinResponse, negative number of other players.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }

                        // Maximum number of players in the session?
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no number of max players.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }
                        MaxPlayers = data.ReadInt32();

                        // Sanity check.
                        if (MaxPlayers < 0)
                        {
                            Console.WriteLine("Received invalid JoinResponse, negative number of max players.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }
                        if (MaxPlayers < NumPlayers)
                        {
                            Console.WriteLine("Received invalid JoinResponse, number of max players smaller than number of players.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }
                        if (LocalPlayer >= MaxPlayers)
                        {
                            Console.WriteLine("Received invalid JoinResponse, local player number larger than max players.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }

                        // Allocate arrays for the players in the session.
                        playerAddresses = new IPEndPoint[MaxPlayers];
                        players = new Player[MaxPlayers];

                        for (int i = 0; i < NumPlayers; i++)
                        {
                            // Get player number.
                            if (!data.HasInt32())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player number.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }
                            int playerNumber = data.ReadInt32();

                            // Sanity checks.
                            if (playerNumber < 0)
                            {
                                Console.WriteLine("Received invalid JoinResponse, negative player number.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }
                            if (playerNumber >= MaxPlayers)
                            {
                                Console.WriteLine("Received invalid JoinResponse, player number larger than max players.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }
                            if (players[playerNumber] != null)
                            {
                                Console.WriteLine("Received invalid JoinResponse, duplicate player info.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }

                            // Get player name.
                            if (!data.HasString())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player name.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }
                            string playerName = data.ReadString().Trim();

                            // Get additional player data.
                            if (!data.HasByteArray())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player data.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }
                            byte[] playerData = data.ReadByteArray();

                            // Get players IP address.
                            if (!data.HasByteArray())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player address.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }
                            IPAddress playerAddress = new IPAddress(data.ReadByteArray());

                            // And the port.
                            if (!data.HasInt32())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player port.");
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                                return;
                            }
                            IPEndPoint playerEndPoint = new IPEndPoint(playerAddress, data.ReadInt32());

                            // All OK, add the player.
                            playerAddresses[playerNumber] = playerEndPoint;
                            players[playerNumber] = new Player(playerNumber, playerName, playerData,
                                delegate() { return protocol.GetPing(playerAddresses[playerNumber]); });
                        }

                        // Get other game relevant data (e.g. game state).
                        if (!data.HasPacket())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no join data.");
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                            return;
                        }
                        Packet joinData = data.ReadPacket();

                        // New state :)
                        ConnectionState = ClientState.Connected;

                        // OK, let the program know.
                        OnJoinResponse(new JoinResponseEventArgs(true, JoinResponseReason.Success, joinData));
                        
                        // Also, fire one join event for each player in the game. Except for
                        // the local player, because that'll likely need special treatment anyway.
                        for (int i = 0; i < MaxPlayers; ++i)
                        {
                            if (i != LocalPlayer && players[i] != null)
                            {
                                OnPlayerJoined(new PlayerEventArgs(players[i]));
                            }
                        }

                        args.Consume();
                    }
                    break;
                case SessionMessage.PlayerJoined:
                    if (ConnectionState == ClientState.Connected && args.Remote.Equals(host))
                    {
                        // Get player number.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player number.");
                            return;
                        }
                        int playerNumber = data.ReadInt32();

                        // Sanity checks.
                        if (playerNumber < 0)
                        {
                            Console.WriteLine("Received invalid PlayerJoined, negative player number.");
                            return;
                        }
                        if (playerNumber >= MaxPlayers)
                        {
                            Console.WriteLine("Received invalid PlayerJoined, player number larger than max players.");
                            return;
                        }
                        if (players[playerNumber] != null)
                        {
                            Console.WriteLine("Received invalid PlayerJoined, duplicate player info.");
                            return;
                        }

                        // Get player name.
                        if (!data.HasString())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player name.");
                            return;
                        }
                        string playerName = data.ReadString().Trim();

                        // Get additional player data.
                        if (!data.HasByteArray())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player data.");
                            return;
                        }
                        byte[] playerData = data.ReadByteArray();

                        // Get players IP address.
                        if (!data.HasByteArray())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player address.");
                            return;
                        }
                        IPAddress playerAddress = new IPAddress(data.ReadByteArray());

                        // And the port.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player port.");
                            return;
                        }
                        IPEndPoint playerEndPoint = new IPEndPoint(playerAddress, data.ReadInt32());

                        // All OK, add the player.
                        playerAddresses[playerNumber] = playerEndPoint;
                        players[playerNumber] = new Player(playerNumber, playerName, playerData,
                                delegate() { return protocol.GetPing(playerAddresses[playerNumber]); });

                        // The the local program about it.
                        OnPlayerJoined(new PlayerEventArgs(players[playerNumber]));

                        args.Consume();
                    }
                    break;
                case SessionMessage.PlayerLeft:
                    if (ConnectionState == ClientState.Connected && args.Remote.Equals(host))
                    {
                        // Get player number.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid PlayerLeft, no player number.");
                            return;
                        }
                        int playerNumber = data.ReadInt32();

                        // Sanity checks.
                        if (playerNumber < 0)
                        {
                            Console.WriteLine("Received invalid PlayerLeft, negative player number.");
                            return;
                        }
                        if (playerNumber >= MaxPlayers)
                        {
                            Console.WriteLine("Received invalid PlayerLeft, player number larger than max players.");
                            return;
                        }
                        if (players[playerNumber] == null)
                        {
                            Console.WriteLine("Received invalid PlayerLeft, no such player.");
                            return;
                        }

                        // OK, remove the player.
                        Player player = players[playerNumber];
                        players[playerNumber] = null;
                        playerAddresses[playerNumber] = null;

                        // Tell the local program about it.
                        OnPlayerLeft(new PlayerEventArgs(player));

                        args.Consume();
                    }
                    break;
                case SessionMessage.Data:
                    if (ConnectionState == ClientState.Connected)
                    {
                        // Custom data, just forward it if we're in a game.
                        ConditionalOnPlayerData(args, data);
                    }
                    break;
                case SessionMessage.GameInfoRequest:
                case SessionMessage.JoinRequest:
                case SessionMessage.Leave:
                    // Ignore as a client.
                    break;
                default:
                    // Invalid packet.
                    Console.WriteLine("Received packet with unknown session message type {0}.", type);
                    break;
            }
        }

        private void OnGameInfoReceived(GameInfoReceivedEventArgs e)
        {
            if (GameInfoReceived != null)
            {
                GameInfoReceived(this, e);
            }
        }

        private void OnJoinResponse(JoinResponseEventArgs e)
        {
            if (JoinResponse != null)
            {
                JoinResponse(this, e);
            }
        }
    }
}
