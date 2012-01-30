using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;

namespace Space.Data
{
    /// <summary>
    /// Interface to allow storing profile in the settings.
    /// </summary>
    public interface IProfile : IPacketizable
    {
        /// <summary>
        /// A list of all existing profiles.
        /// </summary>
        IEnumerable<string> Profiles { get; }

        /// <summary>
        /// The profile name. This is equal to the profile's file name.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Creates a new profile with the specified name and the specified
        /// player class.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <param name="playerClass">The player class.</param>
        /// <exception cref="ArgumentException">profile name is invalid.</exception>
        void Create(string name, PlayerClassType playerClass);

        /// <summary>
        /// Loads this profile from disk. If loading fails this will default to
        /// a new profile with the fall-back character class.
        /// </summary>
        /// <exception cref="ArgumentException">profile name is invalid.</exception>
        void Load(string name);

        /// <summary>
        /// Stores the profile to disk, under the specified profile name.
        /// </summary>
        void Save();
        
        /// <summary>
        /// Take a snapshot of a character's current state in a running game.
        /// </summary>
        /// <param name="avatar">The avatar to take a snapshot of.</param>
        void Capture(Entity avatar);

        /// <summary>
        /// Restores a character snapshot stored in this profile.
        /// </summary>
        /// <param name="playerNumber">The number of the player in the game
        /// he is restored to.</param>
        /// <param name="manager">The entity manager to add the restored
        /// entities to.</param>
        /// <returns>The restored avatar.</returns>
        void Restore(int playerNumber, IEntityManager manager);
    }
}
