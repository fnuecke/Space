using System;
using Engine.ComponentSystem.Modules;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Modules
{
    /// <summary>
    /// Represents a single hull item, which determines an entity's max life
    /// and health regeneration.
    /// </summary>
    public class HullModule : AbstractModule<SpaceModifier>
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
            AddAttributeTypeToInvalidate(SpaceModifier.Health);
            AddAttributeTypeToInvalidate(SpaceModifier.HealthRegeneration);
        }

        #endregion

        #region Serialization / Hashing

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

        #region Copying

        public override AbstractModule<SpaceModifier> DeepCopy(AbstractModule<SpaceModifier> into)
        {
            var copy = (HullModule)base.DeepCopy(into);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
                copy.Health = Health;
                copy.HealthRegeneration = HealthRegeneration;
            }

            return copy;
        }

        #endregion
    }
}
