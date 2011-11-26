using System;
using System.Net;

namespace Engine.Session
{
    /// <summary>
    /// Possible connection states for the client.
    /// </summary>
    public enum ClientState
    {
        Unconnected,
        Connecting,
        Connected
    }

    /// <summary>
    /// Interface for client side representations of a session.
    /// </summary>
    public interface IClientSession : ISession
    {
        /// <summary>
        /// Called when we receive information about an open game.
        /// Only possibly called after Search() was called.
        /// </summary>
        event EventHandler<EventArgs> GameInfoReceived;

        /// <summary>
        /// Called when we successfully joined a server (i.e. Join() was
        /// called and the server accepted us).
        /// </summary>
        event EventHandler<EventArgs> JoinResponse;

        /// <summary>
        /// Current state of this session.
        /// </summary>
        ClientState ConnectionState { get; set; }

        /// <summary>
        /// Send a ping into the local network, looking for open games.
        /// </summary>
        void Search();

        /// <summary>
        /// Join a game on the given host.
        /// </summary>
        /// <param name="remote">the remote host that runs the session.</param>
        /// <param name="playerName">the with which to register.</param>
        /// <param name="data">additional data to be associated with our player (Player.Data).</param>
        void Join(IPEndPoint remote, string playerName, byte[] data);

        /// <summary>
        /// Leave the session.
        /// </summary>
        void Leave();
    }
}
