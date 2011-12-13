using System;
using System.Collections.Generic;
using Engine.Network;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    /// <summary>
    /// <summary>
    /// Common interface for sessions of either server or client type.
    /// </summary>
    public interface ISession<TPlayerData, TPacketizerContext> : IGameComponent, IDisposable
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Called when a new player joins the session.
        /// </summary>
        event EventHandler<EventArgs> PlayerJoined;

        /// <summary>
        /// Called when a player left the session.
        /// </summary>
        event EventHandler<EventArgs> PlayerLeft;

        /// <summary>
        /// Called when a player sent data.
        /// </summary>
        event EventHandler<EventArgs> PlayerData;

        /// <summary>
        /// Get a list of all players in the game.
        /// </summary>
        IEnumerable<Player<TPlayerData, TPacketizerContext>> AllPlayers { get; }

        /// <summary>
        /// Reference to the data struct with info about the local player.
        /// </summary>
        /// <remarks>Shortcut for <c>session.GetPlayer(session.LocalPlayerNumber)</c>.</remarks>
        Player<TPlayerData, TPacketizerContext> LocalPlayer { get; }

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
        Player<TPlayerData, TPacketizerContext> GetPlayer(int playerNumber);

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
        bool HasPlayer(Player<TPlayerData, TPacketizerContext> player);

        /// <summary>
        /// Send some data to the server.
        /// </summary>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        void SendToHost(Packet packet, PacketPriority priority);

        /// <summary>
        /// Send some data to a specific player.
        /// </summary>
        /// <param name="player">the player to send the data to.</param>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        void SendToPlayer(Player<TPlayerData, TPacketizerContext> player, Packet packet, PacketPriority priority);

        /// <summary>
        /// Send a message to all players in the game, and the server.
        /// </summary>
        /// <param name="packet">the data to send.</param>
        /// <param name="priority">the priority with which to deliver the packet.</param>
        void SendToEveryone(Packet packet, PacketPriority priority);
    }
}
