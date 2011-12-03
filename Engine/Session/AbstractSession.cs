using System;
using System.Collections.Generic;
using System.Net;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    /// <summary>
    /// Message types sessions send around for internal logic.
    /// </summary>
    internal enum SessionMessage
    {
        /// <summary>
        /// Only used internally, to check if players still respond, and can
        /// therefore be considered in the session.
        /// </summary>
        ConnectionTest,

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
        /// Data packet to be handled by someone else.
        /// </summary>
        Data
    }

    /// <summary>
    /// Base implementation for server and client side sessions, i.e. functionality used by both.
    /// </summary>
    public abstract class AbstractSession<TPlayerData, TPacketizerContext>
        : GameComponent, ISession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
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

        /// <summary>
        /// The time in milliseconds to wait between sending connection checks to
        /// all our connected clients.
        /// </summary>
        protected const int ConnectionCheckInterval = 5000;

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
        /// Get a list of all players in the game.
        /// </summary>
        public IEnumerable<Player<TPlayerData, TPacketizerContext>> AllPlayers
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Reference to the data struct with info about the local player.
        /// </summary>
        /// <remarks>Shortcut for <c>session.GetPlayer(session.LocalPlayerNumber)</c>.</remarks>
        public Player<TPlayerData, TPacketizerContext> LocalPlayer { get { return GetPlayer(LocalPlayerNumber); } }

        /// <summary>
        /// Number of the local player.
        /// </summary>
        public int LocalPlayerNumber { get; protected set; }

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
        /// Packetizer used for this session's game.
        /// </summary>
        protected IPacketizer<TPlayerData, TPacketizerContext> packetizer;

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
        protected Player<TPlayerData, TPacketizerContext>[] players;

        /// <summary>
        /// Last time we checked our clients' connections.
        /// </summary>
        protected DateTime lastConnectionCheck = DateTime.Now;

        #endregion

        #region Constructor / Cleanup

        public AbstractSession(Game game, IProtocol protocol)
            : base(game)
        {
            LocalPlayerNumber = 0;
            this.protocol = protocol;

            protocol.MessageTimeout += HandlePlayerTimeout;
            protocol.Data += HandlePlayerData;
        }

        public override void Initialize()
        {
            packetizer = ((IPacketizer<TPlayerData, TPacketizerContext>)Game.Services.GetService(typeof(IPacketizer<TPlayerData, TPacketizerContext>))).CopyFor(this);

            base.Initialize();
        }

        /// <summary>
        /// Close this session, detaching it from the underlying protocol and
        /// making it invalid for further use.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            protocol.MessageTimeout -= HandlePlayerTimeout;
            protocol.Data -= HandlePlayerData;

            base.Dispose(disposing);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get info on the player with the given number.
        /// </summary>
        /// <param name="player">the number of the player.</param>
        /// <returns>information on the player.</returns>
        public Player<TPlayerData, TPacketizerContext> GetPlayer(int player)
        {
            return HasPlayer(player) ? players[player] : null;
        }

        /// <summary>
        /// Check if the player with the given number exists.
        /// </summary>
        /// <param name="player">the number of the player to check.</param>
        /// <returns><c>true</c> if such a player exists.</returns>
        public bool HasPlayer(int player)
        {
            return (players != null && player >= 0 && player < MaxPlayers && players[player] != null);
        }

        /// <summary>
        /// Send some data to the server.
        /// </summary>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        public void SendToHost(Packet packet, PacketPriority priority)
        {
            SendToHost(SessionMessage.Data, packet, priority);
        }

        /// <summary>
        /// Send some data to a specific player.
        /// </summary>
        /// <param name="player">the player to send the data to.</param>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        public void SendToPlayer(Player<TPlayerData, TPacketizerContext> player, Packet packet, PacketPriority priority)
        {
            SendToEndPoint(playerAddresses[player.Number], SessionMessage.Data, packet, priority);
        }

        /// <summary>
        /// Send a message to all players in the game, and the server.
        /// </summary>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        public void SendToEveryone(Packet packet, PacketPriority priority)
        {
            SendToEveryone(SessionMessage.Data, packet, priority);
        }

        #endregion

        #region Internal send stuff

        /// <summary>
        /// Send some data of the given type to the server.
        /// </summary>
        /// <param name="type">the type of message that is sent.</param>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        internal abstract void SendToHost(SessionMessage type, Packet packet, PacketPriority priority);

        /// <summary>
        /// Internal variant for sending data to a specific host.
        /// </summary>
        /// <param name="remote">the remote machine to send the data to.</param>
        /// <param name="type">the type of message that is sent.</param>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        internal void SendToEndPoint(IPEndPoint remote, SessionMessage type, Packet packet, PacketPriority priority)
        {
            Packet wrapper = new Packet(5 + (packet != null ? packet.Length : 0));
            wrapper.Write((byte)type);
            wrapper.Write(packet);
            protocol.Send(wrapper, remote, priority);
        }

        /// <summary>
        /// As the internal Send, just for SendAll.
        /// </summary>
        /// <param name="type">the type of message to send.</param>
        /// <param name="type">the type of message that is sent.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        internal abstract void SendToEveryone(SessionMessage type, Packet packet, PacketPriority priority);

        #endregion

        #region Events

        /// <summary>
        /// Handle disconnects to players due to timeouts.
        /// </summary>
        protected abstract void HandlePlayerTimeout(object sender, EventArgs e);

        /// <summary>
        /// Handle data received from a remote machine.
        /// </summary>
        protected abstract void HandlePlayerData(object sender, EventArgs e);

        #endregion

        #region Utility methods

        /// <summary>
        /// Trigger a player data event, but only if the player that sent the packet
        /// is in the list of known players.
        /// </summary>
        protected void ConditionalOnPlayerData(ProtocolDataEventArgs args, Packet data)
        {
            // Check if this is a player of this session.
            int player = Array.IndexOf(playerAddresses, args.Remote);

            // If it is, forward the data.
            if (HasPlayer(player))
            {
                OnPlayerData(new PlayerDataEventArgs<TPlayerData, TPacketizerContext>(players[player], args, data));
            }
        }

        protected void OnPlayerJoined(PlayerEventArgs<TPlayerData, TPacketizerContext> e)
        {
            if (PlayerJoined != null)
            {
                PlayerJoined(this, e);
            }
        }

        protected void OnPlayerLeft(PlayerEventArgs<TPlayerData, TPacketizerContext> e)
        {
            if (PlayerLeft != null)
            {
                PlayerLeft(this, e);
            }
        }

        protected void OnPlayerData(PlayerDataEventArgs<TPlayerData, TPacketizerContext> e)
        {
            if (PlayerData != null)
            {
                PlayerData(this, e);
            }
        }

        #endregion
    }
}
