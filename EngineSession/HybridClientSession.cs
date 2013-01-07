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
        where TPlayerData : class, IPacketizable, new()
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
        /// Connection to the server shutting down.
        /// </summary>
        public event EventHandler<EventArgs> Disconnecting;

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
        public Player LocalPlayer
        {
            get { return GetPlayer(_localPlayerNumber); }
        }

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

        /// <summary>
        /// Reset was requested from async connection thread.
        /// </summary>
        private bool _resetRequested;

        #endregion

        #region Construction / Destruction

        public HybridClientSession()
            : base(new UdpProtocol(UdpHeader))
        {
            ConnectionState = ClientState.Unconnected;
            _localPlayerNumber = -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
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
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        public override void Update()
        {
            lock (this)
            {
                if (_resetRequested)
                {
                    Reset();
                }
                else if (_stream != null)
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
                                var type = (SessionMessage)packet.ReadByte();

                                // Statistics.
                                Info.PutIncomingTraffic(packet.Length,
                                                        type == SessionMessage.Data
                                                            ? TrafficTypes.Data
                                                            : TrafficTypes.Protocol);
                                Info.PutIncomingPacketSize(packet.Length);

                                using (var data = packet.ReadPacket())
                                {
                                    HandleTcpData(type, data);
                                }
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        // Connection failed, disconnect.
                        Logger.TraceException("Socket connection died.", ex);
                        Reset();
                    }
                    catch (PacketException ex)
                    {
                        // Received invalid packet from server.
                        Logger.WarnException("Invalid packet received from server.", ex);
                        Reset();
                    }
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
            Logger.Trace("Sending ping to search for open games.");
            using (var packet = new Packet())
            {
                packet.Write((byte)SessionMessage.GameInfoRequest);
                Udp.Send(packet, DefaultMulticastEndpoint);
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
            Logger.Debug("Begin connecting to host at '{0}'.", remote);
            _playerName = playerName;
            _playerData = (TPlayerData)playerData;
            ConnectionState = ClientState.Connecting;
            _tcp = new TcpClient {NoDelay = true};
#if DEBUG
            // This is for local loopback connections so they don't block when sending,
            // the game state dump. Meaning for local testing only.
            _tcp.SendBufferSize = 50000000;
#endif
            _tcp.BeginConnect(remote.Address, remote.Port, HandleConnected, _tcp);
        }

        /// <summary>
        /// Join a local game.
        /// </summary>
        /// <param name="server">the local server to join.</param>
        /// <param name="playerName">the name with which to register.</param>
        /// <param name="playerData">additional data to be associated with our player.</param>
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
            Logger.Debug("Begin connecting to local server.");
            _playerName = playerName;
            _playerData = (TPlayerData)playerData;
            ConnectionState = ClientState.Connecting;

            // Check if the server is already full.
            if (server.NumPlayers >= server.MaxPlayers)
            {
                Logger.Debug("Join failed, server already full.");
                Reset();
                return;
            }

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
                packetInner.
                    Write(_playerName).
                    Write(_playerData);
                packet.
                    Write((byte)SessionMessage.JoinRequest).
                    Write(packetInner);
                var written = _stream.Write(packet);

                // Statistics.
                Info.PutOutgoingTraffic(written, TrafficTypes.Protocol);
                Info.PutOutgoingPacketSize(written);
                Info.PutOutgoingPacketCompression((packet.Length / (float)written) - 1f);
            }

            // Let's try this. This can throw if the server is already
            // full, but that shouldn't happen, because we checked above.
            ((HybridServerSession<TPlayerData>)server).
                Add(new SlidingPacketStream(toServer, toClient));
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
        protected override void Send(SessionMessage type, IWritablePacket packet = null)
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
                    var written = _stream.Write(wrapper);

                    // Statistics.
                    Info.PutOutgoingTraffic(written, type == SessionMessage.Data
                                                         ? TrafficTypes.Data
                                                         : TrafficTypes.Protocol);
                    Info.PutOutgoingPacketSize(written);
                    if (packet != null)
                    {
                        Info.PutOutgoingPacketCompression((packet.Length / (float)written) - 1f);
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.TraceException("Socket connection died.", ex);
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
            lock (this)
            {
                if (_tcp == null)
                {
                    return;
                }
                try
                {
                    Logger.Debug("Connected to host, sending actual join request.");
                    _tcp.EndConnect(result);
                    _stream = new EncryptedPacketStream(new CompressedPacketStream(new NetworkPacketStream(_tcp.GetStream())));
                    using (var packet = new Packet())
                    using (var packetInner = new Packet())
                    {
                        packetInner.
                            Write(_playerName).
                            Write(_playerData);
                        packet.
                            Write((byte)SessionMessage.JoinRequest).
                            Write(packetInner);
                        var written = _stream.Write(packet);

                        // Statistics.
                        Info.PutOutgoingTraffic(written, TrafficTypes.Protocol);
                        Info.PutOutgoingPacketSize(written);
                        Info.PutOutgoingPacketCompression((packet.Length / (float)written) - 1f);
                    }
                }
                catch (SocketException ex)
                {
                    // Connection failed.
                    Logger.DebugException("Join failed.", ex);
                    _resetRequested = true;
                }
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
                Logger.Warn("Received invalid packet, no SessionMessage type.");
                return;
            }
            var type = (SessionMessage)e.Data.ReadByte();

            // Get additional data.
            using (var packet = e.Data.ReadPacket())
            {
                switch (type)
                {
                    case SessionMessage.GameInfoResponse:
                        // Got some info on a running game. We only care if we're not in a game.
                        if (ConnectionState == ClientState.Unconnected)
                        {
                            if (packet == null)
                            {
                                // Bad data.
                                Logger.Warn("Invalid GameInfoResponse (no data).");
                            }
                            else
                            {
                                try
                                {
                                    // Get number of max players.
                                    var maxPlayers = packet.ReadInt32();

                                    // Get number of current players.
                                    var numPlayers = packet.ReadInt32();

                                    // Get additional data.
                                    using (var customData = packet.ReadPacket())
                                    {
                                        Logger.Trace(
                                            "Got game info from host '{0}': {1}/{2} players, data of length {3}.",
                                            e.RemoteEndPoint, numPlayers, maxPlayers, customData.Length);

                                        // Propagate to local program.
                                        OnGameInfoReceived(new GameInfoReceivedEventArgs(e.RemoteEndPoint, numPlayers,
                                                                                         maxPlayers, customData));
                                    }
                                }
                                catch (PacketException ex)
                                {
                                    // Bad data.
                                    Logger.WarnException("Invalid GameInfoResponse.", ex);
                                }
                            }
                        }
                        break;

                    case SessionMessage.Data:
                        // Custom data, just forward it if we're in a session.
                        if (ConnectionState == ClientState.Connected)
                        {
                            try
                            {
                                OnData(new ClientDataEventArgs(
                                           packet, _tcp != null && Equals(e.RemoteEndPoint, _tcp.Client.RemoteEndPoint)));
                            }
                            catch (PacketException ex)
                            {
                                Logger.WarnException("Invalid Data.", ex);
                            }
                        }
                        break;

                        // Nothing else is handled via UDP.
                    default:
                        Logger.Debug("Unknown SessionMessage via UDP: {0}.", type);
                        break;
                }
            }
        }

        /// <summary>
        /// Got some data via our TCP connection, so it's from the server.
        /// </summary>
        private void HandleTcpData(SessionMessage type, IReadablePacket packet)
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
                        if (_localPlayerNumber < 0 || NumPlayers < 0 || MaxPlayers < 0 || MaxPlayers < NumPlayers ||
                            _localPlayerNumber >= MaxPlayers)
                        {
                            throw new PacketException("Inconsistent session info.");
                        }

                        // Allocate array for the players in the session.
                        Players = new Player[MaxPlayers];

                        // Get other game relevant data.
                        using (var joinData = packet.ReadPacket())
                        {
                            // Get info on players already in the session, including us.
                            for (var i = 0; i < NumPlayers; i++)
                            {
                                // Get player number.
                                var playerNumber = packet.ReadInt32();

                                // Sanity checks.
                                if (playerNumber < 0 || playerNumber >= MaxPlayers || Players[playerNumber] != null)
                                {
                                    throw new PacketException("Invalid player number.");
                                }

                                // Get player name.
                                var playerName = packet.ReadString();

                                // Get additional player data.
                                var playerData = packet.ReadPacketizable<TPlayerData>();

                                // All OK, add the player.
                                Players[playerNumber] = new Player(playerNumber, playerName, playerData);
                            }

                            // New state :)
                            ConnectionState = ClientState.Connected;

                            if (_tcp != null)
                            {
                                Logger.Debug("Successfully joined game at '{0}'.",
                                             (IPEndPoint)_tcp.Client.RemoteEndPoint);
                            }
                            else
                            {
                                Logger.Debug("Successfully joined local game.");
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
                        var playerNumber = packet.ReadInt32();

                        // Sanity checks.
                        if (playerNumber < 0 || playerNumber >= MaxPlayers || Players[playerNumber] != null)
                        {
                            throw new PacketException("Invalid player number.");
                        }

                        // Get player name.
                        var playerName = packet.ReadString();

                        // Get additional player data.
                        var playerData = packet.ReadPacketizable<TPlayerData>();

                        // All OK, add the player.
                        Players[playerNumber] = new Player(playerNumber, playerName, playerData);

                        // The the local program about it.
                        OnPlayerJoined(new PlayerEventArgs(Players[playerNumber]));
                    }
                    break;

                case SessionMessage.PlayerLeft:
                    // Some player left the session.
                    if (ConnectionState == ClientState.Connected)
                    {
                        // Get player number.
                        var playerNumber = packet.ReadInt32();

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
                            var player = Players[playerNumber];
                            Players[playerNumber] = null;

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

                default:
                    // Ignore everything else.
                    Logger.Trace("Unknown SessionMessage via TCP: {0}.", type);
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
            _resetRequested = false;

            if (ConnectionState != ClientState.Unconnected)
            {
                lock (this)
                {
                    Logger.Debug("Resetting session.");

                    OnDisconnecting(EventArgs.Empty);

                    Players = null;
                    _localPlayerNumber = -1;
                    NumPlayers = 0;
                    MaxPlayers = 0;

                    if (_stream != null)
                    {
                        try
                        {
                            _stream.Flush();
                        }
                        catch (IOException ex)
                        {
                            Logger.WarnException("Failed flushing stream while closing connection.", ex);
                        }
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

        private void OnDisconnecting(EventArgs e)
        {
            if (Disconnecting != null)
            {
                Disconnecting(this, e);
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