using System;
using System.Collections.Generic;
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
    /// <typeparam name="TItem">The enum with possible item types.</typeparam>
    public sealed class Equipment : Component
    {
        #region Properties

        /// <summary>
        /// Enumerates over all currently equipped items.
        /// </summary>
        public IEnumerable<Entity> AllItems
        {
            get
            {
                foreach (var slots in _slots.Values)
                {
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] > 0)
                        {
                            yield return Entity.Manager.GetEntity(slots[i]);
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
        private Dictionary<Type, int[]> _slots = new Dictionary<Type, int[]>();

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
            if (_slots.ContainsKey(itemType))
            {
                return _slots[itemType].Length;
            }
            return 0;
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

            if (_slots.ContainsKey(type))
            {
                if (_slots[type].Length == count)
                {
                    // Nothing changes.
                    return;
                }

                var slots = new int[count];
                if (slots.Length < _slots[type].Length)
                {
                    // Less space than before, unequip stuff in removed slots.
                    for (int i = slots.Length; i < _slots[type].Length; i++)
                    {
                        Unequip(type, i);
                    }
                }
                else
                {
                    // Got more slots, copy over.
                    _slots[type].CopyTo(slots, 0);
                }

                // Set new slot array.
                _slots[type] = slots;
            }
            else
            {
                // Don't have that yet, create new array.
                _slots[type] = new int[count];
            }
        }

        #endregion

        #region Equipment

        /// <summary>
        /// Equip an item in the specified slot.
        /// </summary>
        /// <param name="item">The item to equip.</param>
        /// <param name="slot">The slot to equip it in.</param>
        public int Equip(Entity item, int slot)
        {
            if (item.UID < 1)
            {
                throw new ArgumentException("Invalid item, not part of the simulation.", "item");
            }
            var itemType = item.GetComponent<Item>();
            if (itemType == null)
            {
                throw new ArgumentException("Invalid item, does not have a type component.", "item");
            }
            Validate(itemType.GetType(), slot);

            var slots = _slots[itemType.GetType()];
            int itemInSolt = slots[slot];

            slots[slot] = item.UID;

            ItemAdded message;
            message.Item = item;
            message.Slot = slot;
            Entity.SendMessage(ref message);
            return itemInSolt;
        }

        /// <summary>
        /// Gets the item equipped in the specified slot.
        /// </summary>
        /// <typeparam name="TItem">The type of the item to get.</typeparam>
        /// <param name="slot">The slot to get the item from.</param>
        /// <returns>The item in that slot, or <c>null</c> if there is no item
        /// in that slot.</returns>
        public Entity GetItem<TItem>(int slot)
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
        public Entity GetItem(Type type, int slot)
        {
            Validate(type, slot);

            var slots = _slots[type];
            if (slots[slot] <= 0)
            {
                return null;
            }

            return Entity.Manager.GetEntity(slots[slot]);
        }

        /// <summary>
        /// Unequips an item from the specified slot.
        /// </summary>
        /// <typeparam name="TItem">The type of the item to unequip.</typeparam>
        /// <param name="slot">The slot to remove the item from.</param>
        /// <returns>The unequipped item.</returns>
        public Entity Unequip<TItem>(int slot)
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
        public Entity Unequip(Type type, int slot)
        {
            Validate(type, slot);

            var slots = _slots[type];
            if (slots[slot] <= 0)
            {
                return null;
            }

            var item = Entity.Manager.GetEntity(slots[slot]);
            slots[slot] = 0;

            ItemRemoved message;
            message.Item = item;
            message.Slot = slot;
            Entity.SendMessage(ref message);

            return item;
        }

        /// <summary>
        /// Validation helper for equipment parameters.
        /// </summary>
        /// <param name="type">The item type to check.</typeparam>
        /// <param name="slot">The slot to check.</param>
        private void Validate(Type type, int slot)
        {
            if (!_slots.ContainsKey(type))
            {
                throw new ArgumentException("Invalid item type.", "T");
            }
            var slots = _slots[type];
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

            packet.Write(_slots.Count);
            foreach (var type in _slots)
            {
                packet.Write(type.Key.AssemblyQualifiedName);
                packet.Write(_slots[type.Key].Length);
                for (int i = 0; i < _slots[type.Key].Length; i++)
                {
                    packet.Write(_slots[type.Key][i]);
                }
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

            _slots.Clear();
            var numSlotTypes = packet.ReadInt32();
            for (int i = 0; i < numSlotTypes; i++)
            {
                var typeName = packet.ReadString();
                var numSlots = packet.ReadInt32();
                var slots = new int[numSlots];
                for (int j = 0; j < numSlots; j++)
                {
                    slots[j] = packet.ReadInt32();
                }
                _slots[Type.GetType(typeName)] = slots;
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

            foreach (var slots in _slots.Values)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    hasher.Put(BitConverter.GetBytes(slots[i]));
                }
            }
        }

        #endregion

        #region Copying

        public override Component DeepCopy(Component into)
        {
            var copy = (Equipment)base.DeepCopy(into);

            if (copy == into)
            {
                copy._slots.Clear();
                foreach (var item in _slots)
                {
                    var slots = new int[item.Value.Length];
                    item.Value.CopyTo(slots, 0);
                    copy._slots[item.Key] = slots;
                }
            }
            else
            {
                copy._slots = new Dictionary<Type, int[]>();
                foreach (var type in _slots)
                {
                    var slots = new int[type.Value.Length];
                    type.Value.CopyTo(slots, 0);
                    copy._slots[type.Key] = slots;
                }
            }

            return copy;
        }

        #endregion
    }
}
