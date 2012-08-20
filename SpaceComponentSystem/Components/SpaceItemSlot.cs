using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents information about a single item slot.
    /// </summary>
    public sealed class SpaceItemSlot : ItemSlot
    {
        #region Fields
        
        /// <summary>
        /// The size of the slot, i.e. the maximum item size that can be
        /// fit into this slot.
        /// </summary>
        public ItemSlotSize Size = ItemSlotSize.Small;

        /// <summary>
        /// The offset of this item slots origin relative to its parent.
        /// </summary>
        public Vector2 Offset = Vector2.Zero;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherSlot = (SpaceItemSlot)other;
            Size = otherSlot.Size;
            Offset = otherSlot.Offset;

            return this;
        }

        /// <summary>
        /// Initializes the component to one primary equipment slot that allows
        /// the specified type id.
        /// </summary>
        /// <param name="typeId">The type id.</param>
        /// <param name="slotSize">Size of the slot.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        public SpaceItemSlot Initialize(int typeId, ItemSlotSize slotSize, Vector2 offset)
        {
            Initialize(typeId);

            Size = slotSize;
            Offset = offset;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Size = ItemSlotSize.Small;
            Offset = Vector2.Zero;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the specified item for this slot. It may only be
        /// put into this slot if the method returns true.
        /// </summary>
        /// <param name="item">The item to validate.</param>
        /// <returns>
        ///   <c>true</c> if the item may be equipped in this slot; <c>false</c> otherwise.
        /// </returns>
        public override bool Validate(Item item)
        {
            return item is SpaceItem && base.Validate(item) &&
                ((SpaceItem)item).SlotSize <= Size;
        }

        /// <summary>
        /// This utility method computes the overall offset of this and
        /// all parent slots. It will also automatically mirror offsets
        /// along the y-axis if the first offset along the y-axis is negative.
        /// This makes it easy to have symmetric wings, for example.
        /// </summary>
        /// <param name="offset">The base offset (relative to the slot).</param>
        /// <returns>The global offset, relative to the equipment origin.</returns>
        public Vector2 AccumulateOffset(Vector2 offset)
        {
            // Walk up the tree and accumulate offsets.
            var slot = this;

            // Keep track of the first (or, walking up: last) node that
            // has an actual offset along the x-axis.
            var potentialRootOffset = Vector2.Zero;
            do
            {
                // If there's an offset, mark it as the new top-level node.
                if (slot.Offset.Y != 0f)
                {
                    // Dump old top level offset into accumulator.
                    offset += potentialRootOffset;
                    // And set new top level offset.
                    potentialRootOffset = slot.Offset;
                }
                else
                {
                    // Nothing special (just offset along the y-axis).
                    offset += slot.Offset;
                }
            } while ((slot = (SpaceItemSlot)slot.Parent) != null);

            // Check if our top-level node is negative along the x-axis.
            if (potentialRootOffset.Y >= 0)
            {
                // Nope, just add.
                offset.Y = potentialRootOffset.Y + offset.Y;
            }
            else
            {
                // Yes, mirror child offsets.
                offset.Y = potentialRootOffset.Y - offset.Y;
            }
            // Add x-offset normally.
            offset.X = potentialRootOffset.X + offset.X;

            return offset;
        }
        
        /// <summary>
        /// This utility method computes the overall offset of this and
        /// all parent slots. It will also automatically mirror offsets
        /// along the x-axis if the first offset along the x-axis is negative.
        /// This makes it easy to have symmetric wings, for example.
        /// </summary>
        /// <returns>The global offset, relative to the equipment origin.</returns>
        public Vector2 AccumulateOffset()
        {
            return AccumulateOffset(Vector2.Zero);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)Size)
                .Write(Offset);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Size = (ItemSlotSize)packet.ReadByte();
            Offset = packet.ReadVector2();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put((byte)Size);
            hasher.Put(Offset);
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
            return base.ToString() + "Size=" + Size + ", Offset=" + Offset.X.ToString(CultureInfo.InvariantCulture) + ":" + Offset.Y.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
