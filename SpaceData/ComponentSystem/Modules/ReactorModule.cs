using System;
using Engine.ComponentSystem.Modules;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Modules
{
    /// <summary>
    /// Represents a reactor item, which is used to store and produce energy.
    /// </summary>
    public class ReactorModule : AbstractModule<SpaceModifier>
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
            AddAttributeTypeToInvalidate(SpaceModifier.Energy);
            AddAttributeTypeToInvalidate(SpaceModifier.EnergyRegeneration);
        }

        #endregion

        #region Serialization / Hashing

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

        #region Copying

        public override AbstractModule<SpaceModifier> DeepCopy(AbstractModule<SpaceModifier> into)
        {
            var copy = (ReactorModule)base.DeepCopy(into);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
                copy.Energy = Energy;
                copy.EnergyRegeneration = EnergyRegeneration;
            }

            return copy;
        }

        #endregion
    }
}
