using System;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents the velocity of an object.
    /// </summary>
    public class Velocity : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The directed speed of the object.
        /// </summary>
        public FPoint Value { get; set; }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(Value);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            Value = packet.ReadFPoint();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Value.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Value.Y.RawValue));
        }

        #endregion
    }
}
