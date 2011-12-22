using System;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents friction for an object.
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

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet
                .Write(Value)
                .Write(MinVelocity);
        }

        public override void Depacketize(Packet packet)
        {
            Value = packet.ReadFixed();
            MinVelocity = packet.ReadFixed();
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Value.RawValue));
            hasher.Put(BitConverter.GetBytes(MinVelocity.RawValue));
        }

        #endregion
    }
}
