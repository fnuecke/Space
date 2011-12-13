using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    public sealed class HybridServerSession<TPlayerData, TPacketizerContext>
        : AbstractHybridSession<TPlayerData, TPacketizerContext>, IServerSession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Events

        /// <summary>
        /// Called when an unconnected client requests game info.
        /// </summary>
        public event EventHandler<EventArgs> GameInfoRequested;

        /// <summary>
        /// A player is joining the game. Fill in any arbitrary data to send
        /// back to the joining client here.
        /// </summary>
        public event EventHandler<EventArgs> JoinRequested;

        #endregion

        #region Fields

        /// <summary>
        /// The connection to the server, used to (reliably) receive data.
        /// </summary>
        private TcpListener _tcp;

        /// <summary>
        /// The list of TCP connections to the connected clients.
        /// </summary>
        private TcpClient[] _clients;

        /// <summary>
        /// The packet streams used for the different clients.
        /// </summary>
        private PacketStream[] _streams;

        /// <summary>
        /// Keep track of free slots (use the first free on on joins).
        /// </summary>
        private BitArray _slots;

        #endregion

        #region Constructor / Cleanup

        public HybridServerSession(Game game, ushort port, int maxPlayers)
            : base(game)
        {
            if (maxPlayers < 0)
            {
                throw new ArgumentException("maxPlayers");
            }

            _tcp = new TcpListener(IPAddress.Any, port);
            _tcp.Start();
            udp = new UdpProtocol(0, udpHeader, DefaultMulticastEndpoint.Address);

            this.MaxPlayers = (int)maxPlayers;
            players = new Player<TPlayerData, TPacketizerContext>[maxPlayers];
            this._clients = new TcpClient[maxPlayers];
            this._streams = new PacketStream[maxPlayers];
            _slots = new BitArray(maxPlayers, false);

            logger.Trace("Started new server session on port {0}.", port);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tcp.Stop();
                _tcp.Server.Close();

                for (int i = 0; i < MaxPlayers; ++i)
                {
                    if (_slots[i])
                    {
                        try { _clients[i].Client.Shutdown(SocketShutdown.Both); }
                        catch (Exception) { }
                        _streams[i].Dispose();
                        _clients[i].Close();

                        _streams[i] = null;
                        _clients[i] = null;
                        _slots[i] = false;
                    }
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        public override void Update(GameTime gameTime)
        {
            // Check for incoming connections.
            while (NumPlayers < MaxPlayers && _tcp.Pending())
            {
                TcpClient client = _tcp.AcceptTcpClient();
                PacketStream stream = new PacketStream(client.GetStream());

                // Do not allow connections from the same IP twice, to avoid
                // ambivalence when getting UDP data packets.
                if (Array.FindIndex(_clients, c => c != null && ((IPEndPoint)c.Client.RemoteEndPoint).Address.Equals(((IPEndPoint)client.Client.RemoteEndPoint).Address)) >= 0)
                {
                    // Player already in the game.
                    try { client.Client.Shutdown(SocketShutdown.Both); }
                    catch (Exception) { }
                    stream.Dispose();
                    client.Close();
                }
                else
                {
                    // Not yet here, give him a slot, wait for join information.
                    int playerNumber = FindFreePlayerNumber();
                    _slots[playerNumber] = true;
                    _clients[playerNumber] = client;
                    _streams[playerNumber] = new PacketStream(client.GetStream());
                    if (++NumPlayers == MaxPlayers)
                    {
                        // Ignore connection requests, we're full.
                        _tcp.Stop();
                    }
                }
            }

            // Check for incoming data.
            for (int i = 0; i < MaxPlayers; ++i)
            {
                if (_streams[i] != null)
                {
                    try
                    {
                        Packet packet;
                        while (_streams[i] != null && (packet = _streams[i].Read()) != null)
                        {
                            SessionMessage type = (SessionMessage)packet.ReadByte();
                            HandleTcpData(i, type, packet.ReadPacket());
                        }
                    }
                    catch (IOException ex)
                    {
                        // Connection failed, disconnect.
                        logger.TraceException("Socket connection died.", ex);
                        Remove(i);
                    }
                    catch (PacketException ex)
                    {
                        // Received invalid packet from server.
                        logger.WarnException("Invalid packet received from server.", ex);
                        Remove(i);
                    }
                }
            }

            base.Update(gameTime);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Disconnects the specified player.
        /// </summary>
        /// <param name="player">The player to disconnect.</param>
        public void Disconnect(Player<TPlayerData, TPacketizerContext> player)
        {
            if (HasPlayer(player))
            {
                SendTo(player, SessionMessage.PlayerLeft, new Packet().
                    Write(player.Number));
                Remove(player.Number);
            }
        }

        #endregion

        #region Sending

        /// <summary>
        /// Sends a data message with the the specified packet as its data to the specified player.
        /// </summary>
        /// <param name="packet">The data to send.</param>
        public void SendTo(Player<TPlayerData, TPacketizerContext> player, Packet packet)
        {
            SendTo(player, SessionMessage.Data, packet);
        }

        /// <summary>
        /// Sends a message of the specified type, with the specified data to all players.
        /// </summary>
        /// <param name="type">The type of the message to send.</param>
        /// <param name="packet">The data to send.</param>
        protected override void Send(SessionMessage type, Packet packet)
        {
            foreach (var player in AllPlayers)
            {
                SendTo(player, type, packet);
            }
        }

        /// <summary>
        /// Sends a message of the specified type, with the specified data to the specified player.
        /// </summary>
        /// <param name="type">The type of the message to send.</param>
        /// <param name="packet">The data to send.</param>
        private void SendTo(Player<TPlayerData, TPacketizerContext> player, SessionMessage type, Packet packet)
        {
            if (!HasPlayer(player))
            {
                throw new ArgumentException("player");
            }
            try
            {
                _streams[player.Number].Write(new Packet().
                    Write((byte)type).
                    Write(packet));
            }
            catch (IOException ex)
            {
                // Client got disconnected.
                logger.TraceException("Socket connection died.", ex);
                Remove(player.Number);
            }
        }

        #endregion

        #region Event Handling

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
                case SessionMessage.GameInfoRequest:
                    // Game info was requested. Wrap it up and send it to the one asking.
                    {
                        RequestEventArgs requestArgs = new RequestEventArgs();
                        OnGameInfoRequested(requestArgs); //< Get custom data to send, if any.
                        udp.Send(new Packet().
                            Write((byte)SessionMessage.GameInfoResponse).
                            Write(MaxPlayers).
                            Write(NumPlayers).
                            Write(requestArgs.Data),
                            args.RemoteEndPoint);
                    }
                    break;

                // Ignore the rest.
                default:
                    logger.Trace("Unknown SessionMessage via UDP: {0}.", type);
                    break;
            }
        }

        private void HandleTcpData(int playerNumber, SessionMessage type, Packet packet)
        {
            switch (type)
            {
                case SessionMessage.JoinRequest:
                    // Player wants to join.
                    {
                        try
                        {
                            // First, get the name he wishes to use.
                            string playerName = packet.ReadString().Trim();

                            // Valid name?
                            if (String.IsNullOrWhiteSpace(playerName))
                            {
                                Remove(playerNumber);
                                return;
                            }

                            // Get custom player data.
                            TPlayerData playerData = new TPlayerData();
                            packet.ReadPacketizable(playerData, packetizer.Context);

                            // Create the player instance for the player.
                            var player = new Player<TPlayerData, TPacketizerContext>(playerNumber, playerName, playerData);

                            // Request additional info first, as this also triggers
                            // validation / prepping of the joining player's player
                            // info, or allow manual override -- disallowing the
                            // player to join.
                            var requestArgs = new JoinRequestEventArgs<TPlayerData, TPacketizerContext>(player, playerData);
                            try
                            {
                                OnJoinRequested(requestArgs);
                            }
                            catch (Exception ex)
                            {
                                // Something went wrong, possible wrong data provided by the client.
                                // In any case, block him.
                                logger.ErrorException("Failed getting join response data.", ex);
                                requestArgs.ShouldBlock = true;
                            }

                            // Should we block the player?
                            if (requestArgs.ShouldBlock)
                            {
                                Remove(playerNumber);
                                return;
                            }

                            // Store the player's info.
                            players[playerNumber] = player;

                            // Build the response.
                            Packet response = new Packet()
                                .Write(playerNumber)
                                .Write(NumPlayers)
                                .Write(MaxPlayers)
                                .Write(requestArgs.Data);

                            // Send info about all players in the game (including himself).
                            foreach (var p in AllPlayers)
                            {
                                response
                                    .Write(p.Number)
                                    .Write(p.Name)
                                    .Write(p.Data);
                            }

                            // Send the response!
                            SendTo(player, SessionMessage.JoinResponse, response);

                            // Tell the other players.
                            var joined = new Packet()
                                .Write(playerNumber)
                                .Write(playerName)
                                .Write(playerData);
                            foreach (var p in AllPlayers)
                            {
                                if (!p.Equals(player))
                                {
                                    SendTo(p, SessionMessage.PlayerJoined, joined);
                                }
                            }

                            // Tell the local program the player has joined.
                            OnPlayerJoined(new PlayerEventArgs<TPlayerData, TPacketizerContext>(players[playerNumber]));
                        }
                        catch (PacketException ex)
                        {
                            logger.WarnException("Invalid JoinRequest.", ex);
                            Remove(playerNumber);
                        }
                    }
                    break;

                case SessionMessage.Leave:
                    // Player wants to leave the session.
                    {
                        Remove(playerNumber);
                    }
                    break;

                case SessionMessage.Data:
                    // Custom data, just forward it.
                    OnData(new ServerDataEventArgs<TPlayerData, TPacketizerContext>(GetPlayer(playerNumber), packet));
                    break;

                // Ignore the rest.
                default:
                    logger.Trace("Unknown SessionMessage via TCP: {0}.", type);
                    break;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Disconnects the player with the specified number.
        /// </summary>
        /// <param name="playerNumber">The number of the player to be removed.</param>
        public void Remove(int playerNumber)
        {
            // Only if he's really still there (might be duplicate call).
            if (_slots[playerNumber])
            {
                // Keep for event dispatching.
                var player = players[playerNumber];

                try { _clients[playerNumber].Client.Shutdown(SocketShutdown.Both); }
                catch (Exception) { }
                _streams[playerNumber].Dispose();
                _clients[playerNumber].Close();

                _clients[playerNumber] = null;
                _streams[playerNumber] = null;
                _slots[playerNumber] = false;
                players[playerNumber] = null;

                --NumPlayers;

                // Start (will do nothing if it's already running), because
                // there's certainly a slot free, now.
                _tcp.Start();

                if (player != null)
                {
                    Send(SessionMessage.PlayerLeft, new Packet().Write(playerNumber));
                    OnPlayerLeft(new PlayerEventArgs<TPlayerData, TPacketizerContext>(player));
                }
            }
        }

        /// <summary>
        /// Gets the first free ID in this game (to fill up holes left by leaving players).
        /// </summary>
        /// <returns>the first free ID.</returns>
        private int FindFreePlayerNumber()
        {
            for (int i = 0; i < _slots.Length; ++i)
            {
                if (!_slots[i])
                {
                    return i;
                }
            }

            throw new InvalidOperationException("Game is already full.");
        }

        #endregion

        #region Event Dispatching

        private void OnJoinRequested(JoinRequestEventArgs<TPlayerData, TPacketizerContext> e)
        {
            if (JoinRequested != null)
            {
                JoinRequested(this, e);
            }
        }

        private void OnGameInfoRequested(RequestEventArgs e)
        {
            if (GameInfoRequested != null)
            {
                GameInfoRequested(this, e);
            }
        }

        #endregion
    }
}
