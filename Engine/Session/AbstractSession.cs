using System;
using System.Net;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    enum SessionMessage
    {

        /// <summary>
        /// Client requested the game information.
        /// </summary>
        GameInfoRequest,

        /// <summary>
        /// Server answered to our request for game info.
        /// </summary>
        GameInfoResponse,

        /// <summary>
        /// Client wants to join our game.
        /// </summary>
        JoinRequest,

        /// <summary>
        /// Server answers to client request for joining a game.
        /// </summary>
        JoinResponse,

        /// <summary>
        /// Client leaves the session. This is just courtesy. The server
        /// would disconnect the client if it stops responding anyway.
        /// </summary>
        Leave,

        /// <summary>
        /// Server informs us that a player has joined.
        /// </summary>
        PlayerJoined,

        /// <summary>
        /// Server informs us that a player has left.
        /// </summary>
        PlayerLeft,

        /// <summary>
        /// Test if connection is still alive and accumulate data for ping.
        /// </summary>
        Ping,

        /// <summary>
        /// Data packet to be handled by someone else.
        /// </summary>
        Data
    }

    /// <summary>
    /// Base implementation for server and client side sessions, i.e. functionality used by both.
    /// </summary>
    abstract class AbstractSession<TPlayerData> : GameComponent, ISession<TPlayerData>
        where TPlayerData : IPacketizable
    {
        #region Constants

        /// <summary>
        /// The default port we'll use to listen for multicast messages (asking for open games).
        /// </summary>
        protected const ushort DefaultMulticastPort = 50000;

        /// <summary>
        /// The default multicast group address we'll use for searching games.
        /// </summary>
        protected readonly IPAddress DefaultMulticastAddress = new IPAddress(new byte[] { 224, 1, 33, 7 });

        #endregion

        #region Events

        /// <summary>
        /// Called when a new player joins the session.
        /// </summary>
        public event EventHandler<EventArgs> PlayerJoined;

        /// <summary>
        /// Called when a player left the session.
        /// </summary>
        public event EventHandler<EventArgs> PlayerLeft;

        /// <summary>
        /// Called when a player sent data.
        /// </summary>
        public event EventHandler<EventArgs> PlayerData;

        #endregion

        #region Properties

        /// <summary>
        /// Number of the local player.
        /// </summary>
        public int LocalPlayer { get; protected set; }

        /// <summary>
        /// Number of players currently in the game.
        /// </summary>
        public int NumPlayers { get; protected set; }

        /// <summary>
        /// Maximum number of player possible in this game.
        /// </summary>
        public int MaxPlayers { get; protected set; }

        #endregion

        #region Fields

        /// <summary>
        /// The underlying protocol that's being used.
        /// </summary>
        protected IProtocol protocol;

        /// <summary>
        /// List of all players currently in the game (may contain holes).
        /// </summary>
        protected IPEndPoint[] playerAddresses;

        /// <summary>
        /// List of all the player structs.
        /// </summary>
        protected Player<TPlayerData>[] players;

        #endregion

        public AbstractSession(Game game, IProtocol protocol)
            : base(game)
        {
            LocalPlayer = 0;
            this.protocol = protocol;

            protocol.MessageTimeout += HandlePlayerTimeout;
            protocol.Data += HandlePlayerData;

            Game.Components.Add(this);
        }

        /// <summary>
        /// Close this session, detaching it from the underlying protocol and
        /// making it invalid for further use.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            protocol.MessageTimeout -= HandlePlayerTimeout;
            protocol.Data -= HandlePlayerData;

            Game.Components.Remove(this);

            base.Dispose(disposing);
        }

        /// <summary>
        /// Get info on the player with the given number.
        /// </summary>
        /// <param name="player">the number of the player.</param>
        /// <returns>information on the player.</returns>
        public Player<TPlayerData> GetPlayer(int player)
        {
            return players[player];
        }

        /// <summary>
        /// Send some data to the server.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <param name="pollRate">lower (but > 0) means more urgent, if the protocol supports it.
        /// In case of the UDP protocol, 0 means the message is only sent once (no reliability guarantee).</param>
        public abstract void Send(Packet data, uint pollRate = 0);

        /// <summary>
        /// Send some data to a specific player.
        /// </summary>
        /// <param name="player">the player to send the data to.</param>
        /// <param name="data">the data to send.</param>
        /// <param name="pollRate">lower (but > 0) means more urgent, if the protocol supports it.
        /// In case of the UDP protocol, 0 means the message is only sent once (no reliability guarantee).</param>
        public void Send(int player, Packet data, uint pollRate = 0)
        {
            Send(playerAddresses[player], SessionMessage.Data, data, pollRate);
        }

        /// <summary>
        /// Send a message to all players in the game, and the server.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <param name="pollRate">lower (but > 0) means more urgent, if the protocol supports it.
        /// In case of the UDP protocol, 0 means the message is only sent once (no reliability guarantee).</param>
        public void SendAll(Packet data, uint pollRate = 0)
        {
            SendAll(SessionMessage.Data, data, pollRate);
        }

        /// <summary>
        /// Internal variant for sending data to a specific host.
        /// </summary>
        /// <param name="remote">the remote machine to send the data to.</param>
        /// <param name="type">the type of message that is sent.</param>
        /// <param name="data">the data to send.</param>
        /// <param name="pollrate">see Send()</param>
        protected void Send(IPEndPoint remote, SessionMessage type, Packet data, uint pollrate = 0)
        {
            Packet wrapper = new Packet(5 + (data != null ? data.Length : 0));
            wrapper.Write((byte)type);
            wrapper.Write(data);
            protocol.Send(wrapper, remote, pollrate);
        }

        /// <summary>
        /// As the internal Send, just for SendAll.
        /// </summary>
        /// <param name="type">the type of message to send.</param>
        /// <param name="data">the data to send.</param>
        /// <param name="pollrate">see Send()</param>
        protected void SendAll(SessionMessage type, Packet data, uint pollrate = 0)
        {
            for (int i = 0; i < MaxPlayers; ++i)
            {
                if (playerAddresses[i] != null)
                {
                    Send(playerAddresses[i], type, data, pollrate);
                }
            }
            Send(data, pollrate);
        }

        /// <summary>
        /// Handle disconnects to players due to timeouts.
        /// </summary>
        protected abstract void HandlePlayerTimeout(object sender, EventArgs e);

        /// <summary>
        /// Handle data received from a remote machine.
        /// </summary>
        protected virtual void HandlePlayerData(object sender, EventArgs e)
        {
            ProtocolDataEventArgs args = (ProtocolDataEventArgs)e;
            ConditionalOnPlayerData(args, args.Data);
        }

        protected void ConditionalOnPlayerData(ProtocolDataEventArgs args, Packet data)
        {
            // Check if this is a player of this session.
            int player = Array.IndexOf(playerAddresses, args.Remote);

            // If it is, forward the data.
            if (player >= 0)
            {
                OnPlayerData(new PlayerDataEventArgs<TPlayerData>(players[player], args, data));
            }
        }

        protected void OnPlayerJoined(PlayerEventArgs<TPlayerData> e)
        {
            if (PlayerJoined != null)
            {
                PlayerJoined(this, e);
            }
        }

        protected void OnPlayerLeft(PlayerEventArgs<TPlayerData> e)
        {
            if (PlayerLeft != null)
            {
                PlayerLeft(this, e);
            }
        }

        protected void OnPlayerData(PlayerDataEventArgs<TPlayerData> e)
        {
            if (PlayerData != null)
            {
                PlayerData(this, e);
            }
        }
    }
}
