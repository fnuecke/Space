using System;
using System.Net;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    ///     Used for <see cref="Engine.Session.ISession#PlayerJoined"/> and
    ///     <see cref="Engine.Session.ISession#PlayerLeft"/>.
    /// </summary>
    public sealed class PlayerEventArgs : EventArgs
    {
        /// <summary>The player the event applies to.</summary>
        public Player Player { get; private set; }

        public PlayerEventArgs(Player player)
        {
            Player = player;
        }
    }

    /// <summary>
    ///     Sued for <see cref="Engine.Session.ISession#Data"/>.
    /// </summary>
    public abstract class SessionDataEventArgs : EventArgs
    {
        /// <summary>The data received.</summary>
        public IReadablePacket Data { get; private set; }

        /// <summary>Initializes this data event with the specified packet.</summary>
        /// <param name="data"></param>
        protected SessionDataEventArgs(IReadablePacket data)
        {
            Data = data;
        }
    }

    /// <summary>
    ///     Used for a server's <c>Data</c> event.
    /// </summary>
    public sealed class ServerDataEventArgs : SessionDataEventArgs
    {
        /// <summary>The player the event applies to.</summary>
        public Player Player { get; private set; }

        public ServerDataEventArgs(IReadablePacket data, Player player)
            : base(data)
        {
            Player = player;
        }
    }

    /// <summary>
    ///     Used for a client's <c>Data</c> event.
    /// </summary>
    public sealed class ClientDataEventArgs : SessionDataEventArgs
    {
        /// <summary>Whether this is an authoritative message (came from the server) or not.</summary>
        public bool IsAuthoritative { get; private set; }

        public ClientDataEventArgs(IReadablePacket data, bool isAuthoritative)
            : base(data)
        {
            IsAuthoritative = isAuthoritative;
        }
    }

    /// <summary>
    ///     Used for <see cref="Engine.Session.IClientSession#GameInfoReceived"/>.
    /// </summary>
    public sealed class GameInfoReceivedEventArgs : EventArgs
    {
        /// <summary>The address of the machine hosting the session.</summary>
        public IPEndPoint Host { get; private set; }

        /// <summary>The number of players currently in the session.</summary>
        public int PlayerCount { get; private set; }

        /// <summary>The maximum number of players in the session.</summary>
        public int MaxPlayers { get; private set; }

        /// <summary>Any additional data the server sent together with the response.</summary>
        public IReadablePacket Data { get; private set; }

        public GameInfoReceivedEventArgs(IPEndPoint host, int playerCount, int maxPlayers, IReadablePacket data)
        {
            Host = host;
            PlayerCount = playerCount;
            MaxPlayers = maxPlayers;
            Data = data;
        }
    }

    /// <summary>
    ///     Used for <see cref="Engine.Session.IClientSession#JoinResponse"/>.
    /// </summary>
    public sealed class JoinResponseEventArgs : EventArgs
    {
        /// <summary>Any additional data the server sent with the answer.</summary>
        public IReadablePacket Data { get; private set; }

        public JoinResponseEventArgs(IReadablePacket data)
        {
            Data = data;
        }
    }

    /// <summary>
    ///     Used for <see cref="Engine.Session.IServerSession#GameInfoRequested"/>.
    /// </summary>
    public class RequestEventArgs : EventArgs, IDisposable
    {
        /// <summary>Data that should be sent should be written to this packet.</summary>
        public IWritablePacket Data { get; private set; }

        public RequestEventArgs()
        {
            Data = new Packet();
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || Data == null)
            {
                return;
            }

            Data.Dispose();
            Data = null;
        }
    }

    /// <summary>
    ///     Used for <see cref="Engine.Session.IServerSession#JoinRequested"/>.
    /// </summary>
    public sealed class JoinRequestEventArgs : RequestEventArgs
    {
        /// <summary>The player doing the joining.</summary>
        public Player Player { get; private set; }

        /// <summary>May be set to tell the session to not allow this player to join.</summary>
        public bool ShouldBlock { get; set; }

        public JoinRequestEventArgs(Player player)
        {
            Player = player;
        }
    }
}