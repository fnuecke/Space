﻿using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Tells the player class via a player's ship.
    /// </summary>
    public sealed class PlayerClass : Component
    {
        #region Fields

        /// <summary>
        /// The player class of this ship's player.
        /// </summary>
        public PlayerClassType Value;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override void Initialize(Component other)
        {
            base.Initialize(other);

            Value = ((PlayerClass)other).Value;
        }

        /// <summary>
        /// Initialize with the specified player class.
        /// </summary>
        /// <param name="playerClass">The player class.</param>
        public void Initialize(PlayerClassType playerClass)
        {
            Value = playerClass;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Value = PlayerClassType.Default;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((int)Value);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = (PlayerClassType)packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Engine.Util.Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes((byte)Value));
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", PlayerClass = " + Value;
        }

        #endregion
    }
}
