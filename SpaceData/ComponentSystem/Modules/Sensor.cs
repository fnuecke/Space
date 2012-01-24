using System;
using Engine.ComponentSystem.Modules;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Modules
{
    public sealed class Sensor : AbstractModule<SpaceModifier>
    {
        #region Fields

        /// <summary>
        /// The Range this Radar has
        /// </summary>
        public float Range;

        #endregion

        #region Constructor

        public Sensor()
        {
            AddAttributeTypeToInvalidate(SpaceModifier.SensorRange);
        }

        #endregion

        #region Serialization / Hashing

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

        #region Copying

        public override AbstractModule<SpaceModifier> DeepCopy(AbstractModule<SpaceModifier> into)
        {
            var copy = (Sensor)base.DeepCopy(into);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
                copy.Range = Range;
            }

            return copy;
        }

        #endregion
    }
}
