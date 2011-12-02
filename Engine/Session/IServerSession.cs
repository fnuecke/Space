using System;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// Interface for server side session implementations.
    /// </summary>
    public interface IServerSession<TPlayerData, TPacketizerContext> : ISession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Called when an unconnected client requests game info.
        /// </summary>
        event EventHandler<EventArgs> GameInfoRequested;

        /// <summary>
        /// A player is joining the game. Fill in any arbitrary data to send
        /// back to the joining client here.
        /// </summary>
        event EventHandler<EventArgs> JoinRequested;

        /// <summary>
        /// Kick a player from the session.
        /// </summary>
        /// <param name="player">the number of the player to kick.</param>
        void Kick(int player);
    }
}
