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
        private const int Version = 7;

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
        public void Capture(int avatar, IManager manager)
        {
            if (avatar < 1)
            {
                throw new ArgumentException(@"Invalid avatar specified.", "avatar");
            }
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
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

            // Store the equipment tree.
            foreach (var slot in equipment.AllSlots)
            {
                _data.Write(slot.Item);
                if (slot.Item > 0)
                {
                    _data.Write(slot.Entity);
                    manager.PacketizeEntity(slot.Item, _data);
                }
            }

            // Track items and their slots.
            var itemSlots = new List<int>();
            var items = new List<int>();

            // And finally, the inventory. Same as with the inventory, we have
            // to serialize the actual items in it.
            for (var i = 0; i < inventory.Capacity; i++)
            {
                var item = inventory[i];
                if (item > 0)
                {
                    itemSlots.Add(i);
                    items.Add(item);
                }
            }

            // Write the number of items in the inventory and actual items.
            _data.Write(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                _data.Write(itemSlots[i]);
                manager.PacketizeEntity(items[i], _data);
            }
        }

        /// <summary>
        /// Restores a character snapshot stored in this profile.
        /// </summary>
        /// <param name="playerNumber">The number of the player in the game
        /// he is restored to.</param>
        /// <param name="manager">The entity manager to add the restored
        /// entities to.</param>
        /// <returns>
        /// The restored avatar.
        /// </returns>
        public int Restore(int playerNumber, IManager manager)
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
                        return Restore0(playerNumber, manager);

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
            return Restore(playerNumber, manager);
        }

        #region Restore implementations

        /// <summary>
        /// Load saves of version 0.
        /// </summary>
        private int Restore0(int playerNumber, IManager manager)
        {
            var avatar = 0;
            var items = new List<int>();
            try
            {
                // Read the player's class and create the ship.
                var playerClass = (PlayerClassType)_data.ReadByte();
                PlayerClass = playerClass;

                // Read the respawn position.
                var position = _data.ReadFarPosition();

                // Create the ship.
                avatar = EntityFactory.CreatePlayerShip(manager, playerClass, playerNumber, position);

                // Get the elements we need to save.
                var attributes = (Attributes<AttributeType>)manager.GetComponent(avatar, Attributes<AttributeType>.TypeId);
                var experience = (Experience)manager.GetComponent(avatar, Experience.TypeId);
                var equipment = (ItemSlot)manager.GetComponent(avatar, ItemSlot.TypeId);
                var inventory = (Inventory)manager.GetComponent(avatar, Inventory.TypeId);

                // Clean out equipment.
                if (equipment.Item > 0)
                {
                    manager.RemoveEntity(equipment.Item);
                }

                // Clear inventory.
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
                // adjusting the actual attribute data, not the base data.
                attributes.DepacketizeLocal(_data);

                // Disable recomputation while fixing equipped item ids.
                attributes.Enabled = false;

                // Restore equipment.
                var itemIdMapping = new Dictionary<int, int>();
                var slotsRemaining = 1;
                while (slotsRemaining-- > 0)
                {
                    var oldItemId = _data.ReadInt32();
                    if (oldItemId <= 0)
                    {
                        continue;
                    }

                    // Got an item in this slot, restore it.
                    var parentItemId = _data.ReadInt32();
                    int newItemId;
                    try
                    {
                        newItemId = manager.DepacketizeEntity(_data);
                    }
                    catch (Exception)
                    {
                        // Failed loading, but don't abort because otherwise the whole system
                        // will blow up -- we're in a pretty unstable situation here, because
                        // the item slots point to non-existant entities!
                        newItemId = 0;
                    }

                    // If we have no mappings yet, this was the old equipment node.
                    if (itemIdMapping.Count == 0)
                    {
                        itemIdMapping.Add(parentItemId, equipment.Entity);
                        equipment.Item = newItemId;
                    }
                    else
                    {
                        // Inner node. Adjust slot in parent pointing to old id.
                        foreach (var component in manager.GetComponents(itemIdMapping[parentItemId], ItemSlot.TypeId))
                        {
                            var slot = (ItemSlot)component;
                            if (slot.Item == oldItemId)
                            {
                                // This is the one.
                                slot.SetItemUnchecked(newItemId);
                                break;
                            }
                        }
                    }

                    // Queue reads for all child slots, unless this item failed loading.
                    if (newItemId > 0)
                    {
                        slotsRemaining += manager.GetComponents(newItemId, ItemSlot.TypeId).Count();
                    }

                    // Add mapping for this entry.
                    itemIdMapping.Add(oldItemId, newItemId);
                }

                // Reenable attribute updating and trigger recomputation.
                attributes.Enabled = true;
                attributes.RecomputeAttributes();

                // Restore inventory, read back the stored items.
                var numInventoryItems = _data.ReadInt32();
                for (var i = 0; i < numInventoryItems; i++)
                {
                    var slot = _data.ReadInt32();
                    var item = manager.DepacketizeEntity(_data);
                    items.Add(item);
                    inventory.Insert(slot, item);
                }

                return avatar;
            }
            catch (Exception)
            {
                // Clean up what we created.
                if (avatar > 0)
                {
                    manager.RemoveEntity(avatar);
                }
                foreach (var item in items)
                {
                    manager.RemoveEntity(item);
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

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public Packet Packetize(Packet packet)
        {
            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// before automatic depacketization is performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void PreDepacketize(Packet packet)
        {
        }

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// after automatic depacketization has been performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void PostDepacketize(Packet packet)
        {
        }

        #endregion
    }
}
