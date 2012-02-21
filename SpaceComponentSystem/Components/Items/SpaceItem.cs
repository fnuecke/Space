﻿using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Base class for items of our space game.
    /// </summary>
    public class SpaceItem : Item
    {
        #region Fields

        /// <summary>
        /// The quality level of this item.
        /// </summary>
        public ItemQuality Quality;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Quality = ((SpaceItem)other).Quality;

            return this;
        }

        /// <summary>
        /// Creates a new item with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        public SpaceItem Initialize(string name, string iconName, ItemQuality quality)
        {
            base.Initialize(name, iconName);

            this.Quality = quality;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Quality = ItemQuality.Common;
        }

        #endregion

        #region Serialization

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
                .Write((byte)Quality);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Quality = (ItemQuality)packet.ReadByte();
        }

        #endregion
    }
}
