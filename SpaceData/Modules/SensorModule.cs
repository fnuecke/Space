using System;
using Engine.Data;
using Engine.Serialization;
using Engine.Util;

namespace Space.Data.Modules
{
    public class SensorModule : AbstractEntityModule<EntityAttributeType>
    {
        #region Fields

        /// <summary>
        /// The Range this Radar has
        /// </summary>
        public float Range;

        #endregion

        #region Constructor

        public SensorModule()
        {
            AddAttributeTypeToInvalidate(EntityAttributeType.SensorRange);
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(Range);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Range = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Range));
        }

        #endregion
    }
}
