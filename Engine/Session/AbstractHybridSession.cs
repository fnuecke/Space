using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Engine.Network;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    public abstract class AbstractHybridSession : GameComponent, ISession
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
        protected readonly IPEndPoint DefaultMulticastEndpoint = new IPEndPoint(new IPAddress(new byte[] { 224, 1, 33, 7 }), 51337);

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
        public event EventHandler<EventArgs> Data;

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

        #endregion

        #region Fields

        /// <summary>
        /// The connection to the server, used to (unreliably) send data.
        /// </summary>
        protected UdpProtocol udp;

        /// <summary>
        /// Udp header to use to identify messages part of our protocol.
        /// </summary>
        protected byte[] udpHeader;

        /// <summary>
        /// Packetizer used for this session's game.
        /// </summary>
        protected IPacketizer packetizer;

        /// <summary>
        /// List of all players known to be in this session.
        /// </summary>
        protected Player[] players;

        #endregion
        
        #region Construction / Destruction

        public AbstractHybridSession(Game game)
            : base(game)
        {
            // Get ourselves a unique header, based on the program we're running in.
            udpHeader = BitConverter.GetBytes(
                new Hasher().
                    Put(Encoding.UTF8.GetBytes(game.GetType().FullName)).
                    Put(Encoding.UTF8.GetBytes(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())).
                    Value);
        }

        public override void Initialize()
        {
            packetizer = ((IPacketizer)Game.Services.GetService(typeof(IPacketizer))).CopyFor(this);

            udp.Data += HandleUdpData;

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                udp.Dispose();
            }

            base.Dispose(disposing);
        }
        
        public override void Update(GameTime gameTime)
        {
            // Drive UDP network.
            udp.Receive();

            base.Update(gameTime);
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
            return (players == null) ? null : players[playerNumber];
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
            return (players != null) && (players[playerNumber] != null);
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
        protected abstract void HandleUdpData(object sender, EventArgs e);

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

        protected void OnPlayerJoined(EventArgs e)
        {
            if (PlayerJoined != null)
            {
                PlayerJoined(this, e);
            }
        }

        protected void OnPlayerLeft(EventArgs e)
        {
            if (PlayerLeft != null)
            {
                PlayerLeft(this, e);
            }
        }

        protected void OnData(EventArgs e)
        {
            if (Data != null)
            {
                Data(this, e);
            }
        }

        #endregion
    }
}
