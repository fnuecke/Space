﻿using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    /// <summary>
    /// Factory for server and client sessions (hosting / joining).
    /// </summary>
    public static class SessionFactory
    {
        /// <summary>
        /// Create a new server session using the given protocol.
        /// </summary>
        /// <param name="game">the game fro which to create the server.</param>
        /// <param name="protocol">the protocol to use (no protocol should ever be used by more than one session!)</param>
        /// <param name="maxPlayers">the maximum number of players allowed in this game.</param>
        /// <returns>the server session.</returns>
        public static IServerSession<TPlayerData, TPacketizerContext> StartServer<TPlayerData, TPacketizerContext>(Game game, IProtocol protocol, int maxPlayers)
            where TPlayerData :IPacketizable<TPacketizerContext>, new()
        {
            return new ServerSession<TPlayerData, TPacketizerContext>(game, protocol, maxPlayers);
        }

        /// <summary>
        /// Create a new client session using the given protocol.
        /// </summary>
        /// <param name="game">the game fro which to create the client.</param>
        /// <param name="protocol">the protocol to use (no protocol should ever be used by more than one session!)</param>
        /// <returns>the client session.</returns>
        public static IClientSession<TPlayerData, TPacketizerContext> StartClient<TPlayerData, TPacketizerContext>(Game game, IProtocol protocol)
            where TPlayerData : IPacketizable<TPacketizerContext>, new()
        {
            return new ClientSession<TPlayerData, TPacketizerContext>(game, protocol);
        }
    }
}
