﻿using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Base class for attributes attached to items.
    /// </summary>
    /// <typeparam name="TAttribute">The enum that holds the possible types of
    /// attributes.</typeparam>
    public sealed class Attribute<TAttribute> : AbstractComponent
        where TAttribute : struct
    {
        #region Fields

        /// <summary>
        /// The actual attribute modifier which is applied.
        /// </summary>
        public AttributeModifier<TAttribute> Modifier;

        #endregion

        #region Constructor

        public Attribute(AttributeModifier<TAttribute> value)
        {
            this.Modifier = value;
        }

        public Attribute()
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
                .Write(Modifier);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Modifier = packet.ReadPacketizable<AttributeModifier<TAttribute>>();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            Modifier.Hash(hasher);
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Attribute<TAttribute>)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Modifier = Modifier.DeepCopy(copy.Modifier);
            }
            else
            {
                copy.Modifier = Modifier.DeepCopy();
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
            return base.ToString() + ", Value = " + Modifier.ToString();
        }

        #endregion
    }

}