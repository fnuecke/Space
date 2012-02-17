using System;
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
            
            hasher.Put(BitConverter.GetBytes(Value));
            hasher.Put(BitConverter.GetBytes(StopVelocity));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
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

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", " + Value.ToString() + ", " + StopVelocity.ToString();
        }

        #endregion
    }
}
