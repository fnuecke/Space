using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Engine.Util;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single armor item, which determines an entity's armor
    /// rating.
    /// </summary>
    public sealed class Armor : Item
    {
        #region Fields
        
        /// <summary>
        /// The armor rating this armor provides.
        /// </summary>
        public float ArmorRating;

        #endregion

        #region Constructor

        public Armor(float armor)
        {
            this.ArmorRating = armor;
        }

        public Armor()
        {
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(ArmorRating);

            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            ArmorRating = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(ArmorRating));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Armor)base.DeepCopy(into);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
                copy.ArmorRating = ArmorRating;
            }

            return copy;
        }

        #endregion
    }
}
