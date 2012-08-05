using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// A status effect that modifies attributes.
    /// </summary>
    public sealed class AttributeStatusEffect<TAttribute> : StatusEffect
        where TAttribute : struct
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The actual attribute modifiers which are applied.
        /// </summary>
        public IList<AttributeModifier<TAttribute>> Modifiers;

        #endregion
        
        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Modifiers = ((AttributeStatusEffect<TAttribute>)other).Modifiers;

            return this;
        }

        /// <summary>
        /// Initialize with the specified modifiers.
        /// </summary>
        /// <param name="value">The value.</param>
        public AttributeStatusEffect<TAttribute> Initialize(IList<AttributeModifier<TAttribute>> value)
        {
            Modifiers = value;

            return this;
        }

        /// <summary>
        /// Initialize with the specified modifier.
        /// </summary>
        /// <param name="value">The value.</param>
        public AttributeStatusEffect<TAttribute> Initialize(AttributeModifier<TAttribute> value)
        {
            return Initialize(new[] { value });
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Modifiers = null;
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

        #region ToString

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
