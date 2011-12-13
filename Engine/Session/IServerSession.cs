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
        /// <param name="player">the player to kick.</param>
        void Disconnect(Player<TPlayerData, TPacketizerContext> player);
        
        /// <summary>
        /// Sends a data message with the the specified packet as its data to the specified player.
        /// </summary>
        /// <param name="packet">The data to send.</param>
        void SendTo(Player<TPlayerData, TPacketizerContext> player, Packet packet);
    }
}
