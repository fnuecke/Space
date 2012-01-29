using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.Session
{
    sealed class Profile : IPacketizable
    {
        #region Logger
        
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties

        /// <summary>
        /// The profile name. This is equal to the profile's file name.
        /// </summary>
        public string Name { get; set; }

        // TODO: additional info we might want do display in character selection, such as level, gold, ...

        #endregion

        #region Fields

        /// <summary>
        /// The serialized character data.
        /// </summary>
        private Packet _data = new Packet();

        #endregion

        /// <summary>
        /// Take a snapshot of a character's current state in a running game.
        /// </summary>
        /// <param name="avatar">The avatar to take a snapshot of.</param>
        public void Capture(Entity avatar)
        {
            if (avatar.UID <= 0)
            {
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }

            // Get the elements we need to save.
            var character = avatar.GetComponent<Character<AttributeType>>();
            var equipment = avatar.GetComponent<Equipment>();
            var inventory = avatar.GetComponent<Inventory>();
            var manager = avatar.Manager;

            if (character == null || equipment == null || inventory == null || manager == null)
            {
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }

            // Make the actual snapshot via serialization.
            _data.Reset();

            // Store the character's base values, just use serialization. This
            // is a slightly adjusted serialization method which does not touch
            // the base class (as we don't need that).
            character.PacketizeLocal(_data);

            // Store the equipment. We need to extract the actual items, not
            // just the IDs, so loop through the equipped items.

            var itemTypes = new[] {
                typeof(Armor),
                typeof(Reactor),
                typeof(Sensor),
                typeof(Shield),
                typeof(Thruster),
                typeof(Weapon)
            };

            _data.Write(itemTypes.Length);
            foreach (var itemType in itemTypes)
            {
                // Number of slots for that item type.
                int slotCount = equipment.GetSlotCount(itemType);

                // Get the list of equipped items of that type.
                var itemSlots = new List<int>();
                var itemsOfType = new List<Entity>();
                for (int i = 0; i < slotCount; i++)
                {
                    var item = equipment.GetItem(itemType, i);
                    if (item != null)
                    {
                        itemSlots.Add(i);
                        itemsOfType.Add(item);
                    }
                }

                // Write the type, count and actual items.
                _data.Write(itemType.AssemblyQualifiedName);
                _data.Write(slotCount);
                _data.Write(itemsOfType.Count);
                for (int i = 0; i < itemsOfType.Count; i++)
                {
                    _data.Write(itemSlots[i]);
                    _data.Write(itemsOfType[i]);
                }
            }

            // And finally, the inventory. Same as with the inventory, we have
            // to serialize the actual items in it.
            _data.Write(inventory.Count);
            foreach (var item in inventory)
            {
                _data.Write(item);
            }

            // TODO: extract additional display info, if desired.
        }

        /// <summary>
        /// Restores a character snapshot stored in this profile into the
        /// specified avatar.
        /// </summary>
        /// <param name="avatar">The avatar to restore the snapshot into.</param>
        public void Restore(Entity avatar)
        {
            if (avatar.UID <= 0)
            {
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }

            // Get the elements we need to save.
            var character = avatar.GetComponent<Character<AttributeType>>();
            var equipment = avatar.GetComponent<Equipment>();
            var inventory = avatar.GetComponent<Inventory>();
            var manager = avatar.Manager;

            if (character == null || equipment == null || inventory == null || manager == null)
            {
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }

            _data.Reset();

            // Restore character.
            character.DepacketizeLocal(_data);

            // Restore equipment.
            int numItemTypes = _data.ReadInt32();
            for (int i = 0; i < numItemTypes; i++)
            {
                var itemType = Type.GetType(_data.ReadString());
                var slotCount = _data.ReadInt32();

                // Reset equipment, remove entities that were previously
                // equipped from the game (can't think of an occasion where
                // this would happen, now, because this should only be done
                // on game start, but just be on the safe side).
                for (int j = 0; j < equipment.GetSlotCount(itemType); j++)
                {
                    var item = equipment.Unequip(itemType, j);
                    if (item != null)
                    {
                        manager.RemoveEntity(item);
                    }
                }

                // Set restored slot count.
                equipment.SetSlotCount(itemType, slotCount);

                // Read items and equip them.
                int numItemsOfType = _data.ReadInt32();
                for (int j = 0; j < numItemTypes; j++)
                {
                    int slot = _data.ReadInt32();
                    var item = _data.ReadPacketizable<Entity>();
                    // Reset uid, add to our entity manager.
                    item.UID = -1;
                    manager.AddEntity(item);
                    equipment.Equip(item, slot);
                }
            }

            // Restore inventory, clear it first. As with the equipment, remove
            // any old items, if there were any.
            while (inventory.Count > 0)
            {
                var item = inventory[inventory.Count - 1];
                inventory.RemoveAt(inventory.Count - 1);
                manager.RemoveEntity(item);
            }

            // Then read back the stored items.
            int numInventoryItems = _data.ReadInt32();
            for (int i = 0; i < numInventoryItems; i++)
            {
                var item = _data.ReadPacketizable<Entity>();
                // Reset uid, add to our entity manager.
                item.UID = -1;
                manager.AddEntity(item);
                inventory.Add(item);
            }
        }

        /// <summary>
        /// Stores the profile to disk, under the specified profile name.
        /// </summary>
        public void Save()
        {

        }

        public Packet Packetize(Packet packet)
        {
            throw new NotImplementedException();
        }

        public void Depacketize(Packet packet)
        {
            throw new NotImplementedException();
        }
    }
}
