﻿using System;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents the acceleration of an object.
    /// 
    /// <para>
    /// Requires: <c>Velocity</c>.
    /// </para>
    /// </summary>
    public sealed class Acceleration : Component
    {
        #region Fields

        /// <summary>
        /// The directed acceleration of the object.
        /// </summary>
        public Vector2 Value;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Value = ((Acceleration)other).Value;

            return this;
        }

        /// <summary>
        /// Initialize the component with the specified value.
        /// </summary>
        /// <param name="acceleration">The acceleration.</param>
        public Acceleration Initialize(Vector2 acceleration)
        {
            Value = acceleration;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Value = Vector2.Zero;
        }

        #endregion

        #region Serialization / Hashing

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
                .Write(Value);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadVector2();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Value.X));
            hasher.Put(BitConverter.GetBytes(Value.Y));
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
            return base.ToString() + ", Value = " + Value.ToString();
        }

        #endregion
    }
}
