using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Represents friction for an object.
    /// 
    /// <para>
    /// Requires: <c>Velocity</c>.
    /// </para>
    /// </summary>
    public sealed class Friction : Component
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

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherFriction = (Friction)other;
            Value = otherFriction.Value;
            StopVelocity = otherFriction.StopVelocity;

            return this;
        }

        /// <summary>
        /// Initialize with the specified friction and stop velocity.
        /// </summary>
        /// <param name="value">The friction.</param>
        /// <param name="stopVelocity">The stop velocity.</param>
        public Friction Initialize(float value, float stopVelocity)
        {
            this.Value = value;
            this.StopVelocity = stopVelocity;

            return this;
        }

        /// <summary>
        /// Initialize with the specified friction.
        /// </summary>
        /// <param name="value">The friction.</param>
        public Friction Initialize(float value)
        {
            Initialize(value, 0);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Value = 0;
            StopVelocity = 0;
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
                .Write(Value)
                .Write(StopVelocity);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = packet.ReadSingle();
            StopVelocity = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(Value);
            hasher.Put(StopVelocity);
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
            return base.ToString() + ", Value=" + Value.ToString(CultureInfo.InvariantCulture) + ", StopVelocity=" + StopVelocity.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
