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
        public event GameInfoRequestedEventHandler GameInfoRequested;
        public event JoinRequestedEventHandler JoinRequested;

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

        protected override void HandlePlayerTimeout(IPEndPoint remote)
        {
            int playerNumber = Array.IndexOf(playerAddresses, remote);
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
            OnPlayerLeft(player);
        }

        protected override bool HandlePlayerData(IPEndPoint remote, Packet data)
        {
            // Get the message type.
            if (!data.HasByte())
            {
                Console.WriteLine("Received invalid packet, no SessionMessage type.");
                return false;
            }
            SessionMessage type = (SessionMessage)data.ReadByte();

            // Get additional data.
            if (data.HasPacket())
            {
                data = data.ReadPacket();
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
                        Packet customData;
                        OnGameInfoRequested(out customData); //< Get custom data to send, if any.
                        Packet response = new Packet(12 + (customData != null ? customData.Length : 0));
                        response.Write(MaxPlayers);
                        response.Write(NumPlayers);
                        response.Write(customData);
                        Send(remote, SessionMessage.GameInfoResponse, response);
                    }
                    return true;
                case SessionMessage.JoinRequest:
                    // Player wants to join.
                    {
                        // Check if the player is already in the game.
                        if (Array.IndexOf(playerAddresses, remote) >= 0)
                        {
                            // Player already in the game.
                            Packet fail = new Packet(2);
                            fail.Write(false);
                            fail.Write((byte)JoinResponseReason.AlreadyInGame);
                            Send(remote, SessionMessage.JoinResponse, fail);
                        }
                        // Or if the game is already full.
                        else if (NumPlayers >= MaxPlayers)
                        {
                            // Game is full.
                            Packet fail = new Packet(2);
                            fail.Write(false);
                            fail.Write((byte)JoinResponseReason.GameFull);
                            Send(remote, SessionMessage.JoinResponse, fail);
                        }
                        else
                        {
                            // OK, allow the player to join. Get the number he'll hold.
                            int playerNumber = FindFreePlayerNumber();

                            // First, get the name he wishes to use.
                            if (!data.HasString())
                            {
                                Console.WriteLine("Received invalid JoinRequest, no player name.");
                                return false;
                            }
                            string playerName = data.ReadString().Trim();

                            // Valid name?
                            if (String.IsNullOrEmpty(playerName))
                            {
                                Packet fail = new Packet(2);
                                fail.Write(false);
                                fail.Write((byte)JoinResponseReason.InvalidName);
                                Send(remote, SessionMessage.JoinResponse, fail);
                                return true;
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
                                return false;
                            }
                            byte[] playerData = data.ReadByteArray();

                            // Store the player's info.
                            playerAddresses[playerNumber] = remote;
                            players[playerNumber] = new Player(playerNumber, playerName, playerData);
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
                            Packet joinData;
                            OnJoinRequested(out joinData);
                            response.Write(joinData);

                            // Send the response!
                            Send(remote, SessionMessage.JoinResponse, response, 100);

                            // Tell the other players.
                            response = new Packet();
                            response.Write(playerNumber);
                            response.Write(playerName);
                            response.Write(playerData);
                            response.Write(remote.Address.GetAddressBytes());
                            response.Write(remote.Port);
                            SendAll(SessionMessage.PlayerJoined, response, 100);

                            // Tell the local program the player has joined.
                            OnPlayerJoined(players[playerNumber]);
                        }
                    }
                    return true;
                case SessionMessage.Leave:
                    // Player wants to leave the session.
                    {
                        int playerNumber = Array.IndexOf(playerAddresses, remote);
                        if (playerNumber >= 0)
                        {
                            RemovePlayer(playerNumber);
                            return true;
                        }
                    }
                    break;
                case SessionMessage.Data:
                    // Custom data, just forward it.
                    return base.HandlePlayerData(remote, data);
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
            return false;
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

        private void OnJoinRequested(out Packet data)
        {
            if (JoinRequested != null)
            {
                JoinRequested(out data);
            }
            else
            {
                data = null;
            }
        }

        private void OnGameInfoRequested(out Packet data)
        {
            if (GameInfoRequested != null)
            {
                GameInfoRequested(out data);
            }
            else
            {
                data = null;
            }
        }

    }
}
