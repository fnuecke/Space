using System;
using System.Collections.Generic;
using Engine.Serialization;

namespace Engine.Session
{
    /// <summary>
    /// <summary>
    /// Common interface for sessions of either server or client type.
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Called when a new player joins the session.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerJoined;

        /// <summary>
        /// Called when a player left the session.
        /// </summary>
        event EventHandler<PlayerEventArgs> PlayerLeft;

        /// <summary>
        /// Called when a player sent data.
        /// </summary>
        event EventHandler<SessionDataEventArgs> Data;

        /// <summary>
        /// Get a list of all players in the game.
        /// </summary>
        IEnumerable<Player> AllPlayers { get; }

        /// <summary>
        /// Number of players currently in the game.
        /// </summary>
        int NumPlayers { get; }

        /// <summary>
        /// Maximum number of player possible in this game.
        /// </summary>
        int MaxPlayers { get; }

        /// <summary>
        /// Get info on the player with the given number.
        /// </summary>
        /// <param name="playerNumber">the number of the player.</param>
        /// <returns>information on the player.</returns>
        Player GetPlayer(int playerNumber);

        /// <summary>
        /// Check if the player with the given number exists.
        /// </summary>
        /// <param name="playerNumber">the number of the player to check.</param>
        /// <returns><c>true</c> if the player is in the session.</returns>
        bool HasPlayer(int playerNumber);

        /// <summary>
        /// Check if the player with the given number exists.
        /// </summary>
        /// <param name="player">the player to check.</param>
        /// <returns><c>true</c> if the player is in the session.</returns>
        bool HasPlayer(Player player);

        /// <summary>
        /// Send some data to the server.
        /// </summary>
        /// <param name="packet">the data to send.</param>
        void Send(Packet packet);

        /// <summary>
        /// Called when the session needs to be updated. This should be called
        /// regularly (in each game's Update) to ensure proper flow of network
        /// traffic.
        /// </summary>
        void Update();
    }
}
