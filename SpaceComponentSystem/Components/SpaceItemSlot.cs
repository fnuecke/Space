using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
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

            return this;
        }

        /// <summary>
        /// Initializes the component to one primary equipment slot that allows
        /// the specified type id.
        /// </summary>
        /// <param name="typeId">The type id.</param>
        /// <param name="slotSize">Size of the slot.</param>
        /// <returns></returns>
        public SpaceItemSlot Initialize(int typeId, ItemSlotSize slotSize)
        {
            Initialize(typeId);

            Size = slotSize;

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
                .Write((byte)Size);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Size = (ItemSlotSize)packet.ReadByte();
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
        }

        #endregion
    }
}
