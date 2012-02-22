using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Engine.ComponentSystem;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Util;
using Space.Data;
using Space.Util;

namespace Space.Session
{
    /// <summary>
    /// Implements profile save and restore functionality.
    /// </summary>
    sealed class Profile : IProfile
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
        private const int Version = 0;

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
                throw new ArgumentException("Invalid profile name, contains invalid character.", "name");
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
                throw new ArgumentException("Invalid profile name, contains invalid character.", "name");
            }

            // Figure out the path, check if it's valid.
            Reset();
            this.Name = name;
            var profilePath = GetFullProfilePath();

            if (!File.Exists(profilePath))
            {
                throw new ArgumentException("Invalid profile name, no such file.", "name");
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
                    Directory.CreateDirectory(Path.GetDirectoryName(profilePath));

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
                        File.WriteAllBytes(profilePath + ".raw", plain);
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
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            // Get the elements we need to save.
            var playerClass = manager.GetComponent<PlayerClass>(avatar);
            var respawn = manager.GetComponent<Respawn>(avatar);
            var character = manager.GetComponent<Character<AttributeType>>(avatar);
            var equipment = manager.GetComponent<Equipment>(avatar);
            var inventory = manager.GetComponent<Inventory>(avatar);

            // Check if we have everything we need.
            if (playerClass == null ||
                respawn == null ||
                character == null ||
                equipment == null ||
                inventory == null)
            {
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }

            // Make the actual snapshot via serialization.
            _data.Dispose();
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

            // Track items and their slots.
            var itemSlots = new List<int>();
            var items = new List<int>();

            _data.Write(itemTypes.Length);
            foreach (var itemType in itemTypes)
            {
                // Number of slots for that item type.
                int slotCount = equipment.GetSlotCount(itemType);

                // Get the list of equipped items of that type.
                for (int i = 0; i < slotCount; i++)
                {
                    var item = equipment.GetItem(itemType, i);
                    if (item.HasValue)
                    {
                        itemSlots.Add(i);
                        items.Add(item.Value);
                    }
                }

                // Write the type, count and actual items.
                _data.Write(itemType.AssemblyQualifiedName);
                _data.Write(slotCount);
                _data.Write(items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    _data.Write(itemSlots[i]);
                    manager.PacketizeEntity(items[i], _data);
                }

                itemSlots.Clear();
                items.Clear();
            }

            // And finally, the inventory. Same as with the inventory, we have
            // to serialize the actual items in it.
            _data.Write(inventory.Capacity);
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var item = inventory[i];
                if (item.HasValue)
                {
                    itemSlots.Add(i);
                    items.Add(item.Value);
                }
            }

            // Write the number of items in the inventory and actual items.
            _data.Write(items.Count);
            for (int i = 0; i < items.Count; i++)
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
                return Initialize(playerNumber, manager);
            }

            // OK, start from scratch.
            _data.Reset();

            // Check version and use according loader, where possible.
            try
            {
                switch (_data.ReadInt32())
                {
                    case 0:
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
            // Read the player's class and create the ship.
            var playerClass = (PlayerClassType)_data.ReadByte();
            PlayerClass = playerClass;

            // Read the respawn position.
            var position = _data.ReadVector2();

            // Create the ship.
            var avatar = EntityFactory.CreatePlayerShip(manager, playerClass, playerNumber, position);

            // Get the elements we need to save.
            var character = manager.GetComponent<Character<AttributeType>>(avatar);
            var equipment = manager.GetComponent<Equipment>(avatar);
            var inventory = manager.GetComponent<Inventory>(avatar);

            // Restore character. Use special packetizer implementation only
            // adjusting the actual character data, not the base data.
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
                    if (item.HasValue)
                    {
                        manager.RemoveEntity(item.Value);
                    }
                }

                // Set restored slot count.
                equipment.SetSlotCount(itemType, slotCount);

                // Read items and equip them.
                int numItemsOfType = _data.ReadInt32();
                for (int j = 0; j < numItemsOfType; j++)
                {
                    int slot = _data.ReadInt32();
                    var item = manager.DepacketizeEntity(_data);
                    equipment.Equip(slot, item);
                }
            }

            // Restore inventory, clear it first. As with the equipment, remove
            // any old items, if there were any.
            for (int i = inventory.Capacity - 1; i >= 0; --i)
            {
                var item = inventory[i];
                if (item.HasValue)
                {
                    manager.RemoveEntity(item.Value);
                }
            }

            // Then read back the stored items.
            int numInventoryItems = _data.ReadInt32();
            for (int i = 0; i < numInventoryItems; i++)
            {
                var slot = _data.ReadInt32();
                var item = manager.DepacketizeEntity(_data);
                inventory.Insert(slot, item);
            }

            return avatar;
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
        /// Initializes this profile to a new ship.
        /// </summary>
        /// <param name="playerNumber">The player number.</param>
        /// <param name="manager">The manager.</param>
        private int Initialize(int playerNumber, IManager manager)
        {
            // Store the character's base values. This is a little roundabout,
            // but this way it'll always be up-to-date.
            var ship = EntityFactory.CreatePlayerShip(manager, PlayerClass, playerNumber, new Vector2(50000, 50000));
            var equipment = manager.GetComponent<Equipment>(ship);

            // Basic starter outfit for that class.
            InitializeEquipment<Armor>(equipment, manager);
            InitializeEquipment<Reactor>(equipment, manager);
            InitializeEquipment<Sensor>(equipment, manager);
            InitializeEquipment<Shield>(equipment, manager);
            InitializeEquipment<Thruster>(equipment, manager);
            InitializeEquipment<Weapon>(equipment, manager);

            return ship;
        }

        /// <summary>
        /// Utility method for initializing an equipment type.
        /// </summary>
        /// <typeparam name="T">The item type to initialize.</typeparam>
        /// <param name="equipment">The equipment.</param>
        /// <param name="manager">The manager.</param>
        private void InitializeEquipment<T>(Equipment equipment, IManager manager) where T : Item
        {
            // Check if we can equip this item.
            if (equipment.GetSlotCount<T>() < 1)
            {
                return;
            }
            // Get the item name.
            var itemName = PlayerClass.GetStarterItemFactoryName<T>();
            if (itemName != null)
            {
                // Got one, create and equip it in slot one.
                equipment.Equip(0, FactoryLibrary.SampleItem(manager, itemName, null));
            }
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
            return packet
                .Write(Name)
                .Write((byte)PlayerClass)
                .Write(_data);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
            PlayerClass = (PlayerClassType)packet.ReadByte();
            if (_data != null)
            {
                _data.Dispose();
            }
            _data = packet.ReadPacket();
        }

        #endregion
    }
}
