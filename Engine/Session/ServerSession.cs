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
    sealed class ServerSession<TPlayerData, TPacketizerContext> : AbstractSession<TPlayerData, TPacketizerContext>, IServerSession<TPlayerData, TPacketizerContext>
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

        public override void Send(Packet data, uint pollRate = 0)
        {
            //throw new InvalidOperationException("Server cannot send messages to itself. Use a more direct design.");
        }

        /// <summary>
        /// Kick a player from the session.
        /// </summary>
        /// <param name="player">the number of the player to kick.</param>
        public void Kick(int playerNumber)
        {
            // Let him know.
            Packet packet = new Packet(4);
            packet.Write(playerNumber);
            Send(playerAddresses[playerNumber], SessionMessage.PlayerLeft, packet);

            // Erase him.
            RemovePlayer(playerNumber);
        }

        protected override void HandlePlayerTimeout(object sender, EventArgs e)
        {
            var args = (ProtocolEventArgs)e;
            int playerNumber = Array.IndexOf(playerAddresses, args.Remote);
            if (playerNumber >= 0)
            {
                RemovePlayer(playerNumber);
            } // else we don't care.
        }

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
            SendAll(SessionMessage.PlayerLeft, packet, 100);

            // Tell the local program the player is gone.
            OnPlayerLeft(new PlayerEventArgs<TPlayerData, TPacketizerContext>(player));
        }

        #endregion

        #region Logic / Event handling

        /// <summary>
        /// Drives the multicast checking.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Drive multicast.
            MulticastReceive();

            base.Update(gameTime);
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
            else
            {
                data = null;
            }

            switch (type)
            {
                case SessionMessage.GameInfoRequest:
                    // Game info was requested. Wrap it up and send it to the one asking.
                    {
                        RequestEventArgs requestArgs = new RequestEventArgs();
                        OnGameInfoRequested(requestArgs); //< Get custom data to send, if any.
                        Packet response = new Packet(12 + requestArgs.Data.Length);
                        response.Write(MaxPlayers);
                        response.Write(NumPlayers);
                        response.Write(requestArgs.Data);
                        Send(args.Remote, SessionMessage.GameInfoResponse, response);
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
                            Send(args.Remote, SessionMessage.JoinResponse, fail);
                        }
                        // Or if the game is already full.
                        else if (NumPlayers >= MaxPlayers)
                        {
                            // Game is full.
                            Packet fail = new Packet(2);
                            fail.Write(false);
                            fail.Write((byte)JoinResponseReason.GameFull);
                            Send(args.Remote, SessionMessage.JoinResponse, fail);
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
                                    Send(args.Remote, SessionMessage.JoinResponse, fail);
                                    args.Consume();
                                    return;
                                }

                                // Anyone else already using that name?
                                for (int i = 0; i < MaxPlayers; ++i)
                                {
                                    if (!slots[i]) continue;

                                    if (players[i].Name.Equals(playerName))
                                    {
                                        // Already taken. Rename him.
                                        playerName = playerName + playerNumber;
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
                                    Send(args.Remote, SessionMessage.JoinResponse, fail);
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
                                for (int i = 0; i < MaxPlayers; i++)
                                {
                                    // Skip empty slots.
                                    if (!slots[i]) continue;

                                    Player<TPlayerData, TPacketizerContext> p = GetPlayer(i);
                                    response.Write(p.Number);
                                    response.Write(p.Name);
                                    response.Write(p.Data);
                                    response.Write(playerAddresses[i].Address.GetAddressBytes());
                                    response.Write(playerAddresses[i].Port);
                                }

                                // Now write the other game relevant data (e.g. game state).
                                response.Write(requestArgs.Data);

                                // Send the response!
                                Send(args.Remote, SessionMessage.JoinResponse, response, 40);

                                // Tell the other players, but *only* the other players.
                                var joined = new Packet();
                                joined.Write(playerNumber);
                                joined.Write(playerName);
                                joined.Write(playerData);
                                joined.Write(args.Remote.Address.GetAddressBytes());
                                joined.Write(args.Remote.Port);
                                for (int i = 0; i < MaxPlayers; ++i)
                                {
                                    if (playerAddresses[i] != null && i != playerNumber)
                                    {
                                        Send(playerAddresses[i], SessionMessage.PlayerJoined, joined, 40);
                                    }
                                }

                                // Tell the local program the player has joined.
                                OnPlayerJoined(new PlayerEventArgs<TPlayerData, TPacketizerContext>(players[playerNumber]));

                                // OK, we handled it.
                                args.Consume();
                            }
                            catch (PacketException ex)
                            {
#if DEBUG
                                Console.WriteLine("Invalid JoinRequest: " + ex.ToString());
#else
                                //Console.WriteLine("Invalid JoinRequest: " + ex.Message);
#endif
                            }
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
                case SessionMessage.GameInfoResponse:
                case SessionMessage.JoinResponse:
                case SessionMessage.PlayerJoined:
                case SessionMessage.PlayerLeft:
                    // Ignore as server.
                    break;
                default:
                    // Invalid packet.
                    Console.WriteLine("Received packet wit unknown session message type {0}.", type);
                    break;
            }
        }

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

        #endregion

        #region Utility

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
