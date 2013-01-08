using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Messages;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// A single equipment slot. This is a recursive structure, due to items having
    /// the capability of having slots.
    /// </summary>
    public class ItemSlot : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the slot the item this slot belongs to is equipped in.
        /// </summary>
        public ItemSlot Parent
        {
            get { return _parent == 0 ? null : (ItemSlot)Manager.GetComponentById(_parent); }
            private set { _parent = value == null ? 0 : value.Id; }
        }

        /// <summary>
        /// Get the root slot of the equipment hierarchy this slot belongs to.
        /// </summary>
        public ItemSlot Root
        {
            get
            {
                var slot = this;
                while (slot.Parent != null)
                {
                    slot = slot.Parent;
                }
                return slot;
            }
        }

        /// <summary>
        /// The ID of the item equipped in that slot. The unequip message can be
        /// supressed by passing the complement of the new value (which will then
        /// be a negative number).
        /// </summary>
        public int Item
        {
            get { return _item; }
            set
            {
                if (value == Entity)
                {
                    throw new ArgumentException("Cannot equip item in itself.");
                }

                // Empty this slot.
                var oldItem = _item;
                _item = 0;

                // Check if there was something in this slot.
                if (oldItem > 0 && Manager.HasEntity(oldItem))
                {
                    // Send unequip message.
                    ItemUnequipped message;
                    message.Item = oldItem;
                    message.Slot = this;
                    Manager.SendMessage(message);

                    // Update hierarchy (after message, as it might need the Parent/Root property).
                    foreach (var slot in Manager.GetComponents(oldItem, TypeId))
                    {
                        ((ItemSlot)slot).Parent = null;
                    }
                }

                Debug.Assert(_item == 0, "Must not equip item to slot in its unequip message handler.");

                // Check if there's something in this slot now.
                if (value > 0)
                {
                    // Get item component.
                    var item = (Item)Manager.GetComponent(value, Components.Item.TypeId);

                    // Check if its really an item.
                    if (item == null)
                    {
                        throw new ArgumentException("Entity does not have an Item component.", "value");
                    }
                    if (!Validate(item))
                    {
                        throw new ArgumentException("Invalid item type for this slot.", "value");
                    }

                    // Set new slot value.
                    _item = value;

                    // Update hierarchy (before message, as it might need the Parent/Root property).
                    foreach (var slot in Manager.GetComponents(value, TypeId))
                    {
                        ((ItemSlot)slot).Parent = this;
                    }

                    // Send equip message.
                    ItemEquipped message;
                    message.Item = _item;
                    message.Slot = this;
                    Manager.SendMessage(message);
                }
            }
        }

        /// <summary>
        /// Enumerates over all slots including this and descendant slots.
        /// </summary>
        public IEnumerable<ItemSlot> AllSlots
        {
            get
            {
                // Use local stack for better performance.
                var slots = new Stack<ItemSlot>();
                // Push base slot.
                slots.Push(this);
                while (slots.Count > 0)
                {
                    // Get next slot in list.
                    var slot = slots.Pop();

                    // Return item in that slot.
                    yield return slot;

                    // Get that item and push child slots.
                    if (slot.Item > 0)
                    {
                        foreach (var childSlot in Manager.GetComponents(slot.Item, TypeId))
                        {
                            slots.Push((ItemSlot)childSlot);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates over all currently equipped items in this and descendant slots.
        /// </summary>
        public IEnumerable<int> AllItems
        {
            get { return AllSlots.Where(s => s.Item > 0).Select(s => s.Item); }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The type of item that can be equipped in the slot.
        /// </summary>
        public int SlotTypeId;

        /// <summary>
        /// Actual field storing value of equipped item.
        /// </summary>
        private int _item;

        /// <summary>
        /// ID of our parent node.
        /// </summary>
        private int _parent;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherSlot = (ItemSlot)other;
            SlotTypeId = otherSlot.SlotTypeId;
            _item = otherSlot._item;
            _parent = otherSlot._parent;

            return this;
        }

        /// <summary>
        /// Initializes the component to one primary equipment slot that allows
        /// the specified type id.
        /// </summary>
        /// <param name="typeId">The type id.</param>
        /// <returns></returns>
        public ItemSlot Initialize(int typeId)
        {
            SlotTypeId = typeId;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            SlotTypeId = 0;
            _item = 0;
            Parent = null;
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
        public virtual bool Validate(Item item)
        {
            return item != null &&
                (SlotTypeId == 0 || item.GetTypeId() == SlotTypeId);
        }

        /// <summary>
        /// This forces setting the item to a new value, only updating the set item's
        /// hierarchy (this slot as parent of slots in the item). This is intended to
        /// be used for post-processig depacketized item slots. This also sends the
        /// equipped event.
        /// </summary>
        /// <param name="item">The item value to set.</param>
        public void SetItemUnchecked(int item)
        {
            _item = item;
            if (_item > 0)
            {
                foreach (var slot in Manager.GetComponents(_item, TypeId))
                {
                    ((ItemSlot)slot).Parent = this;
                }

                // Send equip message.
                ItemEquipped message;
                message.Item = _item;
                message.Slot = this;
                Manager.SendMessage(message);
            }
        }

        #endregion
    }
}
