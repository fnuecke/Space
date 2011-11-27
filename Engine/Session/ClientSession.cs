using System;
using System.Net;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

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

        public ClientSession(Game game, IProtocol protocol)
            : base(game, protocol)
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
                    try
                    {
                        // Get number of max players.
                        int maxPlayers = data.ReadInt32();

                        // Get number of current players.
                        int numPlayers = data.ReadInt32();

                        // Get additional data.
                        Packet customData = data.ReadPacket();

                        // Propagate to local program.
                        OnGameInfoReceived(new GameInfoReceivedEventArgs(args.Remote, numPlayers, maxPlayers, customData));

                        // OK, handled it (not really necessary in current implementation,
                        // as it's unacked, but it can't hurt).
                        args.Consume();
                    }
#if DEBUG
                    catch (PacketException ex)
                    {
                        Console.WriteLine("Invalid GameInfoResponse: " + ex.ToString());
                    }
#else
                    catch (PacketException)
                    {
                    }
#endif
                    break;
                case SessionMessage.JoinResponse:
                    if (ConnectionState == ClientState.Connecting && args.Remote.Equals(host))
                    {
                        try
                        {
                            // Success or not?
                            if (!data.ReadBoolean())
                            {
                                // Joining failed :(
                                Leave();
                                OnJoinResponse(new JoinResponseEventArgs(false, (JoinResponseReason)data.ReadByte(), null));
                                return;
                            }

                            // Get our number.
                            LocalPlayer = data.ReadInt32();

                            // Get info about other players in the session.
                            NumPlayers = data.ReadInt32();

                            // Maximum number of players in the session?
                            MaxPlayers = data.ReadInt32();

                            // Sanity checks.
                            if (LocalPlayer < 0 || NumPlayers < 0 || MaxPlayers < 0 || MaxPlayers < NumPlayers || LocalPlayer >= MaxPlayers)
                            {
                                throw new PacketException("Inconsistent session info.");
                            }

                            // Allocate arrays for the players in the session.
                            playerAddresses = new IPEndPoint[MaxPlayers];
                            players = new Player[MaxPlayers];

                            // Get info on players already in the session.
                            for (int i = 0; i < NumPlayers; i++)
                            {
                                // Get player number.
                                int playerNumber = data.ReadInt32();

                                // Get player name.
                                string playerName = data.ReadString().Trim();

                                // Get additional player data.
                                byte[] playerData = data.ReadByteArray();

                                // Get players IP address.
                                IPAddress playerAddress = new IPAddress(data.ReadByteArray());

                                // And the port, making up the full endpoint.
                                IPEndPoint playerEndPoint = new IPEndPoint(playerAddress, data.ReadInt32());

                                // Sanity checks.
                                if (playerNumber < 0 || playerNumber >= MaxPlayers || players[playerNumber] != null)
                                {
                                    throw new PacketException("Invalid player number.");
                                }

                                // All OK, add the player.
                                playerAddresses[playerNumber] = playerEndPoint;
                                players[playerNumber] = new Player(playerNumber, playerName, playerData,
                                    delegate() { return protocol.GetPing(playerAddresses[playerNumber]); });
                            }

                            // Get other game relevant data (e.g. game state).
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

                            // OK, handled.
                            args.Consume();
                        }
#if DEBUG
                        catch (PacketException ex)
                        {
                            Console.WriteLine("Invalid JoinResponse: " + ex.ToString());
#else
                        catch (PacketException)
                        {
#endif
                            Leave();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                        }
                    }
                    break;
                case SessionMessage.PlayerJoined:
                    if (ConnectionState == ClientState.Connected && args.Remote.Equals(host))
                    {
                        try
                        {
                            // Get player number.
                            int playerNumber = data.ReadInt32();

                            // Get player name.
                            string playerName = data.ReadString().Trim();

                            // Get additional player data.
                            byte[] playerData = data.ReadByteArray();

                            // Get players IP address.
                            IPAddress playerAddress = new IPAddress(data.ReadByteArray());

                            // And the port, making up the full endpoint.
                            IPEndPoint playerEndPoint = new IPEndPoint(playerAddress, data.ReadInt32());

                            // Sanity checks.
                            if (playerNumber < 0 || playerNumber >= MaxPlayers || players[playerNumber] != null)
                            {
                                throw new PacketException("Invalid player number.");
                            }

                            // All OK, add the player.
                            playerAddresses[playerNumber] = playerEndPoint;
                            players[playerNumber] = new Player(playerNumber, playerName, playerData,
                                    delegate() { return protocol.GetPing(playerAddresses[playerNumber]); });

                            // The the local program about it.
                            OnPlayerJoined(new PlayerEventArgs(players[playerNumber]));

                            // OK, handled it.
                            args.Consume();
                        }
#if DEBUG
                        catch (PacketException ex)
                        {
                            Console.WriteLine("Invalid PlayerJoined: " + ex.ToString());
                        }
#else
                        catch (PacketException)
                        {
                        }
#endif
                    }
                    break;
                case SessionMessage.PlayerLeft:
                    if (ConnectionState == ClientState.Connected && args.Remote.Equals(host))
                    {
                        try
                        {
                            // Get player number.
                            int playerNumber = data.ReadInt32();

                            // Sanity checks.
                            if (playerNumber < 0 || playerNumber >= MaxPlayers || players[playerNumber] == null)
                            {
                                throw new PacketException("Invalid player number.");
                            }

                            // OK, remove the player.
                            Player player = players[playerNumber];
                            players[playerNumber] = null;
                            playerAddresses[playerNumber] = null;

                            // Tell the local program about it.
                            OnPlayerLeft(new PlayerEventArgs(player));

                            // OK, handled it.
                            args.Consume();
                        }
#if DEBUG
                        catch (PacketException ex)
                        {
                            Console.WriteLine("Invalid PlayerLeft: " + ex.ToString());
                        }
#else
                        catch (PacketException)
                        {
                        }
#endif
                    }
                    break;
                case SessionMessage.Data:
                    if (ConnectionState == ClientState.Connected)
                    {
                        // Custom data, just forward it if we're in a session.
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
#if DEBUG
                    Console.WriteLine("Received packet with unknown session message type {0}.", type);
#endif
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
