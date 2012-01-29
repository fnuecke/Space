using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Messages;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Represents a player's inventory, with a list of items in it.
    /// </summary>
    public sealed class Inventory : AbstractComponent
    {
        #region Fields
        
        /// <summary>
        /// A list of items currently in this inventory.
        /// </summary>
        private List<int> _items = new List<int>();

        #endregion

        #region Accessors

        /// <summary>
        /// Adds the item to the inventory.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddItem(Entity item)
        {
            _items.Add(item.UID);
        }

        /// <summary>
        /// Removes the item from the inventory.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>Whether the item was successfully removed.</returns>
        public bool RemoveItem(Entity item)
        {
            return RemoveItem(item.UID);
        }

        /// <summary>
        /// Removes the item with the specified id from the inventory.
        /// </summary>
        /// <param name="entityUid">The entity uid of the item.</param>
        /// <returns>Whether the item was successfully removed.</returns>
        public bool RemoveItem(int entityUid)
        {
            return _items.Remove(entityUid);
        }

        /// <summary>
        /// Removes the item at the specified index from the inventory.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <returns>The removed item.</returns>
        public Entity RemoveItemAt(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new ArgumentException("Invalid index.");
            }
            var entity = Entity.Manager.GetEntity(_items[index]);
            _items.RemoveAt(index);
            return entity;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Check for removed entities.
        /// </summary>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is EntityRemoved)
            {
                // If an entity was removed from the game and it was in this
                // inventory, remove it here, too.
                var removed = (EntityRemoved)(ValueType)message;
                RemoveItem(removed.Entity);
            }
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
            base.Packetize(packet);

            packet.Write(_items.Count);
            for (int i = 0; i < _items.Count; i++)
            {
                packet.Write(_items[i]);
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

            _items.Clear();
            int numItems = packet.ReadInt32();
            for (int i = 0; i < numItems; i++)
            {
                _items.Add(packet.ReadInt32());
            }
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Inventory)base.DeepCopy(into);

            if (copy == into)
            {
                copy._items.Clear();
                copy._items.AddRange(_items);
            }
            else
            {
                copy._items = new List<int>(_items);
            }

            return copy;
        }

        #endregion
    }
}
