using Engine.Network;
using Microsoft.Xna.Framework;

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
        /// <param name="game">the game fro which to create the server.</param>
        /// <param name="protocol">the protocol to use (no protocol should ever be used by more than one session!)</param>
        /// <param name="maxPlayers">the maximum number of players allowed in this game.</param>
        /// <returns>the server session.</returns>
        public static IServerSession StartServer(Game game, IProtocol protocol, int maxPlayers)
        {
            return new ServerSession(game, protocol, maxPlayers);
        }

        /// <summary>
        /// Create a new client session using the given protocol.
        /// </summary>
        /// <param name="game">the game fro which to create the client.</param>
        /// <param name="protocol">the protocol to use (no protocol should ever be used by more than one session!)</param>
        /// <returns>the client session.</returns>
        public static IClientSession StartClient(Game game, IProtocol protocol)
        {
            return new ClientSession(game, protocol);
        }

        private SessionFactory()
        {
        }
    }
}
