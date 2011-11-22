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

        public event GameInfoReceivedEventHandler GameInfoReceived;
        public event JoinResponseEventHandler JoinResponse;

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

        protected override void HandlePlayerTimeout(IPEndPoint remote)
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                if (remote.Equals(host))
                {
                    // Lost connection to host :(
                    if (ConnectionState == ClientState.Connecting)
                    {
                        Leave();
                        OnJoinResponse(false, JoinResponseReason.ConnectionFailed, null);
                    }
                    else
                    {
                        Player player = players[LocalPlayer];
                        Leave();
                        OnPlayerLeft(player);
                    }
                } // else could not send to other client -> not so bad
            }
        }

        protected override bool HandlePlayerData(IPEndPoint remote, Packet data)
        {
            // Get the message type.
            if (!data.HasByte())
            {
                Console.WriteLine("Received invalid packet, no SessionMessage type.");
                return false;
            }
            SessionMessage type = (SessionMessage)data.ReadByte();

            // Get additional data.
            if (data.HasPacket())
            {
                data = data.ReadPacket();
            }
            else
            {
                data = null;
            }

            switch (type)
            {
                case SessionMessage.GameInfoResponse:
                    {
                        // Get number of max players.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid GameInfoResponse, no maxPlayers.");
                            return false;
                        }
                        int maxPlayers = data.ReadInt32();

                        // Get number of current players.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid GameInfoResponse, no numPlayers.");
                            return false;
                        }
                        int numPlayers = data.ReadInt32();

                        // Get additional data.
                        if (!data.HasPacket())
                        {
                            Console.WriteLine("Received invalid GameInfoResponse, no customData.");
                            return false;
                        }
                        Packet customData = data.ReadPacket();

                        // Propagate to local program.
                        OnGameInfoReceived(remote, numPlayers, maxPlayers, customData);
                    }
                    break;
                case SessionMessage.JoinResponse:
                    if (ConnectionState == ClientState.Connecting && remote.Equals(host))
                    {
                        // Success or not?
                        if (!data.HasBoolean())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no success info.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }
                        bool success = data.ReadBoolean();

                        if (!success)
                        {
                            // Joining failed :(
                            Leave();
                            // Do we at least know why?
                            if (data.HasByte())
                            {
                                OnJoinResponse(false, (JoinResponseReason)data.ReadByte(), null);
                            }
                            else
                            {
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            }
                            return false;
                        }

                        // Get our number.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no player number.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }
                        LocalPlayer = data.ReadInt32();

                        // Sanity check.
                        if (LocalPlayer < 0)
                        {
                            Console.WriteLine("Received invalid JoinResponse, invalid local player number.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }

                        // Get info about other players in the session.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no number of other players.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }
                        NumPlayers = data.ReadInt32();

                        // Sanity check.
                        if (NumPlayers < 0)
                        {
                            Console.WriteLine("Received invalid JoinResponse, negative number of other players.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }

                        // Maximum number of players in the session?
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no number of max players.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }
                        MaxPlayers = data.ReadInt32();

                        // Sanity check.
                        if (MaxPlayers < 0)
                        {
                            Console.WriteLine("Received invalid JoinResponse, negative number of max players.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }
                        if (MaxPlayers < NumPlayers)
                        {
                            Console.WriteLine("Received invalid JoinResponse, number of max players smaller than number of players.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }
                        if (LocalPlayer >= MaxPlayers)
                        {
                            Console.WriteLine("Received invalid JoinResponse, local player number larger than max players.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
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
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }
                            int playerNumber = data.ReadInt32();

                            // Sanity checks.
                            if (playerNumber < 0)
                            {
                                Console.WriteLine("Received invalid JoinResponse, negative player number.");
                                Leave();
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }
                            if (playerNumber >= MaxPlayers)
                            {
                                Console.WriteLine("Received invalid JoinResponse, player number larger than max players.");
                                Leave();
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }
                            if (players[playerNumber] != null)
                            {
                                Console.WriteLine("Received invalid JoinResponse, duplicate player info.");
                                Leave();
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }

                            // Get player name.
                            if (!data.HasString())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player name.");
                                Leave();
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }
                            string playerName = data.ReadString().Trim();

                            // Get additional player data.
                            if (!data.HasByteArray())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player data.");
                                Leave();
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }
                            byte[] playerData = data.ReadByteArray();

                            // Get players IP address.
                            if (!data.HasByteArray())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player address.");
                                Leave();
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }
                            IPAddress playerAddress = new IPAddress(data.ReadByteArray());

                            // And the port.
                            if (!data.HasInt32())
                            {
                                Console.WriteLine("Received invalid JoinResponse, no player port.");
                                Leave();
                                OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                                return false;
                            }
                            IPEndPoint playerEndPoint = new IPEndPoint(playerAddress, data.ReadInt32());

                            // All OK, add the player.
                            players[playerNumber] = new Player(playerNumber, playerName, playerData);
                            playerAddresses[playerNumber] = playerEndPoint;
                        }

                        // Get other game relevant data (e.g. game state).
                        if (!data.HasPacket())
                        {
                            Console.WriteLine("Received invalid JoinResponse, no join data.");
                            Leave();
                            OnJoinResponse(false, JoinResponseReason.InvalidServerData, null);
                            return false;
                        }
                        Packet joinData = data.ReadPacket();

                        // New state :)
                        ConnectionState = ClientState.Connected;

                        // OK, let the program know.
                        OnJoinResponse(true, JoinResponseReason.Success, joinData);
                        
                        // Also, fire one join event for each player in the game. Except for
                        // the local player, because that'll likely need special treatment anyway.
                        for (int i = 0; i < MaxPlayers; ++i)
                        {
                            if (i != LocalPlayer && players[i] != null)
                            {
                                OnPlayerJoined(players[i]);
                            }
                        }
                        return true;
                    }
                    break;
                case SessionMessage.PlayerJoined:
                    if (ConnectionState == ClientState.Connected && remote.Equals(host))
                    {
                        // Get player number.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player number.");
                            return false;
                        }
                        int playerNumber = data.ReadInt32();

                        // Sanity checks.
                        if (playerNumber < 0)
                        {
                            Console.WriteLine("Received invalid PlayerJoined, negative player number.");
                            return false;
                        }
                        if (playerNumber >= MaxPlayers)
                        {
                            Console.WriteLine("Received invalid PlayerJoined, player number larger than max players.");
                            return false;
                        }
                        if (players[playerNumber] != null)
                        {
                            Console.WriteLine("Received invalid PlayerJoined, duplicate player info.");
                            return false;
                        }

                        // Get player name.
                        if (!data.HasString())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player name.");
                            return false;
                        }
                        string playerName = data.ReadString().Trim();

                        // Get additional player data.
                        if (!data.HasByteArray())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player data.");
                            return false;
                        }
                        byte[] playerData = data.ReadByteArray();

                        // Get players IP address.
                        if (!data.HasByteArray())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player address.");
                            return false;
                        }
                        IPAddress playerAddress = new IPAddress(data.ReadByteArray());

                        // And the port.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid PlayerJoined, no player port.");
                            return false;
                        }
                        IPEndPoint playerEndPoint = new IPEndPoint(playerAddress, data.ReadInt32());

                        // All OK, add the player.
                        players[playerNumber] = new Player(playerNumber, playerName, playerData);
                        playerAddresses[playerNumber] = playerEndPoint;

                        // The the local program about it.
                        OnPlayerJoined(players[playerNumber]);

                        return true;
                    }
                    break;
                case SessionMessage.PlayerLeft:
                    if (ConnectionState == ClientState.Connected && remote.Equals(host))
                    {
                        // Get player number.
                        if (!data.HasInt32())
                        {
                            Console.WriteLine("Received invalid PlayerLeft, no player number.");
                            return false;
                        }
                        int playerNumber = data.ReadInt32();

                        // Sanity checks.
                        if (playerNumber < 0)
                        {
                            Console.WriteLine("Received invalid PlayerLeft, negative player number.");
                            return false;
                        }
                        if (playerNumber >= MaxPlayers)
                        {
                            Console.WriteLine("Received invalid PlayerLeft, player number larger than max players.");
                            return false;
                        }
                        if (players[playerNumber] == null)
                        {
                            Console.WriteLine("Received invalid PlayerLeft, no such player.");
                            return false;
                        }

                        // OK, remove the player.
                        Player player = players[playerNumber];
                        players[playerNumber] = null;
                        playerAddresses[playerNumber] = null;

                        // Tell the local program about it.
                        OnPlayerLeft(player);

                        return true;
                    }
                    break;
                case SessionMessage.Data:
                    if (ConnectionState == ClientState.Connected)
                    {
                        // Custom data, just forward it if we're in a game.
                        return base.HandlePlayerData(remote, data);
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
            return false;
        }

        private void OnGameInfoReceived(IPEndPoint host, int numPlayers, int maxPlayers, Packet data)
        {
            if (GameInfoReceived != null)
            {
                GameInfoReceived(host, numPlayers, maxPlayers, data);
            }
        }

        private void OnJoinResponse(bool success, JoinResponseReason reason, Packet data)
        {
            if (JoinResponse != null)
            {
                JoinResponse(success, reason, data);
            }
        }
    }
}
