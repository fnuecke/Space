using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Microsoft.Xna.Framework;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>Represents information about a single item slot.</summary>
    public sealed class SpaceItemSlot : ItemSlot
    {
        #region Fields

        /// <summary>The size of the slot, i.e. the maximum item size that can be fit into this slot.</summary>
        public ItemSlotSize Size = ItemSlotSize.Small;

        /// <summary>The offset of this item slots origin relative to its parent.</summary>
        public Vector2 Offset = Vector2.Zero;

        /// <summary>The rotation of this item slot relative to its parent.</summary>
        public float Rotation;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherSlot = (SpaceItemSlot) other;
            Size = otherSlot.Size;
            Offset = otherSlot.Offset;
            Rotation = otherSlot.Rotation;

            return this;
        }

        /// <summary>Initializes the component to one primary equipment slot that allows the specified type id.</summary>
        /// <param name="typeId">The type id.</param>
        /// <param name="slotSize">Size of the slot.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="rotation">The rotation.</param>
        /// <returns></returns>
        public SpaceItemSlot Initialize(int typeId, ItemSlotSize slotSize, Vector2 offset, float rotation)
        {
            Initialize(typeId);

            Size = slotSize;
            Offset = offset;
            Rotation = rotation;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Size = ItemSlotSize.Small;
            Offset = Vector2.Zero;
            Rotation = 0f;
        }

        #endregion

        #region Methods

        /// <summary>Validates the specified item for this slot. It may only be put into this slot if the method returns true.</summary>
        /// <param name="item">The item to validate.</param>
        /// <returns>
        ///     <c>true</c> if the item may be equipped in this slot; <c>false</c> otherwise.
        /// </returns>
        public override bool Validate(Item item)
        {
            return item is SpaceItem && base.Validate(item) &&
                   ((SpaceItem) item).RequiredSlotSize <= Size;
        }

        /// <summary>
        ///     This utility method computes the overall offset of this and all parent slots. It will also automatically
        ///     mirror offsets along the y-axis if the first offset along the y-axis is negative. This makes it easy to have
        ///     symmetric wings, for example.
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
                var slotOffset = slot.Offset;
                var parent = slot.Parent;
                if (parent != null)
                {
                    var parentItem =
                        (SpaceItem) Manager.GetComponent(parent.Item, Engine.ComponentSystem.RPG.Components.Item.TypeId);
                    slotOffset *= parentItem.RequiredSlotSize.Scale();
                }
                // If there's an offset, mark it as the new top-level node.
                if (slot.Offset.Y != 0f)
                {
                    // Dump old top level offset into accumulator.
                    offset += potentialRootOffset;
                    // And set new top level offset.
                    potentialRootOffset = slotOffset;
                }
                else
                {
                    // Nothing special (just offset along the y-axis).
                    offset += slotOffset;
                }
            } while ((slot = (SpaceItemSlot) slot.Parent) != null);

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
        ///     This utility method computes the overall offset of this and all parent slots. It will also automatically
        ///     mirror offsets along the x-axis if the first offset along the x-axis is negative. This makes it easy to have
        ///     symmetric wings, for example.
        /// </summary>
        /// <returns>The global offset, relative to the equipment origin.</returns>
        public Vector2 AccumulateOffset()
        {
            return AccumulateOffset(Vector2.Zero);
        }

        /// <summary>Accumulates the rotation of this slot, relative to the root node and adds the specified rotation.</summary>
        /// <param name="rotation">The base rotation.</param>
        /// <returns>The rotation for items in this slot.</returns>
        public float AccumulateRotation(float rotation = 0f)
        {
            // Walk up the tree and accumulate offsets.
            var slot = this;

            // Keep track of the first (or, walking up: last) node that
            // has an actual offset along the x-axis.
            var mirror = false;
            do
            {
                // Accumulate the slot rotations.
                rotation += slot.Rotation;
                // If there's an offset, mark it as the new top-level node.
                if (slot.Offset.Y != 0f)
                {
                    mirror = slot.Offset.Y < 0f;
                }
            } while ((slot = (SpaceItemSlot) slot.Parent) != null);

            return mirror ? -rotation : rotation;
        }

        /// <summary>
        ///     Accumulates offset and rotation at the same time, which is more efficient than doing so separately. The
        ///     specified values will be used as base offset and base rotation.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="rotation">The rotation.</param>
        public void Accumulate(ref Vector2 offset, ref float rotation)
        {
            // Walk up the tree and accumulate offsets.
            var slot = this;

            // Keep track of the first (or, walking up: last) node that
            // has an actual offset along the x-axis.
            var potentialRootOffset = Vector2.Zero;
            do
            {
                var slotOffset = slot.Offset;
                var parent = slot.Parent;
                if (parent != null)
                {
                    var parentItem =
                        (SpaceItem) Manager.GetComponent(parent.Item, Engine.ComponentSystem.RPG.Components.Item.TypeId);
                    slotOffset *= parentItem.RequiredSlotSize.Scale();
                }
                // Accumulate the slot rotations.
                rotation += slot.Rotation;
                // If there's an offset, mark it as the new top-level node.
                if (slot.Offset.Y != 0f)
                {
                    // Dump old top level offset into accumulator.
                    offset += potentialRootOffset;
                    // And set new top level offset.
                    potentialRootOffset = slotOffset;
                }
                else
                {
                    // Nothing special (just offset along the y-axis).
                    offset += slotOffset;
                }
            } while ((slot = (SpaceItemSlot) slot.Parent) != null);

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
                // Also mirror rotation.
                rotation = -rotation;
            }
            // Add x-offset normally.
            offset.X = potentialRootOffset.X + offset.X;
        }

        #endregion
    }
}