using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    public sealed class HybridServerSession<TPlayerData, TPacketizerContext>
        : AbstractHybridSession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
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
        private TcpListener tcp;

        /// <summary>
        /// The list of TCP connections to the connected clients.
        /// </summary>
        private TcpClient[] clients;

        /// <summary>
        /// The packet streams used for the different clients.
        /// </summary>
        private PacketStream[] streams;

        /// <summary>
        /// Keep track of free slots (use the first free on on joins).
        /// </summary>
        private BitArray slots;

        #endregion

        #region Constructor / Cleanup

        public HybridServerSession(Game game, ushort port, int maxPlayers)
            : base(game)
        {
            if (maxPlayers < 0)
            {
                throw new ArgumentException("maxPlayers");
            }

            tcp = new TcpListener(IPAddress.Any, port);
            tcp.Start();
            udp = new UdpProtocol(0, udpHeader, DefaultMulticastEndpoint.Address);

            this.MaxPlayers = (int)maxPlayers;
            players = new Player<TPlayerData, TPacketizerContext>[maxPlayers];
            this.clients = new TcpClient[maxPlayers];
            this.streams = new PacketStream[maxPlayers];
            slots = new BitArray(maxPlayers, false);
        }

        protected override void Dispose(bool disposing)
        {
            tcp.Stop();
            tcp.Server.Close();

            // TODO remove clients bla

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        public override void Update(GameTime gameTime)
        {
            while (NumPlayers < MaxPlayers && tcp.Pending())
            {
                TcpClient client = tcp.AcceptTcpClient();
                PacketStream stream = new PacketStream(client.GetStream());

                // Do not allow connections from the same IP twice, to avoid
                // ambivalence when getting UDP data packets.
                if (Array.FindIndex(clients, c => ((IPEndPoint)c.Client.RemoteEndPoint).Address.Equals(((IPEndPoint)client.Client.RemoteEndPoint).Address)) >= 0)
                {
                    // Player already in the game.
                    stream.Write(new Packet().
                        Write((byte)SessionMessage.JoinResponse).
                        Write(new Packet().
                            Write(false).
                            Write((byte)JoinResponseReason.AlreadyInGame)));
                    client.Client.Shutdown(SocketShutdown.Both);
                    stream.Dispose();
                    client.Close();
                }
                else
                {
                    // Not yet here, give him a slot, wait for join information.
                    int playerNumber = FindFreePlayerNumber();
                    slots[playerNumber] = true;
                    clients[playerNumber] = client;
                    streams[playerNumber] = new PacketStream(client.GetStream());
                }
            }

            base.Update(gameTime);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Kick a player from the session.
        /// </summary>
        /// <param name="player">the player to kick.</param>
        public void Kick(Player<TPlayerData, TPacketizerContext> player)
        {
            if (HasPlayer(player))
            {
                // Let him know.
                SendTo(player, SessionMessage.PlayerLeft, new Packet().
                    Write(player.Number));

                // Erase him.
                RemovePlayer(player);
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
        /// Sends a data-less message of the specified type to the specified player.
        /// </summary>
        /// <param name="type">The type of the data-less message to send.</param>
        protected void SendTo(Player<TPlayerData, TPacketizerContext> player, SessionMessage type)
        {
            SendTo(player, type, null);
        }

        /// <summary>
        /// Sends a message of the specified type, with the specified data to all players.
        /// </summary>
        /// <param name="type">The type of the message to send.</param>
        /// <param name="packet">The data to send.</param>
        protected abstract void Send(SessionMessage type, Packet packet)
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
        protected abstract void SendTo(Player<TPlayerData, TPacketizerContext> player, SessionMessage type, Packet packet)
        {
            if (!HasPlayer(player))
            {
                throw new ArgumentException("player");
            }
            try
            {
                streams[player.Number].Write(new Packet().
                    Write((byte)type).
                    Write(packet));
            }
            catch (SocketException)
            {
                // Client got disconnected.
                RemovePlayer(player);
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

                case SessionMessage.Data:
                    // Custom data, just forward it if this is from a player of this session.
                    Player<TPlayerData, TPacketizerContext> player = GetPlayer(Array.FindIndex(clients, c => c.Client.RemoteEndPoint.Equals(args.RemoteEndPoint)));

                    // If it is, forward the data.
                    if (player != null)
                    {
                        OnPlayerData(new PlayerDataEventArgs<TPlayerData, TPacketizerContext>(player, data, false));
                    }
                    break;

                // Ignore the rest.
                default:
                    logger.Trace("Unknown SessionMessage via UDP: {0}.", type);
                    break;
            }
        }

        private void HandleTcpData(Player<TPlayerData, TPacketizerContext> player, SessionMessage type, Packet packet)
        {
            switch (type)
            {
                case SessionMessage.JoinRequest:
                    // Player wants to join.
                    {
                        // Or if the game is already full.
                        else if (NumPlayers >= MaxPlayers)
                        {
                            // Game is full.
                            Packet fail = new Packet(2);
                            fail.Write(false);
                            fail.Write((byte)JoinResponseReason.GameFull);
                            SendToEndPoint(args.Remote, SessionMessage.JoinResponse, fail, PacketPriority.None);
                        }
                        else
                        {
                            try
                            {
                                // OK, allow the player to join. Get the number he'll hold.
                                int playerNumber = FindFreePlayerNumber();

                                // First, get the name he wishes to use.
                                string playerName = data.ReadString().Trim();

                                // Valid name?
                                if (String.IsNullOrWhiteSpace(playerName))
                                {
                                    Packet fail = new Packet(2);
                                    fail.Write(false);
                                    fail.Write((byte)JoinResponseReason.InvalidName);
                                    SendToEndPoint(args.Remote, SessionMessage.JoinResponse, fail, PacketPriority.None);
                                    args.Consume();
                                    return;
                                }

                                // Anyone else already using that name?
                                foreach (var p in AllPlayers)
                                {
                                    if (p.Name.Equals(playerName))
                                    {
                                        // Already taken. Rename him.
                                        playerName = playerName + playerNumber;
                                        break;
                                    }
                                }

                                // Get custom player data.
                                TPlayerData playerData = new TPlayerData();
                                data.ReadPacketizable(playerData, packetizer.Context);

                                // Create the player instance for the player.
                                var player = new Player<TPlayerData, TPacketizerContext>(playerNumber, playerName, playerData,
                                    delegate() { return protocol.GetPing(playerAddresses[playerNumber]); });

                                // Request additional info first, as this also triggers
                                // validation / prepping of the joining player's player
                                // info, or allow manual override -- disallowing the
                                // player to join.
                                var requestArgs = new JoinRequestEventArgs<TPlayerData, TPacketizerContext>(player, playerData);
                                try
                                {
                                    OnJoinRequested(requestArgs);
                                }
#if DEBUG
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error in OnJoinRequested: " + ex);
#else
                                catch (Exception)
                                {
#endif
                                    // Something went wrong, possible wrong data provided by the client.
                                    // In any case, block him.
                                    requestArgs.ShouldBlock = true;
                                }

                                // Should we block the player?
                                if (requestArgs.ShouldBlock)
                                {
                                    Packet fail = new Packet(2);
                                    fail.Write(false);
                                    fail.Write((byte)JoinResponseReason.Unknown);
                                    SendToEndPoint(args.Remote, SessionMessage.JoinResponse, fail, PacketPriority.None);
                                    args.Consume();
                                    return;
                                }

                                // Store the player's info.
                                playerAddresses[playerNumber] = args.Remote;
                                players[playerNumber] = player;
                                slots[playerNumber] = true;
                                ++NumPlayers;

                                // Build the response.
                                Packet response = new Packet();
                                response.Write(true);

                                // Tell the player his number.
                                response.Write(playerNumber);

                                // Send info about all players in the game (including himself).
                                response.Write(NumPlayers);
                                response.Write(MaxPlayers);
                                foreach (var p in AllPlayers)
                                {
                                    response.Write(p.Number);
                                    response.Write(p.Name);
                                    response.Write(p.Data);
                                    response.Write(playerAddresses[p.Number].Address.GetAddressBytes());
                                    response.Write(playerAddresses[p.Number].Port);
                                }

                                // Now write the other game relevant data (e.g. game state).
                                response.Write(requestArgs.Data);

                                // Send the response!
                                SendToPlayer(player, SessionMessage.JoinResponse, response, PacketPriority.Medium);

                                // Tell the other players.
                                var joined = new Packet();
                                joined.Write(playerNumber);
                                joined.Write(playerName);
                                joined.Write(playerData);
                                joined.Write(args.Remote.Address.GetAddressBytes());
                                joined.Write(args.Remote.Port);
                                foreach (var p in AllPlayers)
                                {
                                    if (!p.Equals(player))
                                    {
                                        SendToPlayer(p, SessionMessage.PlayerJoined, joined, PacketPriority.Medium);
                                    }
                                }

                                // Tell the local program the player has joined.
                                OnPlayerJoined(new PlayerEventArgs<TPlayerData, TPacketizerContext>(players[playerNumber]));

                                // OK, we handled it.
                                args.Consume();
                            }
                            catch (PacketException ex)
                            {
                                logger.WarnException("Invalid JoinRequest.", ex);
                                RemovePlayer(player);
                            }
                        }
                    }
                    break;

                case SessionMessage.Leave:
                    // Player wants to leave the session.
                    {
                        RemovePlayer(player);
                    }
                    break;

                case SessionMessage.Data:
                    // Custom data, just forward it.
                    OnPlayerData(new PlayerDataEventArgs<TPlayerData, TPacketizerContext>(player, data, false));
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
        /// Method to actually remove a player from the session. This frees
        /// his slot and sends the remaining clients the notification that
        /// that player has left.
        /// </summary>
        /// <param name="playerNumber"></param>
        private void RemovePlayer(Player<TPlayerData, TPacketizerContext> player)
        {
            // Erase the player from the session.
            playerAddresses[player.Number] = null;
            players[player.Number] = null;
            slots[player.Number] = false;
            --NumPlayers;

            // Tell the other clients.
            Packet packet = new Packet(sizeof(int));
            packet.Write(player.Number);
            SendToEveryone(SessionMessage.PlayerLeft, packet, PacketPriority.Low);

            // Tell the local program the player is gone.
            OnPlayerLeft(new PlayerEventArgs<TPlayerData, TPacketizerContext>(player));
        }

        /// <summary>
        /// Gets the first free ID in this game (to fill up holes left by leaving players).
        /// </summary>
        /// <returns>the first free ID.</returns>
        private int FindFreePlayerNumber()
        {
            for (int i = 0; i < slots.Length; ++i)
            {
                if (!slots[i])
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
