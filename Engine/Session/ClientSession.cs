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
    public sealed class ClientSession<TPlayerData, TPacketizerContext> : AbstractSession<TPlayerData, TPacketizerContext>, IClientSession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Events

        /// <summary>
        /// Called when we receive information about an open game.
        /// Only possibly called after Search() was called.
        /// </summary>
        public event EventHandler<EventArgs> GameInfoReceived;

        /// <summary>
        /// Called when we successfully joined a server (i.e. Join() was
        /// called and the server accepted us).
        /// </summary>
        public event EventHandler<EventArgs> JoinResponse;

        #endregion

        #region Properties

        /// <summary>
        /// Current state of this session.
        /// </summary>
        public ClientState ConnectionState { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The actual host address of the game we're in / trying to join.
        /// </summary>
        private IPEndPoint host;

        #endregion

        #region Constructor

        public ClientSession(Game game, IProtocol protocol)
            : base(game, protocol)
        {
            ConnectionState = ClientState.Unconnected;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Send a ping into the local network, looking for open games.
        /// </summary>
        public void Search()
        {
            Packet packet = new Packet(1);
            packet.Write((byte)SessionMessage.GameInfoRequest);
            // Send as a multicast / broadcast.
            protocol.Send(packet, new IPEndPoint(DefaultMulticastAddress, DefaultMulticastPort), PacketPriority.None);
        }

        /// <summary>
        /// Join a game on the given host.
        /// </summary>
        /// <param name="remote">the remote host that runs the session.</param>
        /// <param name="playerName">the with which to register.</param>
        /// <param name="data">additional data to be associated with our player (Player.Data).</param>
        public void Join(IPEndPoint remote, string playerName, TPlayerData data)
        {
            if (ConnectionState == ClientState.Unconnected)
            {
                host = remote;
                Packet packet = new Packet();
                packet.Write(playerName);
                packet.Write(data);
                SendToEndPoint(host, SessionMessage.JoinRequest, packet, PacketPriority.Lowest);
                ConnectionState = ClientState.Connecting;
            }
            else
            {
                throw new InvalidOperationException("Must leave the current game first.");
            }
        }

        /// <summary>
        /// Leave the session.
        /// </summary>
        public void Leave()
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                SendToEndPoint(host, SessionMessage.Leave, null, 0);
            }

            ConnectionState = ClientState.Unconnected;
            playerAddresses = null;
            host = null;
            LocalPlayerNumber = 0;
            NumPlayers = 0;
            MaxPlayers = 0;
        }

        #endregion

        #region Internal send stuff

        /// <summary>
        /// Send some data of the given type to the server.
        /// </summary>
        /// <param name="type">the type of message to send.</param>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        internal override void SendToHost(SessionMessage type, Packet packet, PacketPriority priority)
        {
            SendToEndPoint(host, type, packet, priority);
        }

        #endregion

        #region Logic

        public override void Update(GameTime gameTime)
        {
            // Periodically send an acked no-op message, which is used to
            // check if the server is still alive.
            if (ConnectionState == ClientState.Connected &&
                (DateTime.Now - lastConnectionCheck).TotalMilliseconds > ConnectionCheckInterval)
            {
                lastConnectionCheck = DateTime.Now;
                SendToEveryone(SessionMessage.ConnectionTest, null, PacketPriority.Lowest);
            }

            base.Update(gameTime);
        }

        #endregion

        #region Event handling

        /// <summary>
        /// A client timed out, if it's the host this means we're out. If we're still
        /// connecting, this means the connection failed. If we're not connected, we
        /// don't even care.
        /// </summary>
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
                        Player<TPlayerData, TPacketizerContext> player = players[LocalPlayerNumber];
                        Leave();
                        OnPlayerLeft(new PlayerEventArgs<TPlayerData, TPacketizerContext>(player));
                    }
                } // else could not send to other client -> not so bad
            }
        }

        /// <summary>
        /// Some remote host send us data, let's see what to do with it.
        /// </summary>
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
                case SessionMessage.ConnectionTest:
                    // Server wants to know if we're still there.
                    if (ConnectionState == ClientState.Connected && args.Remote.Equals(host))
                    {
                        // Yep, still in a session, and this came from it's host. Allow acking.
                        args.Consume();
                    }
                    break;

                case SessionMessage.GameInfoResponse:
                    // Got some info on a running game.
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
                    // Got a reply from a server for a join response.
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
                            LocalPlayerNumber = data.ReadInt32();

                            // Get info about other players in the session.
                            NumPlayers = data.ReadInt32();

                            // Maximum number of players in the session?
                            MaxPlayers = data.ReadInt32();

                            // Sanity checks.
                            if (LocalPlayerNumber < 0 || NumPlayers < 0 || MaxPlayers < 0 || MaxPlayers < NumPlayers || LocalPlayerNumber >= MaxPlayers)
                            {
                                throw new PacketException("Inconsistent session info.");
                            }

                            // Allocate arrays for the players in the session.
                            playerAddresses = new IPEndPoint[MaxPlayers];
                            players = new Player<TPlayerData, TPacketizerContext>[MaxPlayers];

                            // Get info on players already in the session, including us.
                            for (int i = 0; i < NumPlayers; i++)
                            {
                                // Get player number.
                                int playerNumber = data.ReadInt32();

                                // Get player name.
                                string playerName = data.ReadString().Trim();

                                // Get additional player data.
                                TPlayerData playerData = new TPlayerData();
                                data.ReadPacketizable(playerData, packetizer.Context);

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
                                players[playerNumber] = new Player<TPlayerData, TPacketizerContext>(playerNumber, playerName, playerData,
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
                            foreach (var player in AllPlayers)
                            {
                                if (!player.Equals(LocalPlayer))
                                {
                                    OnPlayerJoined(new PlayerEventArgs<TPlayerData, TPacketizerContext>(player));
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
                    // Some player joined the session.
                    if (ConnectionState == ClientState.Connected && args.Remote.Equals(host))
                    {
                        try
                        {
                            // Get player number.
                            int playerNumber = data.ReadInt32();

                            // Get player name.
                            string playerName = data.ReadString().Trim();

                            // Get additional player data.
                            TPlayerData playerData = new TPlayerData();
                            data.ReadPacketizable(playerData, packetizer.Context);

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
                            players[playerNumber] = new Player<TPlayerData, TPacketizerContext>(playerNumber, playerName, playerData,
                                    delegate() { return protocol.GetPing(playerAddresses[playerNumber]); });

                            // The the local program about it.
                            OnPlayerJoined(new PlayerEventArgs<TPlayerData, TPacketizerContext>(players[playerNumber]));

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
                    // Some player left the session.
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

                            if (playerNumber == LocalPlayerNumber)
                            {
                                // We were removed from the game.
                                Leave();
                            }
                            else
                            {
                                // OK, remove the player.
                                Player<TPlayerData, TPacketizerContext> player = players[playerNumber];
                                players[playerNumber] = null;
                                playerAddresses[playerNumber] = null;

                                // Tell the local program about it.
                                OnPlayerLeft(new PlayerEventArgs<TPlayerData, TPacketizerContext>(player));
                            }
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
                    // Custom data, just forward it if we're in a session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        if (args.Remote.Equals(host))
                        {
                            OnPlayerData(new PlayerDataEventArgs<TPlayerData, TPacketizerContext>(null, args, data));
                        }
                        else
                        {
                            ConditionalOnPlayerData(args, data);
                        }
                    }
                    break;

                // Ignore everything else.
                default:
                    break;
            }
        }

        #endregion

        #region Utility methods

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

        #endregion
    }
}
