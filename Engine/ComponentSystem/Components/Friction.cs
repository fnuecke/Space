using System;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents friction for an object.
    /// </summary>
    public class Friction : AbstractComponent
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

        #region Serialization / Hashing

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(Value);
            packet.Write(MinVelocity);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            Value = packet.ReadFixed();
            MinVelocity = packet.ReadFixed();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Value.RawValue));
            hasher.Put(BitConverter.GetBytes(MinVelocity.RawValue));
        }

        #endregion
    }
}
