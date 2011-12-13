using System;
using System.Net;
using System.Net.Sockets;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    public sealed class HybridClientSession<TPlayerData, TPacketizerContext>
        : AbstractHybridSession<TPlayerData, TPacketizerContext>, IClientSession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
#region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

#endregion

        #region Events

        /// <summary>
        /// Called when we receive information about an open game.
        /// Only possibly called after Search() was called.
        /// </summary>
        public event EventHandler<EventArgs> GameInfoReceived;

        /// <summary>
        /// Called when we got a response to our <c>Join</c> call, either
        /// successfully or not.
        /// </summary>
        public event EventHandler<EventArgs> JoinResponse;

        /// <summary>
        /// Connection to the server was lost.
        /// </summary>
        public event EventHandler<EventArgs> Disconnected;

        #endregion

        #region Properties

        /// <summary>
        /// Current state of this session.
        /// </summary>
        public ClientState ConnectionState { get; private set; }

        /// <summary>
        /// Reference to the data struct with info about the local player.
        /// </summary>
        public Player<TPlayerData, TPacketizerContext> LocalPlayer { get { return GetPlayer(localPlayerNumber); } }

        #endregion

        #region Fields

        /// <summary>
        /// The connection to the server, used to (reliably) receive data.
        /// </summary>
        private TcpClient tcp;

        /// <summary>
        /// The packet stream used to send packets to and receive packets from the server.
        /// </summary>
        private PacketStream stream;

        /// <summary>
        /// Number of the local player.
        /// </summary>
        private int localPlayerNumber;

        /// <summary>
        /// The name of the local player (remembered from join for actual join request).
        /// </summary>
        private string playerName;

        /// <summary>
        /// Player data (as for name, remembered for actual request).
        /// </summary>
        private TPlayerData playerData;

        #endregion

        #region Construction / Destruction

        public HybridClientSession(Game game)
            : base(game)
        {
            udp = new UdpProtocol(0, udpHeader);
            ConnectionState = ClientState.Unconnected;
            localPlayerNumber = -1;
        }

        #endregion
        
        #region Logic

        public override void Update(GameTime gameTime)
        {
            if (stream != null)
            {
                try
                {
                    Packet packet;
                    while ((packet = stream.Read()) != null)
                    {
                        SessionMessage type = (SessionMessage)packet.ReadByte();
                        HandleTcpData(type, packet.ReadPacket());
                    }
                }
                catch (SocketException)
                {
                    // Connection failed, disconnect.
                    Reset(true);
                }
                catch (PacketException)
                {
                    // Received invalid packet from server.
                    Reset(true);
                }
            }

            base.Update(gameTime);
        }

        #endregion

        #region Public API
        
        /// <summary>
        /// Send a ping into the local network, looking for open games.
        /// </summary>
        public void Search()
        {
            // Send as a multicast / broadcast.
            udp.Send(new Packet(1).Write((byte)SessionMessage.GameInfoRequest), DefaultMulticastEndpoint);
        }

        /// <summary>
        /// Join a game on the given host.
        /// </summary>
        /// <param name="remote">the remote host that runs the session.</param>
        /// <param name="playerName">the with which to register.</param>
        /// <param name="playerData">additional data to be associated with our player (Player.Data).</param>
        public void Join(IPEndPoint remote, string playerName, TPlayerData playerData)
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                throw new InvalidOperationException("Must leave the current session first.");
            }
            ConnectionState = ClientState.Connecting;
            this.playerName = playerName;
            this.playerData = playerData;
            tcp = new TcpClient();
            tcp.BeginConnect(remote.Address, remote.Port, new AsyncCallback(HandleConnected), tcp);
        }

        /// <summary>
        /// Leave the session.
        /// </summary>
        public void Leave()
        {
            if (ConnectionState == ClientState.Unconnected)
            {
                throw new InvalidOperationException("Not in a session.");
            }
            Send(SessionMessage.Leave);
            Reset(true);
        }

        #endregion

        #region Sending

        /// <summary>
        /// Sends a message of the specified type, with the specified data.
        /// </summary>
        /// <param name="type">The type of the message to send.</param>
        /// <param name="packet">The data to send.</param>
        protected override void Send(SessionMessage type, Packet packet)
        {
            if (ConnectionState != ClientState.Connected)
            {
                throw new InvalidOperationException();
            }
            try
            {
                stream.Write(new Packet().
                    Write((byte)type).
                    Write(packet));
            }
            catch (SocketException)
            {
                Reset(true);
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Connected successfully to the server, send login data.
        /// </summary>
        private void HandleConnected(IAsyncResult result)
        {
            try
            {
                tcp.EndConnect(result);
                stream = new PacketStream(tcp.GetStream());
                stream.Write(new Packet().
                    Write((byte)SessionMessage.JoinRequest).
                    Write(playerName).
                    Write(playerData));
            }
            catch (Exception)
            {
                // Connection failed.
                Reset(true);
            }
        }

        /// <summary>
        /// Received some data from a client, let's see what we got.
        /// </summary>
        protected override void HandleUdpData(object sender, EventArgs e)
        {
            var args = (ProtocolDataEventArgs)e;

            // Get the message type.
            if (!args.Data.HasByte())
            {
                logger.Warn("Received invalid packet, no SessionMessage type.");
                return;
            }
            SessionMessage type = (SessionMessage)args.Data.ReadByte();

            // Get additional data.
            Packet data = args.Data.HasPacket() ? args.Data.ReadPacket() : null;

            switch (type)
            {
                case SessionMessage.GameInfoResponse:
                    // Got some info on a running game. We only care if we're not in a game.
                    if (ConnectionState == ClientState.Unconnected)
                    {
                        try
                        {
                            // Get number of max players.
                            int maxPlayers = data.ReadInt32();

                            // Get number of current players.
                            int numPlayers = data.ReadInt32();

                            // Get additional data.
                            Packet customData = data.ReadPacket();

                            // Propagate to local program.
                            OnGameInfoReceived(new GameInfoReceivedEventArgs(args.RemoteEndPoint, numPlayers, maxPlayers, customData));
                        }
                        catch (PacketException ex)
                        {
                            logger.WarnException("Invalid GameInfoResponse.", ex);
                        }
                    }
                    break;

                // Nothing else is handled via UDP.
                default:
                    logger.Trace("Unknown SessionMessage via UDP: {0}.", type);
                    break;
            }
        }

        private void HandleTcpData(SessionMessage type, Packet data)
        {
            switch (type)
            {
                case SessionMessage.JoinResponse:
                    // Got a reply from a server for a join response.
                    if (ConnectionState == ClientState.Connecting)
                    {
                        try
                        {
                            // Success or not?
                            if (!data.ReadBoolean())
                            {
                                // Joining failed :(
                                Reset();
                                OnJoinResponse(new JoinResponseEventArgs(false, (JoinResponseReason)data.ReadByte(), null));
                                return;
                            }

                            // Get our number.
                            localPlayerNumber = data.ReadInt32();

                            // Get info about other players in the session.
                            NumPlayers = data.ReadInt32();

                            // Maximum number of players in the session?
                            MaxPlayers = data.ReadInt32();

                            // Sanity checks.
                            if (localPlayerNumber < 0 || NumPlayers < 0 || MaxPlayers < 0 || MaxPlayers < NumPlayers || localPlayerNumber >= MaxPlayers)
                            {
                                throw new PacketException("Inconsistent session info.");
                            }

                            // Allocate array for the players in the session.
                            players = new Player<TPlayerData, TPacketizerContext>[MaxPlayers];

                            // Get info on players already in the session, including us.
                            for (int i = 0; i < NumPlayers; i++)
                            {
                                // Get player number.
                                int playerNumber = data.ReadInt32();

                                // Sanity checks.
                                if (playerNumber < 0 || playerNumber >= MaxPlayers || players[playerNumber] != null)
                                {
                                    throw new PacketException("Invalid player number.");
                                }

                                // Get player name.
                                string playerName = data.ReadString();

                                // Get additional player data.
                                TPlayerData playerData = new TPlayerData();
                                data.ReadPacketizable(playerData, packetizer.Context);

                                // All OK, add the player.
                                players[playerNumber] = new Player<TPlayerData, TPacketizerContext>(playerNumber, playerName, playerData);
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
                        }
                        catch (PacketException ex)
                        {
                            logger.WarnException("Invalid JoinResponse.", ex);
                            Reset();
                            OnJoinResponse(new JoinResponseEventArgs(false, JoinResponseReason.InvalidServerData, null));
                        }
                    }
                    break;

                case SessionMessage.PlayerJoined:
                    // Some player joined the session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        try
                        {
                            // Get player number.
                            int playerNumber = data.ReadInt32();

                            // Sanity checks.
                            if (playerNumber < 0 || playerNumber >= MaxPlayers || players[playerNumber] != null)
                            {
                                throw new PacketException("Invalid player number.");
                            }

                            // Get player name.
                            string playerName = data.ReadString();

                            // Get additional player data.
                            TPlayerData playerData = new TPlayerData();
                            data.ReadPacketizable(playerData, packetizer.Context);

                            // All OK, add the player.
                            players[playerNumber] = new Player<TPlayerData, TPacketizerContext>(playerNumber, playerName, playerData);

                            // The the local program about it.
                            OnPlayerJoined(new PlayerEventArgs<TPlayerData, TPacketizerContext>(players[playerNumber]));
                        }
                        catch (PacketException ex)
                        {
                            logger.WarnException("Invalid PlayerJoined.", ex);
                            Reset(true);
                        }
                    }
                    break;

                case SessionMessage.PlayerLeft:
                    // Some player left the session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        try
                        {
                            // Get player number.
                            int playerNumber = data.ReadInt32();

                            // Sanity checks.
                            if (!HasPlayer(playerNumber))
                            {
                                throw new PacketException("Invalid player number.");
                            }

                            if (playerNumber == localPlayerNumber)
                            {
                                // We were removed from the game.
                                Reset(true);
                            }
                            else
                            {
                                // OK, remove the player.
                                Player<TPlayerData, TPacketizerContext> player = players[playerNumber];
                                players[playerNumber] = null;

                                // Tell the local program about it.
                                OnPlayerLeft(new PlayerEventArgs<TPlayerData, TPacketizerContext>(player));
                            }
                        }
                        catch (PacketException ex)
                        {
                            logger.WarnException("Invalid PlayerLeft.", ex);
                            Reset(true);
                        }
                    }
                    break;

                case SessionMessage.Data:
                    // Custom data, just forward it if we're in a session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        try
                        {
                            int playerNumber = data.ReadInt32();
                            OnPlayerData(new PlayerDataEventArgs<TPlayerData, TPacketizerContext>(GetPlayer(playerNumber), data, true));
                        }
                        catch (PacketException ex)
                        {
                            logger.WarnException("Invalid Data.", ex);
                            Reset(true);
                        }
                    }
                    break;

                // Ignore everything else.
                default:
                    logger.Trace("Unknown SessionMessage via TCP: {0}.", type);
                    break;
            }
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Resets the connection to its unconnected, initial state.
        /// </summary>
        private void Reset(bool fireEvent = false)
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                players = null;
                localPlayerNumber = -1;
                NumPlayers = 0;
                MaxPlayers = 0;

                if (ConnectionState == ClientState.Connected)
                {
                    stream.Dispose();
                    tcp.Close();
                }
                else if (ConnectionState == ClientState.Connecting)
                {
                    tcp.GetStream().Close();
                    tcp.Close();
                }
                stream = null;
                tcp = null;

                ConnectionState = ClientState.Unconnected;

                if (fireEvent)
                {
                    OnDisconnected(EventArgs.Empty);
                }
            }
        }

        #endregion

        #region Event Dispatching

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

        private void OnDisconnected(EventArgs e)
        {
            if (Disconnected != null)
            {
                Disconnected(this, e);
            }
        }

        #endregion
    }
}
