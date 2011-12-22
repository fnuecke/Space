using System;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents the acceleration of an object.
    /// </summary>
    public sealed class Acceleration : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The directed acceleration of the object.
        /// </summary>
        public FPoint Value { get; set; }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet.Write(Value);
        }

        public override void Depacketize(Packet packet)
        {
            Value = packet.ReadFPoint();
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Value.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Value.Y.RawValue));
        }

        #endregion
    }
}
