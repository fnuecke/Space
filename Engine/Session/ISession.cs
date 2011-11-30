﻿using System;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.Session
{
    /// <summary>
    /// Common interface for sessions of either server or client type.
    /// </summary>
    public interface ISession<TPlayerData> : IGameComponent, IDisposable
        where TPlayerData : IPacketizable
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
        /// Reference to the data struct with info about the local player.
        /// </summary>
        /// <remarks>Shortcut for <c>session.GetPlayer(session.LocalPlayerNumber)</c>.</remarks>
        Player<TPlayerData> LocalPlayer { get; }

        /// <summary>
        /// Number of the local player.
        /// </summary>
        int LocalPlayerNumber { get; }

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
        /// <param name="player">the number of the player.</param>
        /// <returns>information on the player.</returns>
        Player<TPlayerData> GetPlayer(int player);

        /// <summary>
        /// Check if the player with the given number exists.
        /// </summary>
        /// <param name="player">the number of the player to check.</param>
        /// <returns><c>true</c> if such a player exists.</returns>
        bool HasPlayer(int player);

        /// <summary>
        /// Send some data to the server.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <param name="pollRate">lower (but > 0) means more urgent, if the protocol supports it.
        /// In case of the UDP protocol, 0 means the message is only sent once (no reliability guarantee).</param>
        void Send(Packet data, uint pollRate = 0);

        /// <summary>
        /// Send some data to a specific player.
        /// </summary>
        /// <param name="player">the player to send the data to.</param>
        /// <param name="data">the data to send.</param>
        /// <param name="pollRate">lower (but > 0) means more urgent, if the protocol supports it.
        /// In case of the UDP protocol, 0 means the message is only sent once (no reliability guarantee).</param>
        void Send(int player, Packet data, uint pollRate = 0);

        /// <summary>
        /// Send a message to all players in the game, and the server.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <param name="pollRate">lower (but > 0) means more urgent, if the protocol supports it.
        /// In case of the UDP protocol, 0 means the message is only sent once (no reliability guarantee).</param>
        void SendAll(Packet data, uint pollrate = 0);
    }
}
