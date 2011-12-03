using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    /// <summary>
    /// Used for hosting a session.
    /// </summary>
    public sealed class ServerSession<TPlayerData, TPacketizerContext> : AbstractSession<TPlayerData, TPacketizerContext>, IServerSession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
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
        /// Keep track of free slots (use the first free on on joins).
        /// </summary>
        private BitArray slots;

        /// <summary>
        /// Socket used to listen to multicast messages, to make our
        /// session findable in the local network.
        /// </summary>
        private UdpClient multicast;

        #endregion

        #region Constructor / Cleanup

        public ServerSession(Game game, IProtocol protocol, int maxPlayers)
            : base(game, protocol)
        {
            this.NumPlayers = 0;
            this.MaxPlayers = maxPlayers;
            this.LocalPlayerNumber = -1;
            playerAddresses = new IPEndPoint[maxPlayers];
            players = new Player<TPlayerData, TPacketizerContext>[maxPlayers];
            slots = new BitArray(maxPlayers, false);

            multicast = new UdpClient(DefaultMulticastPort);
            multicast.JoinMulticastGroup(DefaultMulticastAddress);
        }

        protected override void Dispose(bool disposing)
        {
            multicast.Close();

            base.Dispose(disposing);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Kick a player from the session.
        /// </summary>
        /// <param name="player">the number of the player to kick.</param>
        public void Kick(int playerNumber)
        {
            if (HasPlayer(playerNumber))
            {
                // Let him know.
                Packet packet = new Packet(4);
                packet.Write(playerNumber);
                SendToEndPoint(playerAddresses[playerNumber], SessionMessage.PlayerLeft, packet, PacketPriority.None);

                // Erase him.
                RemovePlayer(playerNumber);
            }
        }

        #endregion

        #region Internal send stuff

        /// <summary>
        /// Send some data to the server. Not supported for the server.
        /// </summary>
        /// <param name="type">the type of message to send.</param>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        internal override void SendToHost(SessionMessage type, Packet packet, PacketPriority priority)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drives the multicast checking.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Drive multicast.
            MulticastReceive();

            // Periodically send an acked no-op message, which is used to
            // check if clients in the game are still alive.
            if ((DateTime.Now - lastConnectionCheck).TotalMilliseconds > ConnectionCheckInterval)
            {
                lastConnectionCheck = DateTime.Now;
                SendToEveryone(SessionMessage.ConnectionTest, null, PacketPriority.Lowest);
            }

            base.Update(gameTime);
        }

        #endregion

        #region Events

        /// <summary>
        /// Handle player timeouts by removing them from the session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void HandlePlayerTimeout(object sender, EventArgs e)
        {
            var args = (ProtocolEventArgs)e;

            // But only if the player is in the game ;)
            int playerNumber = Array.IndexOf(playerAddresses, args.Remote);
            if (playerNumber >= 0)
            {
                RemovePlayer(playerNumber);
            } // else we don't care.
        }

        /// <summary>
        /// Got some data from a client in the session.
        /// </summary>
        protected override void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (ProtocolDataEventArgs)e;

            // Get the message type.
            if (!args.Data.HasByte())
            {
#if DEBUG
                Console.WriteLine("Received invalid packet, no SessionMessage type.");
#endif
                return;
            }
            SessionMessage type = (SessionMessage)args.Data.ReadByte();

            // Get additional data.
            Packet data = null;
            if (args.Data.HasPacket())
            {
                data = args.Data.ReadPacket();
            }
            else
            {
                data = null;
            }

            switch (type)
            {
                case SessionMessage.ConnectionTest:
                    // Client wants to know if we're still there.
                    if (Array.IndexOf(playerAddresses, args.Remote) >= 0)
                    {
                        // We know the client to be in the session. Allow acking.
                        args.Consume();
                    }
                    break;

                case SessionMessage.GameInfoRequest:
                    // Game info was requested. Wrap it up and send it to the one asking.
                    {
                        RequestEventArgs requestArgs = new RequestEventArgs();
                        OnGameInfoRequested(requestArgs); //< Get custom data to send, if any.
                        Packet response = new Packet(12 + requestArgs.Data.Length);
                        response.Write(MaxPlayers);
                        response.Write(NumPlayers);
                        response.Write(requestArgs.Data);
                        SendToEndPoint(args.Remote, SessionMessage.GameInfoResponse, response, PacketPriority.None);
                    }
                    args.Consume();
                    break;

                case SessionMessage.JoinRequest:
                    // Player wants to join.
                    {
                        // Check if the player is already in the game.
                        if (Array.IndexOf(playerAddresses, args.Remote) >= 0)
                        {
                            // Player already in the game.
                            Packet fail = new Packet(2);
                            fail.Write(false);
                            fail.Write((byte)JoinResponseReason.AlreadyInGame);
                            SendToEndPoint(args.Remote, SessionMessage.JoinResponse, fail, PacketPriority.None);
                        }
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
                                if (String.IsNullOrEmpty(playerName))
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
                                        SendToPlayer(player, SessionMessage.PlayerJoined, joined, PacketPriority.Medium);
                                    }
                                }

                                // Tell the local program the player has joined.
                                OnPlayerJoined(new PlayerEventArgs<TPlayerData, TPacketizerContext>(players[playerNumber]));

                                // OK, we handled it.
                                args.Consume();
                            }
#if DEBUG
                            catch (PacketException ex)
                            {
                                Console.WriteLine("Invalid JoinRequest: " + ex.ToString());
                            }
#else
                            catch (PacketException ex)
                            {
                            }
#endif
                        }
                    }
                    break;

                case SessionMessage.Leave:
                    // Player wants to leave the session.
                    {
                        int playerNumber = Array.IndexOf(playerAddresses, args.Remote);
                        if (playerNumber >= 0)
                        {
                            RemovePlayer(playerNumber);
                            args.Consume();
                        }
                    }
                    break;

                case SessionMessage.Data:
                    // Custom data, just forward it.
                    ConditionalOnPlayerData(args, data);
                    break;

                // Ignore the rest.
                default:
                    break;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Check for incoming messages on our multicast socket (game info requests).
        /// </summary>
        private void MulticastReceive()
        {
            if (multicast.Client != null)
            {
                var remote = new IPEndPoint(0, 0);
                while (multicast.Available > 0)
                {
                    // Try to finish receiving and forward the packet to the protocol.
                    byte[] buffer = multicast.Receive(ref remote);
                    protocol.Inject(buffer, remote);
                }
            }
        }

        /// <summary>
        /// Method to actually remove a player from the session. This frees
        /// his slot and sends the remaining clients the notification that
        /// that player has left.
        /// </summary>
        /// <param name="playerNumber"></param>
        private void RemovePlayer(int playerNumber)
        {
            // Erase the player from the session.
            Player<TPlayerData, TPacketizerContext> player = players[playerNumber];
            playerAddresses[playerNumber] = null;
            players[playerNumber] = null;
            slots[playerNumber] = false;
            --NumPlayers;

            // Tell the other clients.
            Packet packet = new Packet(4);
            packet.Write(playerNumber);
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
