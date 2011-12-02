using System;
using System.Net;
using Engine.Network;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// Event args used for <see cref="Engine.Session.ISession#PlayerJoined"/> and
    /// <see cref="Engine.Session.ISession#PlayerLeft"/>.
    /// </summary>
    public class PlayerEventArgs<TPlayerData, TPacketizerContext> : EventArgs
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The player the event applies to.
        /// </summary>
        public Player<TPlayerData, TPacketizerContext> Player { get; private set; }

        public PlayerEventArgs(Player<TPlayerData, TPacketizerContext> player)
        {
            this.Player = player;
        }
    }

    /// <summary>
    /// Event args used for <see cref="Engine.Session.ISession#PlayerData"/>.
    /// </summary>
    public class PlayerDataEventArgs<TPlayerData, TPacketizerContext> : PlayerEventArgs<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The data received from the player.
        /// </summary>
        public Packet Data { get; private set; }

        /// <summary>
        /// This data was sent by the server (also means <c>Player == null</c>).
        /// </summary>
        public bool IsFromServer { get; private set; }

        /// <summary>
        /// Inner event args that triggered this one.
        /// </summary>
        private ProtocolDataEventArgs innerArgs;

        public PlayerDataEventArgs(Player<TPlayerData, TPacketizerContext> player, ProtocolDataEventArgs innerArgs, Packet data)
            : base(player)
        {
            this.Data = data;
            this.innerArgs = innerArgs;
            this.IsFromServer = (player == null);
        }

        /// <summary>
        /// Called by listeners to signal the event was handled.
        /// </summary>
        public void Consume()
        {
            innerArgs.Consume();
        }
    }

    /// <summary>
    /// Event args used to notifiy clients of info received from a server
    /// about a running session.
    /// </summary>
    public class GameInfoReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The address of the machine hosting the session.
        /// </summary>
        public IPEndPoint Host { get; private set; }

        /// <summary>
        /// The number of players currently in the session.
        /// </summary>
        public int NumPlayers { get; private set; }

        /// <summary>
        /// The maximum number of players in the session.
        /// </summary>
        public int MaxPlayers { get; private set; }

        /// <summary>
        /// Any additional data the server sent together with the response.
        /// </summary>
        public Packet Data { get; private set; }

        public GameInfoReceivedEventArgs(IPEndPoint host, int numPlayers, int maxPlayers, Packet data)
        {
            this.Host = host;
            this.NumPlayers = numPlayers;
            this.MaxPlayers = maxPlayers;
            this.Data = data;
        }
    }

    public enum JoinResponseReason
    {
        /// <summary>
        /// Join was successful!
        /// </summary>
        Success,

        /// <summary>
        /// Unknown reason a join failed (invalid packet?).
        /// </summary>
        Unknown,

        /// <summary>
        /// The game we tried to join is already full.
        /// </summary>
        GameFull,

        /// <summary>
        /// Server says we're already in the game we're trying to join.
        /// </summary>
        AlreadyInGame,

        /// <summary>
        /// The name we provided was refused by the server (e.g. because it was empty?).
        /// </summary>
        InvalidName,

        /// <summary>
        /// Response we got from the server was invalid.
        /// </summary>
        InvalidServerData,

        /// <summary>
        /// Failed establishing a connection to the server.
        /// </summary>
        ConnectionFailed
    }

    /// <summary>
    /// Event args for join responses as dispatched on clients.
    /// </summary>
    public class JoinResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Tells whether the join request was successful or failed.
        /// </summary>
        public bool WasSuccess { get; private set; }

        /// <summary>
        /// The reason for the state of <c>WasSuccess</c>.
        /// </summary>
        public JoinResponseReason Reason { get; private set; }

        /// <summary>
        /// Any additional data the server sent with the answer.
        /// </summary>
        public Packet Data { get; private set; }

        public JoinResponseEventArgs(bool wasSuccess, JoinResponseReason reason, Packet data)
        {
            this.WasSuccess = wasSuccess;
            this.Reason = reason;
            this.Data = data;
        }
    }

    /// <summary>
    /// Event args for handling join or info requests on servers.
    /// </summary>
    public class RequestEventArgs : EventArgs
    {
        /// <summary>
        /// Data that should be sent should be written to this packet.
        /// </summary>
        public Packet Data { get; set; }

        public RequestEventArgs()
        {
            this.Data = new Packet();
        }
    }

    /// <summary>
    /// Event args for handling join or info requests on servers.
    /// </summary>
    public class JoinRequestEventArgs<TPlayerData, TPacketizerContext> : RequestEventArgs
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The player doing the joining.
        /// </summary>
        public Player<TPlayerData, TPacketizerContext> Player { get; private set; }

        /// <summary>
        /// Data that should be sent should be written to this packet.
        /// </summary>
        public TPlayerData PlayerData { get; private set; }

        /// <summary>
        /// May be set to tell the session to not allow this player to join.
        /// </summary>
        public bool ShouldBlock { get; set; }

        public JoinRequestEventArgs(Player<TPlayerData, TPacketizerContext> player, TPlayerData playerData)
        {
            this.Player = player;
            this.PlayerData = playerData;
        }

    }
}
