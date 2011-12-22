using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

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
        #region Properties

        /// <summary>
        /// The damping to apply to this objects movement (simulates friction).
        /// </summary>
        public Fixed Value { get; set; }

        /// <summary>
        /// Minimum velocity of an object before it is stopped (avoids jitter
        /// for very low velocity, due to which objects with damping never
        /// stop, even though they should).
        /// </summary>
        public Fixed MinVelocity { get; set; }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the velocity based on this friction.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            // Apply friction only if set to a positive value.
            if (Value > (Fixed)0)
            {
                // Get velocity.
                var velocity = Entity.GetComponent<Velocity>();

                // Only if a velocity is known.
                if (velocity != null)
                {
                    // Save previous velocity for stop check (due to MinVelocity).
                    var previousVelocity = velocity.Value.Norm;

                    // Apply friction.
                    velocity.Value = velocity.Value * ((Fixed)1 - Value);

                    // If we're below a certain minimum speed, just stop, otherwise
                    // it'd be hard to. We only stop if we were faster than the minimum,
                    // before application of friction. Otherwise we might have problems
                    // getting moving at all, if the acceleration is too low.
                    if (previousVelocity >= MinVelocity &&
                        velocity.Value.Norm < MinVelocity)
                    {
                        velocity.Value = FPoint.Zero;
                    }
                }
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
                .Write(Value)
                .Write(MinVelocity);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            Value = packet.ReadFixed();
            MinVelocity = packet.ReadFixed();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(Value.RawValue));
            hasher.Put(BitConverter.GetBytes(MinVelocity.RawValue));
        }

        #endregion
    }
}
