using System;
using Engine.Data;
using Engine.Serialization;
using Engine.Util;

namespace Space.Data
{
    /// <summary>
    /// Represents a single hull item, which determines an entity's max life
    /// and health regeneration.
    /// </summary>
    public class HullModule : AbstractEntityModule<EntityAttributeType>
    {
        #region Fields
        
        /// <summary>
        /// The amount of health this hull provides.
        /// </summary>
        public float Health;

        /// <summary>
        /// The amount of health this hull regenerates per tick.
        /// </summary>
        public float HealthRegeneration;
        
        #endregion

        #region Constructor

        public HullModule()
        {
            AddAttributeTypeToInvalidate(EntityAttributeType.Health);
            AddAttributeTypeToInvalidate(EntityAttributeType.HealthRegeneration);
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(Health)
                .Write(HealthRegeneration);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Health = packet.ReadSingle();
            HealthRegeneration = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Health));
            hasher.Put(BitConverter.GetBytes(HealthRegeneration));
        }

        #endregion
    }
}
