using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Engine.ComponentSystem;
using Engine.ComponentSystem.RPG.Components;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.Session
{
    /// <summary>
    /// Implements profile save and restore functionality.
    /// </summary>
    internal sealed class Profile : IProfile
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// File version of the saved profiles. In case we change something
        /// fundamentally so that we can handle files differently. This is
        /// the version we write to new snapshots.
        /// </summary>
        private const int Version = 8;

        /// <summary>
        /// Header for our save game files.
        /// </summary>
        private static readonly byte[] Header = Encoding.ASCII.GetBytes("MPSAV");

        /// <summary>
        /// Pattern used to eliminate invalid chars from profile names, for saving.
        /// </summary>
        private static readonly Regex InvalidCharPattern = new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))), RegexOptions.Compiled);

        /// <summary>
        /// Cryptography service we use to encrypt our save files.
        /// </summary>
        private static readonly SimpleCrypto Crypto = new SimpleCrypto(
            new byte[] { 174, 190, 179, 189, 31, 66, 187, 235, 115, 253, 233, 119, 144, 33, 238, 191, 210, 244, 101, 247, 193, 75, 136, 202, 188, 1, 124, 237, 118, 223, 99, 140 },
            new byte[] { 123, 239, 208, 52, 86, 203, 255, 232, 156, 225, 31, 219, 2, 65, 143, 155 });

        #endregion

        #region Properties

        /// <summary>
        /// A list of all existing profiles.
        /// </summary>
        public IEnumerable<string> Profiles
        {
            get
            {
                var profileFolder = InvalidCharPattern.Replace(Settings.Instance.ProfileFolder, "_");
                if (Directory.Exists(profileFolder))
                {
                    foreach (var fileName in Directory.EnumerateFiles(profileFolder, "*.sav"))
                    {
                        yield return Path.GetFileNameWithoutExtension(fileName);
                    }
                }
            }
        }

        /// <summary>
        /// The profile name. This is equal to the profile's file name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The player class of this profile.
        /// </summary>
        public PlayerClassType PlayerClass { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The serialized character data.
        /// </summary>
        [PacketizerCreate]
        private Packet _data;

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_data != null)
            {
                _data.Dispose();
            }
        }

        #endregion

        #region Create / Load / Save

        /// <summary>
        /// Creates a new profile with the specified name and the specified
        /// player class.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <param name="playerClass">The player class.</param>
        /// <exception cref="ArgumentException">profile name is invalid.</exception>
        public void Create(string name, PlayerClassType playerClass)
        {
            if (InvalidCharPattern.IsMatch(name))
            {
                throw new ArgumentException(@"Invalid profile name, contains invalid character.", "name");
            }

            // Save name and class, initialization will be done when restore
            // is called for the first time.
            Reset();
            this.Name = name;
            PlayerClass = playerClass;

            if (File.Exists(GetFullProfilePath()))
            {
                Logger.Warn("Profile with that name already exists.");
            }
        }

        /// <summary>
        /// Loads this profile from disk. If loading fails this will default to
        /// a new profile with the fall-back character class.
        /// </summary>
        /// <exception cref="ArgumentException">Profile name is invalid.</exception>
        public void Load(string name)
        {
            if (InvalidCharPattern.IsMatch(name))
            {
                throw new ArgumentException(@"Invalid profile name, contains invalid character.", "name");
            }

            // Figure out the path, check if it's valid.
            Reset();
            Name = name;
            var profilePath = GetFullProfilePath();

            if (!File.Exists(profilePath))
            {
                throw new ArgumentException(@"Invalid profile name, no such file.", "name");
            }

            try
            {
                // Load the file contents, which are encrypted and compressed.
                var encrypted = File.ReadAllBytes(profilePath);
                var compressed = Crypto.Decrypt(encrypted);
                var plain = SimpleCompression.Decompress(compressed);

                // Now we have the plain data, handle it as a packet to read our
                // data from it.
                using (var packet = new Packet(plain))
                {
                    // Get file header.
                    if (!CheckHeader(packet.ReadByteArray()))
                    {
                        Logger.Error("Failed loading profile, invalid header, using default.");
                        return;
                    }

                    // Get the hash the data had when writing.
                    var hash = packet.ReadInt32();

                    // Get the player class.
                    var playerClass = (PlayerClassType)packet.ReadByte();

                    // And the actual data.
                    var data = packet.ReadByteArray();

                    // Check if the hash matches.
                    var hasher = new Hasher();
                    hasher.Put((byte)playerClass);
                    if (data != null)
                    {
                        hasher.Put(data);
                    }
                    if (hasher.Value == hash)
                    {
                        // All is well, keep the data, drop our old data, if any.
                        PlayerClass = playerClass;
                        _data = new Packet(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed loading profile, using default.", ex);
            }
        }

        /// <summary>
        /// Stores the profile to disk, under the specified profile name.
        /// </summary>
        public void Save()
        {
            // Get our plain data and hash it.
            var plain = (_data != null) ? _data.GetBuffer() : null;
            var hasher = new Hasher();
            hasher.Put((byte)PlayerClass);
            if (plain != null)
            {
                hasher.Put(plain);
            }

            // Write it to a packet, compress it, encrypt it and save it.
            using (var packet = new Packet())
            {
                // Put our hash and plain data.
                packet.Write(Header);
                packet.Write(hasher.Value);
                packet.Write((byte)PlayerClass);
                packet.Write(plain);

                // Compress and encrypt, then save.
                var compressed = SimpleCompression.Compress(packet.GetBuffer());
                var encrypted = Crypto.Encrypt(compressed);

                var profilePath = GetFullProfilePath();
                try
                {
                    var path = Path.GetDirectoryName(profilePath);
                    if (path != null)
                    {
                        Directory.CreateDirectory(path);
                    }

                    // Make a backup if there's a save already.
                    if (File.Exists(profilePath))
                    {
                        var backupPath = profilePath + ".bak";
                        try
                        {
                            if (File.Exists(backupPath))
                            {
                                File.Delete(backupPath);
                            }
                            File.Move(profilePath, backupPath);
                        }
                        catch (Exception ex)
                        {
                            Logger.WarnException("Failed backing-up saved profile.", ex);
                        }
                    }

                    try
                    {
                        File.WriteAllBytes(profilePath, encrypted);
#if DEBUG
                        if (plain != null)
                        {
                            File.WriteAllBytes(profilePath + ".raw", plain);
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Failed saving profile.", ex);
                        // If we have a backup, try to restore it.
                        var backupPath = profilePath + ".bak";
                        if (File.Exists(backupPath))
                        {
                            try
                            {
                                if (File.Exists(profilePath))
                                {
                                    File.Delete(profilePath);
                                }
                                File.Copy(backupPath, profilePath);
                            }
                            catch (Exception ex2)
                            {
                                Logger.WarnException("Failed restoring backed-up profile.", ex2);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Failed saving.", ex);
                }
            }
        }

        #endregion

        #region Snapshots

        /// <summary>
        /// Take a snapshot of a character's current state in a running game.
        /// </summary>
        /// <param name="manager">The component system manager.</param>
        /// <param name="avatar">The avatar to take a snapshot of.</param>
        public void Capture(IManager manager, int avatar)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (avatar < 1)
            {
                throw new ArgumentException(@"Invalid avatar specified.", "avatar");
            }

            // Get the elements we need to save.
            var playerClass = (PlayerClass)manager.GetComponent(avatar, ComponentSystem.Components.PlayerClass.TypeId);
            var respawn = (Respawn)manager.GetComponent(avatar, Respawn.TypeId);
            var experience = (Experience)manager.GetComponent(avatar, Experience.TypeId);
            var attributes = (Attributes<AttributeType>)manager.GetComponent(avatar, Attributes<AttributeType>.TypeId);
            var equipment = (ItemSlot)manager.GetComponent(avatar, ItemSlot.TypeId);
            var inventory = (Inventory)manager.GetComponent(avatar, Inventory.TypeId);

            // Check if we have everything we need.
            if (playerClass == null ||
                respawn == null ||
                attributes == null ||
                equipment == null ||
                inventory == null)
            {
                throw new ArgumentException(@"Invalid avatar specified.", "avatar");
            }

            // Make the actual snapshot via serialization.
            if (_data != null)
            {
                _data.Dispose();
            }
            _data = new Packet();

            // Write file version.
            _data.Write(Version);

            // Store the player class. Needed to create the actual ship when
            // loading. Also update the profile accordingly (should never
            // change, but who knows...)
            PlayerClass = playerClass.Value;
            _data.Write((byte)playerClass.Value);

            // Save the current spawning position.
            _data.Write(respawn.Position);

            // Store experience.
            _data.Write(experience.Value);

            // Store the attribute base values, just use serialization. This
            // is a slightly adjusted serialization method which does not touch
            // the base class (as we don't need that).
            attributes.PacketizeLocal(_data);

            // Store the equipment tree. We do this by writing all actually equipped
            // items and their current id, and the id of the item that is now their
            // parent. This allows connecting them back after deserializing them into
            // new entities with different ids.
            _data.Write(equipment.AllItems.Count());
            foreach (var slot in equipment.AllSlots.Where(s => s.Item > 0))
            {
                // Write old id of containing slot and id of stored item.
                _data.Write(slot.Id);
                manager.PacketizeEntity(slot.Item, _data);
            }

            // And finally, the inventory. Same as with the equipment, we have
            // to serialize the actual items in it. This is not as complicated
            // as the equipment, though, as it's not a tree, just a plain list.
            _data.Write(inventory.Count);
            _data.Write(inventory.IsReadOnly);
            if (inventory.IsReadOnly)
            {
                // Fixed length inventory, write capacity and index for each item.
                _data.Write(inventory.Capacity);
                for (var slot = 0; slot < inventory.Capacity; ++slot)
                {
                    var item = inventory[slot];
                    if (item > 0)
                    {
                        _data.Write(slot);
                        manager.PacketizeEntity(item, _data);
                    }
                }
            }
            else
            {
                // List inventory, just dump all the items.
                foreach (var item in inventory)
                {
                    manager.PacketizeEntity(item, _data);
                }
            }
        }

        /// <summary>
        /// Restores a character snapshot stored in this profile.
        /// </summary>
        /// <param name="manager">The entity manager to add the restored
        /// entities to.</param>
        /// <param name="playerNumber">The number of the player in the game
        /// he is restored to.</param>
        /// <returns>
        /// The restored avatar.
        /// </returns>
        public int Restore(IManager manager, int playerNumber)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (_data == null)
            {
                // No data yet, meaning the profile isn't initialized, yet.
                return EntityFactory.CreatePlayerShip(manager, PlayerClass, playerNumber, new FarPosition(50000, 50000));
            }

            // OK, start from scratch.
            _data.Reset();

            // Check version and use according loader, where possible.
            try
            {
                switch (_data.ReadInt32())
                {
                    case Version:
                        return Restore0(manager, playerNumber);

                    default:
                        Logger.Error("Unknown profile version, using default.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed restoring profile, using default.", ex);
            }

            // Restore a default profile and return it.
            Reset();
            return Restore(manager, playerNumber);
        }

        #region Restore implementations

        /// <summary>
        /// Load saves of version 0.
        /// </summary>
        private int Restore0(IManager manager, int playerNumber)
        {
            var undo = new Stack<Action>();
            try
            {
                // Read the player's class and create the ship.
                var playerClass = (PlayerClassType)_data.ReadByte();
                PlayerClass = playerClass;

                // Read the respawn position.
                var position = _data.ReadFarPosition();

                // Create the ship.
                var avatar = EntityFactory.CreatePlayerShip(manager, playerClass, playerNumber, position);
                undo.Push(() => manager.RemoveEntity(avatar));

                // Get the elements we need to save.
                var attributes = (Attributes<AttributeType>)manager.GetComponent(avatar, Attributes<AttributeType>.TypeId);
                var experience = (Experience)manager.GetComponent(avatar, Experience.TypeId);
                var equipment = (ItemSlot)manager.GetComponent(avatar, ItemSlot.TypeId);
                var inventory = (Inventory)manager.GetComponent(avatar, Inventory.TypeId);

                // Clean out default equipment.
                if (equipment.Item > 0)
                {
                    // This will recursively delete all items in child slots.
                    manager.RemoveEntity(equipment.Item);
                }

                // Clear default inventory. Iterate backwards to be compatible with
                // both fixed and flexible length inventories.
                for (var i = inventory.Capacity - 1; i >= 0; --i)
                {
                    var item = inventory[i];
                    if (item > 0)
                    {
                        manager.RemoveEntity(item);
                    }
                }

                // Read back experience. Disable while setting to suppress message.
                experience.Enabled = false;
                experience.Value = _data.ReadInt32();
                experience.Enabled = true;

                // Restore attributes. Use special packetizer implementation only
                // adjusting the actual attribute data, not the base data such as
                // entity id (which we want to keep).
                attributes.DepacketizeLocal(_data);

                // Disable recomputation while fixing equipped item ids.
                attributes.Enabled = false;

                // Restore equipment. This whole part is a bit messy. We first have to
                // read back all stored items, then link them back together the way they
                // were before. To do this without breaking the simulation in case
                // something goes wrong, we store those links in an extra dictionary and
                // set all references to zero until we have all items. This dictionary
                // maps the new item id to the old slot id the item was in. We get the
                // translation of the old component ids to the new ones from the
                // depacketizing operation and accumulate all changes in an extra map.
                var itemIdMapping = new Dictionary<int, int>();
                // This is used to get the change in component ids from reading the item
                // entities back after serialization.
                var componentIdMap = new Dictionary<int, int>();
                // See how many items we can expect.
                var equipmentCount = _data.ReadInt32();
                for (var i = 0; i < equipmentCount; ++i)
                {
                    // Read old ids and item entity.
                    var oldSlotId = _data.ReadInt32();
                    var newItemId = manager.DepacketizeEntity(_data, componentIdMap);
                    itemIdMapping.Add(newItemId, oldSlotId);

                    // Null out any slot references to avoid trying to remove non-existent
                    // stuff on undo (when something else fails).
                    foreach (ItemSlot slot in manager.GetComponents(newItemId, ItemSlot.TypeId))
                    {
                        slot.SetItemUnchecked(0);
                    }

                    // Push undo command.
                    undo.Push(() => manager.RemoveEntity(newItemId));
                }

                // No rebuild the equipment tree by restoring the links.
                foreach (var entry in itemIdMapping)
                {
                    // Get the new slot id by looking up the new component id based on the old one.
                    // There's one special case: the slot is not in our component map. This means
                    // the slot was not on one of the equipped items, and therefore has to be
                    // our equipment.
                    var newItemId = entry.Key;
                    var oldSlotId = entry.Value;
                    if (componentIdMap.ContainsKey(oldSlotId))
                    {
                        // Known component, so it has to be a slot on an item.
                        var newSlot = (ItemSlot)manager.GetComponentById(componentIdMap[oldSlotId]);

                        // Set the slot's content to the item's new id.
                        newSlot.SetItemUnchecked(newItemId);

                        // Undo linking for entity removal above (to avoid duplicate removals).
                        undo.Push(() => newSlot.SetItemUnchecked(0));
                    }
                    else
                    {
                        // Unknown, so it has to be our equipment.
                        System.Diagnostics.Debug.Assert(equipment.Item == 0, "Got multiple equipment tree roots.");
                        equipment.SetItemUnchecked(newItemId);

                        // Undo linking for entity removal above (to avoid duplicate removals).
                        undo.Push(() => equipment.SetItemUnchecked(0));
                    }
                }

                // Reenable attribute updating and trigger recomputation.
                attributes.Enabled = true;
                attributes.RecomputeAttributes();

                // Restore inventory, read back the stored items.
                var inventoryCount = _data.ReadInt32();
                if (_data.ReadBoolean())
                {
                    // Fixed size inventory.
                    inventory.Capacity = _data.ReadInt32();
                    for (var i = 0; i < inventoryCount; i++)
                    {
                        var slot = _data.ReadInt32();
                        var item = manager.DepacketizeEntity(_data);
                        inventory.Insert(slot, item);
                    }
                }
                else
                {
                    // List inventory.
                    for (var i = 0; i < inventoryCount; ++i)
                    {
                        var item = manager.DepacketizeEntity(_data);
                        inventory.Add(item);
                    }
                }

                return avatar;
            }
            catch (Exception)
            {
                // Clean up what we created.
                while (undo.Count > 0)
                {
                    // Looks funny, don't it? ;)
                    undo.Pop()();
                }
                throw;
            }
        }

        #endregion

        #endregion

        #region Utility methods

        /// <summary>
        /// Get a cleaned up, full path to the file to save this profile in.
        /// </summary>
        /// <returns>The path to this profile.</returns>
        private string GetFullProfilePath()
        {
            return Path.Combine(InvalidCharPattern.Replace(Settings.Instance.ProfileFolder, "_"), Name + ".sav");
        }

        /// <summary>
        /// Check if the header is correct.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <returns></returns>
        private bool CheckHeader(byte[] header)
        {
            if (header == null || header.Length != Header.Length)
            {
                return false;
            }
            for (int i = 0; i < Header.Length; i++)
            {
                if (header[i] != Header[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the profile to an uninitialized state with the default
        /// player class.
        /// </summary>
        private void Reset()
        {
            PlayerClass = PlayerClassType.Default;
            if (_data != null)
            {
                _data.Dispose();
                _data = null;
            }
        }

        #endregion
    }
}
