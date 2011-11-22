using Engine.Network;

namespace Engine.Session
{
    /// <summary>
    /// Factory for server and client sessions (hosting / joining).
    /// </summary>
    public sealed class SessionFactory
    {
        /// <summary>
        /// Create a new server session using the given protocol.
        /// </summary>
        /// <param name="protocol">the protocol to use (no protocol should ever be used by more than one session!)</param>
        /// <param name="maxPlayers">the maximum number of players allowed in this game.</param>
        /// <returns>the server session.</returns>
        public static IServerSession StartServer(IProtocol protocol, int maxPlayers)
        {
            return new ServerSession(protocol, maxPlayers);
        }

        /// <summary>
        /// Create a new client session using the given protocol.
        /// </summary>
        /// <param name="protocol">the protocol to use (no protocol should ever be used by more than one session!)</param>
        /// <returns>the client session.</returns>
        public static IClientSession StartClient(IProtocol protocol)
        {
            return new ClientSession(protocol);
        }

        private SessionFactory()
        {
        }
    }
}
