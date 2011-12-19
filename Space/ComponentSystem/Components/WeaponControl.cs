using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Controls whether weapons on an entity should be shooting.
    /// </summary>
    public class WeaponControl : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// Whether ima currently firin mah lazer or not.
        /// </summary>
        public bool IsShooting { get; set; }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return packet.Write(IsShooting);
        }

        public override void Depacketize(Packet packet)
        {
            IsShooting = packet.ReadBoolean();
        }

        public override void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(IsShooting));
        }

        #endregion
    }
}
