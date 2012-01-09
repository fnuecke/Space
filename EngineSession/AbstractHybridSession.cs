using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Engine.Network;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Session
{
    public abstract class AbstractHybridSession : ISession
    {
        #region Types

        /// Message types sessions send around for internal logic.
        /// </summary>
        protected enum SessionMessage
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
            /// Data packet to be handled by someone else.
            /// </summary>
            Data
        }

        #endregion

        #region Constants

        /// <summary>
        /// The default multicast group address we'll use for searching games.
        /// </summary>
        protected static readonly IPEndPoint _defaultMulticastEndpoint = new IPEndPoint(new IPAddress(new byte[] { 224, 1, 33, 7 }), 51337);

        /// <summary>
        /// Udp header to use to identify messages part of our protocol.
        /// </summary>
        protected static readonly byte[] _udpHeader;

        #endregion

        #region Events

        /// <summary>
        /// Called when a new player joins the session.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerJoined;

        /// <summary>
        /// Called when a player left the session.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerLeft;

        /// <summary>
        /// Called when the server sent data.
        /// </summary>
        public event EventHandler<SessionDataEventArgs> Data;

        #endregion

        #region Properties

        /// <summary>
        /// Get a list of all players in the game.
        /// </summary>
        public IEnumerable<Player> AllPlayers { get { for (int i = 0; i < MaxPlayers; i++) { if (HasPlayer(i)) { yield return GetPlayer(i); } } } }

        /// <summary>
        /// Number of players currently in the game.
        /// </summary>
        public int NumPlayers { get; protected set; }

        /// <summary>
        /// Maximum number of player possible in this game.
        /// </summary>
        public int MaxPlayers { get; protected set; }

        /// <summary>
        /// Protocol information for this session (TCP only).
        /// </summary>
        public IProtocolInfo Information { get { return _information; } }

        #endregion

        #region Fields

        /// <summary>
        /// The connection to the server, used to (unreliably) send data.
        /// </summary>
        protected UdpProtocol _udp;

        /// <summary>
        /// Information on data we send and receive via tcp.
        /// </summary>
        protected ProtocolInfo _information;

        /// <summary>
        /// List of all players known to be in this session.
        /// </summary>
        protected Player[] _players;

        #endregion
        
        #region Construction / Destruction

        static AbstractHybridSession()
        {
            // Get ourselves a unique header, based on the program we're running in.
            _udpHeader = BitConverter.GetBytes(
                new Hasher().
                    Put(Encoding.UTF8.GetBytes(System.Reflection.Assembly.GetExecutingAssembly().GetName().FullName)).
                    Put(Encoding.UTF8.GetBytes(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())).
                    Value);
        }

        public AbstractHybridSession(UdpProtocol udp)
        {
            _udp = udp;
            _udp.Data += HandleUdpData;

            _information = new ProtocolInfo(60);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_udp != null)
                {
                    _udp.Dispose();
                    _udp = null;
                }

                _players = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called when the session needs to be updated. This should be called
        /// regularly (in each game's Update) to ensure proper flow of network
        /// traffic.
        /// </summary>
        public virtual void Update()
        {
            // Drive UDP network.
            _udp.Receive();
        }

        /// <summary>
        /// Get a player with the given number that's in the session.
        /// </summary>
        /// <param name="playerNumber">the number of the player.</param>
        /// <returns>the player in the session with the given number.</returns>
        public Player GetPlayer(int playerNumber)
        {
            if (playerNumber < 0 || playerNumber >= MaxPlayers)
            {
                throw new ArgumentException("playerNumber");
            }
            return (_players == null) ? null : _players[playerNumber];
        }

        /// <summary>
        /// Checks if a player with the given number exists in the session.
        /// </summary>
        /// <param name="playerNumber">the number to check.</param>
        /// <returns>whether a player with that number is in the session.</returns>
        public bool HasPlayer(int playerNumber)
        {
            if (playerNumber < 0 || playerNumber >= MaxPlayers)
            {
                throw new ArgumentException("playerNumber");
            }
            return (_players != null) && (_players[playerNumber] != null);
        }

        /// <summary>
        /// Checks if the given player is in this session. This does not
        /// only check the player's number, but also if this very instance
        /// came from this session.
        /// </summary>
        /// <param name="player">the player to check.</param>
        /// <returns>if the player is in the session or not.</returns>
        public bool HasPlayer(Player player)
        {
            return HasPlayer(player.Number) && GetPlayer(player.Number) == player;
        }

        /// <summary>
        /// Received some data from a client, let's see what we got.
        /// </summary>
        protected abstract void HandleUdpData(object sender, ProtocolDataEventArgs e);

        #endregion

        #region Sending

        /// <summary>
        /// Sends a data message with the the specified packet as its data.
        /// </summary>
        /// <param name="packet">The data to send.</param>
        public void Send(Packet packet)
        {
            Send(SessionMessage.Data, packet);
        }

        /// <summary>
        /// Sends a data-less message of the specified type.
        /// </summary>
        /// <param name="type">The type of the data-less message to send.</param>
        protected void Send(SessionMessage type)
        {
            Send(type, null);
        }

        /// <summary>
        /// Sends a message of the specified type, with the specified data.
        /// </summary>
        /// <param name="type">The type of the message to send.</param>
        /// <param name="packet">The data to send.</param>
        protected abstract void Send(SessionMessage type, Packet packet);

        #endregion

        #region Event Dispatching

        protected void OnPlayerJoined(PlayerEventArgs e)
        {
            if (PlayerJoined != null)
            {
                PlayerJoined(this, e);
            }
        }

        protected void OnPlayerLeft(PlayerEventArgs e)
        {
            if (PlayerLeft != null)
            {
                PlayerLeft(this, e);
            }
        }

        protected void OnData(SessionDataEventArgs e)
        {
            if (Data != null)
            {
                Data(this, e);
            }
        }

        #endregion
    }
}
