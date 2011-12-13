using System;
using System.Net;
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

    public interface SessionDataEventArgs
    {
        /// <summary>
        /// The data received.
        /// </summary>
        Packet Data { get; }
    }

    /// <summary>
    /// Event args used for a server's data event.
    /// </summary>
    public class ServerDataEventArgs<TPlayerData, TPacketizerContext>
        : PlayerEventArgs<TPlayerData, TPacketizerContext>, SessionDataEventArgs
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The data received from the player.
        /// </summary>
        public Packet Data { get; private set; }

        public ServerDataEventArgs(Player<TPlayerData, TPacketizerContext> player, Packet data)
            : base(player)
        {
            this.Data = data;
        }
    }

    /// <summary>
    /// Event args used for a client's data event.
    /// </summary>
    public class ClientDataEventArgs : EventArgs, SessionDataEventArgs
    {
        /// <summary>
        /// The data received from the player.
        /// </summary>
        public Packet Data { get; private set; }

        public ClientDataEventArgs(Packet data)
        {
            this.Data = data;
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

    /// <summary>
    /// Event args for join responses as dispatched on clients.
    /// </summary>
    public class JoinResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Any additional data the server sent with the answer.
        /// </summary>
        public Packet Data { get; private set; }

        public JoinResponseEventArgs(Packet data)
        {
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
