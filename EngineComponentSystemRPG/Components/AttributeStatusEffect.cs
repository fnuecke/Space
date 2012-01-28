using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// A status effect that modifies attributes.
    /// </summary>
    public sealed class AttributeStatusEffect<TAttribute> : AbstractStatusEffect
        where TAttribute : struct
    {
        #region Fields

        /// <summary>
        /// The actual attribute modifiers which are applied.
        /// </summary>
        public IList<AttributeModifier<TAttribute>> Modifiers;

        #endregion
        
        #region Constructor

        public AttributeStatusEffect(IList<AttributeModifier<TAttribute>> value)
        {
            this.Modifiers = value;
        }

        public AttributeStatusEffect(AttributeModifier<TAttribute> value)
            : this(new[] { value })
        {
        }

        public AttributeStatusEffect()
        {
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Modifiers);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Modifiers = packet.ReadPacketizables<AttributeModifier<TAttribute>>();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            foreach (var modifier in Modifiers)
            {
                modifier.Hash(hasher);
            }
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (AttributeStatusEffect<TAttribute>)base.DeepCopy(into);

            if (copy == into)
            {
                if (copy.Modifiers.Count != Modifiers.Count)
                {
                    copy.Modifiers = new AttributeModifier<TAttribute>[Modifiers.Count];
                }
            }
            else
            {
                copy.Modifiers = new AttributeModifier<TAttribute>[Modifiers.Count];
            }

            for (int i = 0; i < Modifiers.Count; i++)
            {
                copy.Modifiers[i] = Modifiers[i].DeepCopy(copy.Modifiers[i]);
            }

            return copy;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Modifiers = [" + string.Join(", ", Modifiers) + "]";
        }

        #endregion
    }
}
