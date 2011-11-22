using System.Net;
using Engine.Serialization;

namespace Engine.Session
{
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

    public delegate void GameInfoReceivedEventHandler(IPEndPoint host, int numPlayers, int maxPlayers, Packet data);
    public delegate void JoinResponseEventHandler(bool success, JoinResponseReason reason, Packet data);

    /// <summary>
    /// Possible connection states for the client.
    /// </summary>
    public enum ClientState
    {
        Unconnected,
        Connecting,
        Connected
    }

    public interface IClientSession : ISession
    {

        /// <summary>
        /// Called when we receive information about an open game.
        /// Only possibly called after Search() was called.
        /// </summary>
        event GameInfoReceivedEventHandler GameInfoReceived;

        /// <summary>
        /// Called when we successfully joined a server (i.e. Join() was
        /// called and the server accepted us).
        /// </summary>
        event JoinResponseEventHandler JoinResponse;

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
