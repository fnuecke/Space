﻿using System;
using Engine.ComponentSystem.Parameterizations;
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
    public sealed class Acceleration : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The directed acceleration of the object.
        /// </summary>
        public Vector2 Value { get; set; }

        #endregion

        #region Constructor

        public Acceleration(Vector2 acceleration)
        {
            this.Value = acceleration;
        }

        public Acceleration()
            : this(Vector2.Zero)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the velocity based on this acceleration.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var velocity = Entity.GetComponent<Velocity>();

            // Apply acceleration if velocity is available.
            if (velocity != null)
            {
                velocity.Value += Value;
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Value);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadVector2();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Value.X));
            hasher.Put(BitConverter.GetBytes(Value.Y));
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + Value.ToString();
        }

        #endregion
    }
}
