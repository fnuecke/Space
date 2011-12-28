using System;
using Engine.Data;
using Engine.Serialization;
using Engine.Util;

namespace Space.Data
{
    /// <summary>
    /// Represents a reactor item, which is used to store and produce energy.
    /// </summary>
    public class ReactorModule : AbstractEntityModule<EntityAttributeType>
    {
        #region Fields
        
        /// <summary>
        /// The amount of energy storage this reactor supplies.
        /// </summary>
        public float Energy;

        /// <summary>
        /// The amount of energy this reactor produces per tick.
        /// </summary>
        public float EnergyRegeneration;
        
        #endregion

        #region Constructor

        public ReactorModule()
        {
            AddAttributeTypeToInvalidate(EntityAttributeType.Energy);
            AddAttributeTypeToInvalidate(EntityAttributeType.EnergyRegeneration);
        }

        #endregion

        #region Serialization / Hashing / Cloning

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(Energy)
                .Write(EnergyRegeneration);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Energy = packet.ReadSingle();
            EnergyRegeneration = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Energy));
            hasher.Put(BitConverter.GetBytes(EnergyRegeneration));
        }

        #endregion
    }
}
