using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Represents the inventory an entity, which can hold items represented by
    /// entities having a certain <c>ItemType</c>.
    /// </summary>
    public sealed class Equipment : Component
    {
        #region Properties

        /// <summary>
        /// Enumerates over all currently equipped items.
        /// </summary>
        public IEnumerable<int> AllItems
        {
            get
            {
                foreach (var slots in _allSlots)
                {
                    for (var i = 0; i < slots.Length; i++)
                    {
                        var slot = slots[i];
                        if (slot.HasValue)
                        {
                            yield return slot.Value;
                        }
                    }
                }
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The list of currently equipped items, in form of their entity id.
        /// </summary>
        private readonly List<int?[]> _allSlots = new List<int?[]>();

        /// <summary>
        /// Lookup table to get the slot list by its type. We don't store the
        /// slot lists here because the iteration order over dictionaries is
        /// not guaranteed.
        /// </summary>
        private readonly Dictionary<Type, int> _slotsByType = new Dictionary<Type, int>();

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherEquipment = (Equipment)other;

            _allSlots.Clear();
            for (var i = 0; i < otherEquipment._allSlots.Count; i++)
            {
                var slots = otherEquipment._allSlots[i];
                var slotsCopy = new int?[slots.Length];
                slots.CopyTo(slotsCopy, 0);
                _allSlots.Insert(i, slots);
            }

            _slotsByType.Clear();
            foreach (var entry in otherEquipment._slotsByType)
            {
                _slotsByType[entry.Key] = entry.Value;
            }

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _allSlots.Clear();
            _slotsByType.Clear();
        }

        #endregion

        #region Slots

        /// <summary>
        /// Get the number of slots of the specified item type.
        /// </summary>
        /// <typeparam name="TItem">The item type.</typeparam>
        /// <returns>The number of available slots.</returns>
        public int GetSlotCount<TItem>() where TItem : Item
        {
            return GetSlotCount(typeof(TItem));
        }

        /// <summary>
        /// Get the number of slots of the specified item type.
        /// </summary>
        /// <param name="itemType">The item type.</param>
        /// <returns>The number of available slots.</returns>
        public int GetSlotCount(Type itemType)
        {
            return _slotsByType.ContainsKey(itemType) ? _allSlots[_slotsByType[itemType]].Length : 0;
        }

        /// <summary>
        /// Sets the number of available slots for the specified item type.
        /// </summary>
        /// <typeparam name="TItem">The item type.</typeparam>
        /// <param name="count">The number of available slots.</param>
        public void SetSlotCount<TItem>(int count) where TItem : Item
        {
            SetSlotCount(typeof(TItem), count);
        }

        /// <summary>
        /// Sets the number of available slots for the specified item type.
        /// </summary>
        /// <param name="type">The item type.</param>
        /// <param name="count">The number of available slots.</param>
        public void SetSlotCount(Type type, int count)
        {
            if (!type.IsSubclassOf(typeof(Item)))
            {
                throw new ArgumentException("Invalid item type.", "type");
            }

            if (_slotsByType.ContainsKey(type))
            {
                var oldSlots = _allSlots[_slotsByType[type]];
                if (oldSlots.Length == count)
                {
                    // Nothing changes.
                    return;
                }

                var slots = new int?[count];
                if (slots.Length < oldSlots.Length)
                {
                    // Less space than before, unequip stuff in removed slots.
                    for (var i = slots.Length; i < oldSlots.Length; i++)
                    {
                        Unequip(type, i);
                    }

                    // Copy remainder over.
                    Array.Copy(oldSlots, 0, slots, 0, slots.Length);
                }
                else
                {
                    // Got more slots, copy over.
                    oldSlots.CopyTo(slots, 0);
                }

                // Set new slot array.
                _allSlots[_slotsByType[type]] = slots;
            }
            else
            {
                // Don't have that yet, create new array.
                _slotsByType[type] = _allSlots.Count;
                _allSlots.Add(new int?[count]);
            }
        }

        #endregion

        #region Equipment

        /// <summary>
        /// Equip an item in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to equip it in.</param>
        /// <param name="item">The item to equip.</param>
        /// <returns>The item previously in that slot.</returns>
        public int? Equip(int slot, int item)
        {
            // Check if its really an item.
            var itemComponent = Manager.GetComponent<Item>(item);
            if (itemComponent == null)
            {
                throw new ArgumentException("Entity does not have an Item component.", "item");
            }
            var itemType = itemComponent.GetType();
            Validate(itemType, slot);

            // Get whatever was in there before by unequipping.
            var itemInSlot = Unequip(itemType, slot);
            var slots = _allSlots[_slotsByType[itemType]];
            slots[slot] = item;

            // Send the message that a new item was equipped.
            ItemEquipped message;
            message.Entity = Entity;
            message.Item = item;
            message.Slot = slot;
            Manager.SendMessage(ref message);

            return itemInSlot;
        }

        /// <summary>
        /// Gets the item equipped in the specified slot.
        /// </summary>
        /// <typeparam name="TItem">The type of the item to get.</typeparam>
        /// <param name="slot">The slot to get the item from.</param>
        /// <returns>The item in that slot, or <c>null</c> if there is no item
        /// in that slot.</returns>
        public int? GetItem<TItem>(int slot)
            where TItem : Item
        {
            return GetItem(typeof(TItem), slot);
        }

        /// <summary>
        /// Gets the item equipped in the specified slot.
        /// </summary>
        /// <param name="type">The type of the item to get.</param>
        /// <param name="slot">The slot to get the item from.</param>
        /// <returns>The item in that slot, or <c>null</c> if there is no item
        /// in that slot.</returns>
        public int? GetItem(Type type, int slot)
        {
            Validate(type, slot);

            var slots = _allSlots[_slotsByType[type]];
            return slots[slot] <= 0 ? null : slots[slot];
        }

        /// <summary>
        /// Unequips an item from the specified slot.
        /// </summary>
        /// <typeparam name="TItem">The type of the item to unequip.</typeparam>
        /// <param name="slot">The slot to remove the item from.</param>
        /// <returns>The unequipped item.</returns>
        public int? Unequip<TItem>(int slot)
            where TItem : Item
        {
            return Unequip(typeof(TItem), slot);
        }

        /// <summary>
        /// Unequips an item from the specified slot.
        /// </summary>
        /// <param name="type">The type of the item to equip.</param>
        /// <param name="slot">The slot to remove the item from.</param>
        /// <returns>The unequipped item, or <c>null</c> if there was no item
        /// in that slot.</returns>
        public int? Unequip(Type type, int slot)
        {
            Validate(type, slot);

            var slots = _allSlots[_slotsByType[type]];
            if (slots[slot] <= 0)
            {
                return 0;
            }

            var item = slots[slot];
            slots[slot] = null;

            if (item.HasValue)
            {
                ItemUnequipped message;
                message.Entity = Entity;
                message.Item = item.Value;
                message.Slot = slot;
                Manager.SendMessage(ref message);
            }

            return item;
        }

        /// <summary>
        /// Validation helper for equipment parameters.
        /// </summary>
        /// <param name="type">The item type to check.</param>
        /// <param name="slot">The slot to check.</param>
        private void Validate(Type type, int slot)
        {
            if (!_slotsByType.ContainsKey(type))
            {
                throw new ArgumentException("Invalid item type.", "type");
            }
            var slots = _allSlots[_slotsByType[type]];
            if (slot < 0 || slot > slots.Length)
            {
                throw new ArgumentException("Invalid slot number.", "slot");
            }
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
            base.Packetize(packet);

            Debug.Assert(_allSlots.Count == _slotsByType.Count);

            packet.Write(_allSlots.Count);
            for (var i = 0; i < _allSlots.Count; i++)
            {
                var slots = _allSlots[i];

                // Write number of available slots.
                packet.Write(slots.Length);

                // Write number of actually occupied slots.
                var count = 0;
                for (var j = 0; j < slots.Length; j++)
                {
                    if (slots[j].HasValue)
                    {
                        ++count;
                    }
                }
                packet.Write(count);

                // Write the items in the occupied slots with the slot
                // they're equipped in.
                for (var j = 0; j < slots.Length; j++)
                {
                    var item = slots[j];
                    if (item.HasValue)
                    {
                        packet.Write(j);
                        packet.Write(item.Value);
                    }
                }
            }
            foreach (var entry in _slotsByType)
            {
                // Write slot type and number.
                packet.Write(entry.Key.AssemblyQualifiedName);
                packet.Write(entry.Value);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _allSlots.Clear();
            var numSlotTypes = packet.ReadInt32();
            for (var i = 0; i < numSlotTypes; i++)
            {
                var numSlots = packet.ReadInt32();
                var slots = new int?[numSlots];
                var numOccupied = packet.ReadInt32();
                for (var j = 0; j < numOccupied; j++)
                {
                    var index = packet.ReadInt32();
                    slots[index] = packet.ReadInt32();
                }
                _allSlots.Add(slots);
            }
            _slotsByType.Clear();
            for (var i = 0; i < numSlotTypes; i++)
            {
                var type = Type.GetType(packet.ReadString());
                Debug.Assert(type != null, "Got an invalid equipment type.");
                _slotsByType.Add(type, packet.ReadInt32());
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            foreach (var item in AllItems)
            {
                hasher.Put(item);
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
            return base.ToString() + ", SlotTypes=[" + string.Join(", ", _slotsByType.Keys) + "]" + ", Slots=[" + string.Join(", ", _allSlots) + "]";
        }

        #endregion
    }
}
