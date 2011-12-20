using System;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents rotation speed of an object.
    /// </summary>
    public class Spin : AbstractComponent
    {
        #region Packetizer registration

        static Spin()
        {
            Packetizer.Register<Spin>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The current rotation speed of the object.
        /// </summary>
        public Fixed Value { get; set; }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet.Write(Value);
        }

        public override void Depacketize(Packet packet)
        {
            Value = packet.ReadFixed();
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Value.RawValue));
        }

        #endregion
    }
}
