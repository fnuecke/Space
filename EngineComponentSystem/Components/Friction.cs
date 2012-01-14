using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents friction for an object.
    /// 
    /// <para>
    /// Requires: <c>Velocity</c>.
    /// </para>
    /// </summary>
    public sealed class Friction : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The damping to apply to this objects movement (simulates friction).
        /// </summary>
        public float Value;

        /// <summary>
        /// Minimum velocity of an object before it is stopped (avoids jitter
        /// for very low velocity, due to which objects with damping never
        /// stop, even though they should).
        /// </summary>
        public float StopVelocity;

        #endregion

        #region Constructor

        public Friction(float value, float stopVelocity)
        {
            this.Value = value;
            this.StopVelocity = stopVelocity;
        }

        public Friction(float value)
            : this(value, 0)
        {
        }

        public Friction()
            : this(0, 0)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the velocity based on this friction.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
            // Apply friction only if set to a positive value.
            if (Value > 0)
            {
                // Get velocity.
                var velocity = Entity.GetComponent<Velocity>();

                // Only if a velocity is known.
                if (velocity != null)
                {
                    // Save previous velocity for stop check (due to MinVelocity).
                    var previousVelocity = velocity.Value.LengthSquared();

                    // Apply friction.
                    velocity.Value = velocity.Value * (1 - Value);

                    // If we're below a certain minimum speed, just stop, otherwise
                    // it'd be hard to. We only stop if we were faster than the minimum,
                    // before application of friction. Otherwise we might have problems
                    // getting moving at all, if the acceleration is too low.
                    if (previousVelocity >= StopVelocity &&
                        velocity.Value.LengthSquared() < StopVelocity)
                    {
                        velocity.Value = Vector2.Zero;
                    }
                }
            }
        }
        
        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Value)
                .Write(StopVelocity);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadSingle();
            StopVelocity = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(Value));
            hasher.Put(BitConverter.GetBytes(StopVelocity));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Friction)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Value = Value;
                copy.StopVelocity = StopVelocity;
            }

            return copy;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + Value.ToString() + ", " + StopVelocity.ToString();
        }

        #endregion
    }
}
