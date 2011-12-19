using System;
using Engine.Math;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents rotation speed of an object.
    /// </summary>
    public class Spin : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The current rotation speed of the object.
        /// </summary>
        public Fixed Value { get; set; }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(Value);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            Value = packet.ReadFixed();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Value.RawValue));
        }

        #endregion
    }
}
