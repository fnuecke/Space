﻿using System;
using System.Net;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// Used for <see cref="Engine.Session.ISession#PlayerJoined"/> and
    /// <see cref="Engine.Session.ISession#PlayerLeft"/>.
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        /// <summary>
        /// The player the event applies to.
        /// </summary>
        public Player Player { get; private set; }

        public PlayerEventArgs(Player player)
        {
            this.Player = player;
        }
    }

    /// <summary>
    /// Sued for <see cref="Engine.Session.ISession#Data"/>.
    /// </summary>
    public interface SessionDataEventArgs
    {
        /// <summary>
        /// The data received.
        /// </summary>
        Packet Data { get; }
    }

    /// <summary>
    /// Used for a server's <c>Data</c> event.
    /// </summary>
    public class ServerDataEventArgs : PlayerEventArgs, SessionDataEventArgs
    {
        /// <summary>
        /// The data received from the player.
        /// </summary>
        public Packet Data { get; private set; }

        public ServerDataEventArgs(Player player, Packet data)
            : base(player)
        {
            this.Data = data;
        }
    }

    /// <summary>
    /// Used for a client's <c>Data</c> event.
    /// </summary>
    public class ClientDataEventArgs : EventArgs, SessionDataEventArgs
    {
        /// <summary>
        /// The data received from the player.
        /// </summary>
        public Packet Data { get; private set; }

        /// <summary>
        /// Whether this is an authoritative message (came from the server)
        /// or not.
        /// </summary>
        public bool IsAuthoritative { get; private set; }

        public ClientDataEventArgs(Packet data, bool isAuthoritative)
        {
            this.Data = data;
            this.IsAuthoritative = isAuthoritative;
        }
    }

    /// <summary>
    /// Used for <see cref="Engine.Session.IClientSession#GameInfoReceived"/>.
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
    /// Used for <see cref="Engine.Session.IClientSession#JoinResponse"/>.
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
    /// Used for <see cref="Engine.Session.IServerSession#GameInfoRequested"/>.
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
    /// Used for <see cref="Engine.Session.IServerSession#JoinRequested"/>.
    /// </summary>
    public class JoinRequestEventArgs : RequestEventArgs
    {
        /// <summary>
        /// The player doing the joining.
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        /// May be set to tell the session to not allow this player to join.
        /// </summary>
        public bool ShouldBlock { get; set; }

        public JoinRequestEventArgs(Player player)
        {
            this.Player = player;
        }
    }
}
