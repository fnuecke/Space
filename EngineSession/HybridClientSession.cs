using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Engine.IO;
using Engine.Network;
using Engine.Serialization;

namespace Engine.Session
{
    public sealed class HybridClientSession<TPlayerData> : AbstractHybridSession, IClientSession
        where TPlayerData : IPacketizable, new()
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Events

        /// <summary>
        /// Called when we receive information about an open game.
        /// Only possibly called after Search() was called.
        /// </summary>
        public event EventHandler<GameInfoReceivedEventArgs> GameInfoReceived;

        /// <summary>
        /// Called when we got a response to our <c>Join</c> call, either
        /// successfully or not.
        /// </summary>
        public event EventHandler<JoinResponseEventArgs> JoinResponse;

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
        public Player LocalPlayer { get { return GetPlayer(_localPlayerNumber); } }

        #endregion

        #region Fields

        /// <summary>
        /// The connection to the server, used to (reliably) receive data.
        /// </summary>
        private TcpClient _tcp;

        /// <summary>
        /// The packet stream used to send packets to and receive packets from the server.
        /// </summary>
        private IPacketStream _stream;

        /// <summary>
        /// Number of the local player.
        /// </summary>
        private int _localPlayerNumber;

        /// <summary>
        /// The name of the local player (remembered from join for actual join request).
        /// </summary>
        private string _playerName;

        /// <summary>
        /// Player data (as for name, remembered for actual request).
        /// </summary>
        private TPlayerData _playerData;

        #endregion

        #region Construction / Destruction

        public HybridClientSession()
            : base(new UdpProtocol(_udpHeader))
        {
            ConnectionState = ClientState.Unconnected;
            _localPlayerNumber = -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_stream != null)
                {
                    _stream.Dispose();
                }

                if (_tcp != null)
                {
                    _tcp.Close();
                }

                ConnectionState = ClientState.Unconnected;

                _tcp = null;
                _stream = null;
                _localPlayerNumber = -1;
                _playerData = default(TPlayerData);
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        public override void Update()
        {
            if (_stream != null)
            {
                try
                {
                    while (_stream != null)
                    {
                        using (var packet = _stream.Read())
                        {
                            if (packet == null)
                            {
                                break;
                            }
                            else
                            {
                                SessionMessage type = (SessionMessage)packet.ReadByte();

                                // Statistics.
                                _information.PutIncomingTraffic(packet.Length,
                                    type == SessionMessage.Data
                                    ? TrafficTypes.Data
                                    : TrafficTypes.Protocol);
                                _information.PutIncomingPacketSize(packet.Length);

                                using (var data = packet.ReadPacket())
                                {
                                    HandleTcpData(type, data);
                                }
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    // Connection failed, disconnect.
                    logger.TraceException("Socket connection died.", ex);
                    Reset();
                }
                catch (PacketException ex)
                {
                    // Received invalid packet from server.
                    logger.WarnException("Invalid packet received from server.", ex);
                    Reset();
                }
            }

            base.Update();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Send a ping into the local network, looking for open games.
        /// </summary>
        public void Search()
        {
            // Send as a multicast / broadcast.
            logger.Trace("Sending ping to search for open games.");
            using (var packet = new Packet())
            {
                _udp.Send(packet.Write((byte)SessionMessage.GameInfoRequest), _defaultMulticastEndpoint);
            }
        }

        /// <summary>
        /// Join a game on the given host.
        /// </summary>
        /// <param name="remote">the remote host that runs the session.</param>
        /// <param name="playerName">the name with which to register.</param>
        /// <param name="playerData">additional data to be associated with our player.</param>
        public void Join(IPEndPoint remote, string playerName, IPacketizable playerData)
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                throw new InvalidOperationException("Must leave the current session first.");
            }
            logger.Debug("Begin connecting to host at '{0}'.", remote);
            this._playerName = playerName;
            this._playerData = (TPlayerData)playerData;
            ConnectionState = ClientState.Connecting;
            _tcp = new TcpClient();
            _tcp.NoDelay = true;
            _tcp.BeginConnect(remote.Address, remote.Port, new AsyncCallback(HandleConnected), _tcp);
        }

        /// <summary>
        /// Join a local game.
        /// </summary>
        /// <param name="server">the local server to join.</param>
        /// <param name="playerName">the name with which to register.</param>
        /// <param name="data">additional data to be associated with our player.</param>
        public void Join(IServerSession server, string playerName, IPacketizable playerData)
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                throw new InvalidOperationException("Must leave the current session first.");
            }
            if (!(server is HybridServerSession<TPlayerData>))
            {
                throw new InvalidOperationException("Incompatible server type.");
            }
            logger.Debug("Begin connecting to local server.");
            this._playerName = playerName;
            this._playerData = (TPlayerData)playerData;
            ConnectionState = ClientState.Connecting;

            // Create the two 'pipes' we use to pass data from client to server
            // and vice versa.
            var toClient = new SlidingStream();
            var toServer = new SlidingStream();

            // Our stream is the one where the sink is the server.
            // The server gets one in the other direction (see below).
            _stream = new SlidingPacketStream(toClient, toServer);
            using (var packet = new Packet())
            using (var packetInner = new Packet())
            {
                packet.
                    Write((byte)SessionMessage.JoinRequest).
                    Write(packetInner.
                        Write(_playerName).
                        Write(_playerData));
                int written = _stream.Write(packet);

                // Statistics.
                _information.PutOutgoingTraffic(written, TrafficTypes.Protocol);
                _information.PutOutgoingPacketSize(written);
                _information.PutOutgoingPacketCompression(((float)packet.Length / (float)written) - 1f);
            }
            try
            {
                // Let's try this... this can throw if the server is already full.
                ((HybridServerSession<TPlayerData>)server).
                    Add(new SlidingPacketStream(toServer, toClient));
            }
            catch (InvalidOperationException ex)
            {
                logger.DebugException("Join failed.", ex);
                Reset();
            }
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
            if (ConnectionState == ClientState.Connected)
            {
                Send(SessionMessage.Leave);
            }
            Reset();
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
                throw new InvalidOperationException("Not in a session.");
            }
            try
            {
                using (var wrapper = new Packet())
                {
                    wrapper.Write((byte)type).Write(packet);
                    int written = _stream.Write(wrapper);

                    // Statistics.
                    _information.PutOutgoingTraffic(written,
                        type == SessionMessage.Data
                        ? TrafficTypes.Data
                        : TrafficTypes.Protocol);
                    _information.PutOutgoingPacketSize(written);
                    _information.PutOutgoingPacketCompression(((float)packet.Length / (float)written) - 1f);
                }
            }
            catch (IOException ex)
            {
                logger.TraceException("Socket connection died.", ex);
                Reset();
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Connected successfully to the server, send login data.
        /// </summary>
        private void HandleConnected(IAsyncResult result)
        {
            if (_tcp == null)
            {
                return;
            }
            try
            {
                logger.Debug("Connected to host, sending actual join request.");
                _tcp.EndConnect(result);
                _stream = new EncryptedPacketStream(new CompressedPacketStream(new NetworkPacketStream(_tcp.GetStream())));
                using (var packet = new Packet())
                using (var packetInner = new Packet())
                {
                    packet.
                        Write((byte)SessionMessage.JoinRequest).
                        Write(packetInner.
                            Write(_playerName).
                            Write(_playerData));
                    int written = _stream.Write(packet);

                    // Statistics.
                    _information.PutOutgoingTraffic(written, TrafficTypes.Protocol);
                    _information.PutOutgoingPacketSize(written);
                    _information.PutOutgoingPacketCompression(((float)packet.Length / (float)written) - 1f);
                }
            }
            catch (SocketException ex)
            {
                // Connection failed.
                logger.DebugException("Join failed.", ex);
                Reset();
            }
        }

        /// <summary>
        /// Received some data from a client, let's see what we got.
        /// </summary>
        protected override void HandleUdpData(object sender, ProtocolDataEventArgs e)
        {
            // Get the message type.
            if (!e.Data.HasByte())
            {
                logger.Warn("Received invalid packet, no SessionMessage type.");
                return;
            }
            SessionMessage type = (SessionMessage)e.Data.ReadByte();

            // Get additional data.
            using (var packet = e.Data.HasPacket() ? e.Data.ReadPacket() : null)
            {
                switch (type)
                {
                    case SessionMessage.GameInfoResponse:
                        // Got some info on a running game. We only care if we're not in a game.
                        if (ConnectionState == ClientState.Unconnected)
                        {
                            try
                            {
                                // Get number of max players.
                                int maxPlayers = packet.ReadInt32();

                                // Get number of current players.
                                int numPlayers = packet.ReadInt32();

                                // Get additional data.
                                using (var customData = packet.ReadPacket())
                                {
                                    logger.Trace("Got game info from host '{0}': {1}/{2} players, data of length {3}.",
                                        e.RemoteEndPoint, numPlayers, maxPlayers, customData.Length);

                                    // Propagate to local program.
                                    OnGameInfoReceived(new GameInfoReceivedEventArgs(e.RemoteEndPoint, numPlayers, maxPlayers, customData));
                                }
                            }
                            catch (PacketException ex)
                            {
                                // Bad data.
                                logger.WarnException("Invalid GameInfoResponse.", ex);
                            }
                        }
                        break;

                    case SessionMessage.Data:
                        // Custom data, just forward it if we're in a session.
                        if (ConnectionState == ClientState.Connected)
                        {
                            try
                            {
                                OnData(new ClientDataEventArgs(packet, _tcp != null && e.RemoteEndPoint == _tcp.Client.RemoteEndPoint));
                            }
                            catch (PacketException ex)
                            {
                                logger.WarnException("Invalid Data.", ex);
                            }
                        }
                        break;

                    // Nothing else is handled via UDP.
                    default:
                        logger.Debug("Unknown SessionMessage via UDP: {0}.", type);
                        break;
                }
            }
        }

        /// <summary>
        /// Got some data via our TCP connection, so it's from the server.
        /// </summary>
        private void HandleTcpData(SessionMessage type, Packet packet)
        {
            switch (type)
            {
                case SessionMessage.JoinResponse:
                    // Got a reply from a server for a join response.
                    if (ConnectionState == ClientState.Connecting)
                    {
                        // Get our number.
                        _localPlayerNumber = packet.ReadInt32();

                        // Get info about other players in the session.
                        NumPlayers = packet.ReadInt32();

                        // Maximum number of players in the session?
                        MaxPlayers = packet.ReadInt32();

                        // Sanity checks.
                        if (_localPlayerNumber < 0 || NumPlayers < 0 || MaxPlayers < 0 || MaxPlayers < NumPlayers || _localPlayerNumber >= MaxPlayers)
                        {
                            throw new PacketException("Inconsistent session info.");
                        }

                        // Allocate array for the players in the session.
                        _players = new Player[MaxPlayers];

                        // Get other game relevant data (e.g. game state).
                        using (var joinData = packet.ReadPacket())
                        {
                            // Get info on players already in the session, including us.
                            for (int i = 0; i < NumPlayers; i++)
                            {
                                // Get player number.
                                int playerNumber = packet.ReadInt32();

                                // Sanity checks.
                                if (playerNumber < 0 || playerNumber >= MaxPlayers || _players[playerNumber] != null)
                                {
                                    throw new PacketException("Invalid player number.");
                                }

                                // Get player name.
                                string playerName = packet.ReadString();

                                // Get additional player data.
                                var playerData = packet.ReadPacketizable<TPlayerData>();

                                // All OK, add the player.
                                _players[playerNumber] = new Player(playerNumber, playerName, playerData);
                            }

                            // New state :)
                            ConnectionState = ClientState.Connected;

                            if (_tcp != null)
                            {
                                logger.Debug("Successfully joined game at '{0}'.", (IPEndPoint)_tcp.Client.RemoteEndPoint);
                            }
                            else
                            {
                                logger.Debug("Successfully joined local game.");
                            }

                            // OK, let the program know.
                            OnJoinResponse(new JoinResponseEventArgs(joinData));
                        }

                        // Also, fire one join event for each player in the game. Except for
                        // the local player, because that'll likely need special treatment anyway.
                        foreach (var player in AllPlayers)
                        {
                            if (!player.Equals(LocalPlayer))
                            {
                                OnPlayerJoined(new PlayerEventArgs(player));
                            }
                        }
                    }
                    break;

                case SessionMessage.PlayerJoined:
                    // Some player joined the session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        // Get player number.
                        int playerNumber = packet.ReadInt32();

                        // Sanity checks.
                        if (playerNumber < 0 || playerNumber >= MaxPlayers || _players[playerNumber] != null)
                        {
                            throw new PacketException("Invalid player number.");
                        }

                        // Get player name.
                        string playerName = packet.ReadString();

                        // Get additional player data.
                        TPlayerData playerData = packet.ReadPacketizable<TPlayerData>();

                        // All OK, add the player.
                        _players[playerNumber] = new Player(playerNumber, playerName, playerData);

                        // The the local program about it.
                        OnPlayerJoined(new PlayerEventArgs(_players[playerNumber]));
                    }
                    break;

                case SessionMessage.PlayerLeft:
                    // Some player left the session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        // Get player number.
                        int playerNumber = packet.ReadInt32();

                        // Sanity checks.
                        if (!HasPlayer(playerNumber))
                        {
                            throw new PacketException("Invalid player number.");
                        }

                        if (playerNumber == _localPlayerNumber)
                        {
                            // We were removed from the game.
                            Reset();
                        }
                        else
                        {
                            // OK, remove the player.
                            Player player = _players[playerNumber];
                            _players[playerNumber] = null;

                            // Tell the local program about it.
                            OnPlayerLeft(new PlayerEventArgs(player));
                        }
                    }
                    break;

                case SessionMessage.Data:
                    // Custom data, just forward it if we're in a session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        OnData(new ClientDataEventArgs(packet, true));
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
        private void Reset()
        {
            if (ConnectionState != ClientState.Unconnected)
            {
                logger.Debug("Resetting session.");

                _players = null;
                _localPlayerNumber = -1;
                NumPlayers = 0;
                MaxPlayers = 0;

                if (_stream != null)
                {
                    _stream.Dispose();
                }
                if (_tcp != null)
                {
                    _tcp.Close();
                }
                _stream = null;
                _tcp = null;

                ConnectionState = ClientState.Unconnected;

                OnDisconnected(EventArgs.Empty);
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
