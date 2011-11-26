using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Engine.Network;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// Used for hosting a session.
    /// </summary>
    sealed class ServerSession : AbstractSession, IServerSession
    {
        public event EventHandler<EventArgs> GameInfoRequested;
        public event EventHandler<EventArgs> JoinRequested;

        /// <summary>
        /// Keep track of free slots (use the first free on on joins).
        /// </summary>
        private BitArray slots;

        /// <summary>
        /// Socket used to listen to multicast messages, to make our
        /// session findable in the local network.
        /// </summary>
        private UdpClient multicast;

        public ServerSession(IProtocol protocol, int maxPlayers)
            : base(protocol)
        {
            this.NumPlayers = 0;
            this.MaxPlayers = maxPlayers;
            playerAddresses = new IPEndPoint[maxPlayers];
            players = new Player[maxPlayers];
            slots = new BitArray(maxPlayers, false);
            
            multicast = new UdpClient(DefaultMulticastPort);
            multicast.JoinMulticastGroup(DefaultMulticastAddress);
            multicast.BeginReceive(new AsyncCallback(HandleMulticastReceived), this);
        }

        public override void Dispose()
        {
            multicast.Close();
            base.Dispose();
        }

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
            Player player = players[playerNumber];
            playerAddresses[playerNumber] = null;
            players[playerNumber] = null;
            slots[playerNumber] = false;
            --NumPlayers;

            // Tell the other clients.
            Packet packet = new Packet(4);
            packet.Write(playerNumber);
            SendAll(SessionMessage.PlayerLeft, packet, 100);

            // Tell the local program the player is gone.
            OnPlayerLeft(new PlayerEventArgs(player));
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
                            // OK, allow the player to join. Get the number he'll hold.
                            int playerNumber = FindFreePlayerNumber();

                            // First, get the name he wishes to use.
                            if (!data.HasString())
                            {
                                Console.WriteLine("Received invalid JoinRequest, no player name.");
                                return;
                            }
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
                            if (!data.HasByteArray())
                            {
                                Console.WriteLine("Received invalid JoinRequest, no player data.");
                                return;
                            }
                            byte[] playerData = data.ReadByteArray();

                            // Store the player's info.
                            playerAddresses[playerNumber] = args.Remote;
                            players[playerNumber] = new Player(playerNumber, playerName, playerData,
                                delegate() { return protocol.GetPing(playerAddresses[playerNumber]); });
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

                                Player p = GetPlayer(i);
                                response.Write(p.Number);
                                response.Write(p.Name);
                                response.Write(p.Data);
                                response.Write(playerAddresses[i].Address.GetAddressBytes());
                                response.Write(playerAddresses[i].Port);
                            }

                            // Add other game relevant data (e.g. game state).
                            RequestEventArgs requestArgs = new RequestEventArgs();
                            OnJoinRequested(requestArgs);
                            response.Write(requestArgs.Data);

                            // Send the response!
                            Send(args.Remote, SessionMessage.JoinResponse, response, 100);

                            // Tell the other players.
                            response = new Packet();
                            response.Write(playerNumber);
                            response.Write(playerName);
                            response.Write(playerData);
                            response.Write(args.Remote.Address.GetAddressBytes());
                            response.Write(args.Remote.Port);
                            SendAll(SessionMessage.PlayerJoined, response, 100);

                            // Tell the local program the player has joined.
                            OnPlayerJoined(new PlayerEventArgs(players[playerNumber]));
                        }
                    }
                    args.Consume();
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

        private void HandleMulticastReceived(IAsyncResult result)
        {
            try
            {
                // Try to finish receiving and forward the packet to the protocol.
                IPEndPoint remote = new IPEndPoint(0, 0);
                protocol.Inject(multicast.EndReceive(result, ref remote), remote);

                // Wait for the next package.
                multicast.BeginReceive(new AsyncCallback(HandleMulticastReceived), this);
            }
            catch (SocketException e)
            {
                // Don't know when this can happen, maybe when the connection gets disabled in the OS?
                Console.WriteLine("Error (multicast socket died): {0} {1}", e.ErrorCode, e.Message);

                // Anyway, unusable, so close it.
                multicast.Close();
            }
            catch (ObjectDisposedException)
            {
                // Socket was closed. Just stop.
            }
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

        private void OnJoinRequested(RequestEventArgs e)
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

    }
}
