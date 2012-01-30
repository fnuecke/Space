using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Entities;
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
        
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants
        
        /// <summary>
        /// File version of the saved profiles. In case we change something
        /// fundamentally so that we can handle files differently. This is
        /// the version we write to new snapshots.
        /// </summary>
        private const int _version = 0;

        /// <summary>
        /// Pattern used to eliminate invalid chars from profile names, for saving.
        /// </summary>
        private static readonly Regex _invalidCharPattern = new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))), RegexOptions.Compiled);

        /// <summary>
        /// Cryptography service we use to encrypt our save files.
        /// </summary>
        private static readonly SimpleCrypto _crypto = new SimpleCrypto(
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
                var profileFolder = _invalidCharPattern.Replace(Settings.Instance.ProfileFolder, "_");
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

        // TODO: additional info we might want do display in character selection, such as level, gold, ...

        #endregion

        #region Fields

        /// <summary>
        /// The serialized character data.
        /// </summary>
        private Packet _data = new Packet();

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _data.Dispose();

            GC.SuppressFinalize(this);
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
            if (_invalidCharPattern.IsMatch(name))
            {
                throw new ArgumentException("Invalid profile name, contains invalid character.", "name");
            }

            this.Name = name;
            var profilePath = GetFullProfilePath();
            if (File.Exists(profilePath))
            {
                logger.Warn("Profile with that name already exists.");
            }

            // Create new profile.
            Initialize(playerClass);
        }

        /// <summary>
        /// Loads this profile from disk. If loading fails this will default to
        /// a new profile with the fall-back character class.
        /// </summary>
        /// <exception cref="ArgumentException">profile name is invalid.</exception>
        public void Load(string name)
        {
            if (_invalidCharPattern.IsMatch(name))
            {
                throw new ArgumentException("Invalid profile name, contains invalid character.", "name");
            }

            this.Name = name;
            var profilePath = GetFullProfilePath();
            try
            {
                // Load the file contents, which are encrypted and compressed.
                var encrypted = File.ReadAllBytes(profilePath);
                var compressed = _crypto.Decrypt(encrypted);
                var plain = SimpleCompression.Decompress(compressed);

                // Now we have the plain data, handle it as a packet to read our
                // data from it.
                using (var packet = new Packet(plain))
                {
                    // Get the hash the data had when writing.
                    var hash = packet.ReadInt32();
                    // And the actual data.
                    var data = packet.ReadByteArray();

                    // Check if the hash matches.
                    var hasher = new Hasher();
                    hasher.Put(data);
                    if (hasher.Value != hash)
                    {
                        // Broken or modified data, don't use it.
                        Initialize(PlayerClassType.Default);
                    }
                    else
                    {
                        // All is well, keep the data, drop our old data, if any.
                        _data.Dispose();
                        _data = new Packet(data);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Failed loading profile, using default.", ex);
                Initialize(PlayerClassType.Default);
            }
        }

        /// <summary>
        /// Stores the profile to disk, under the specified profile name.
        /// </summary>
        public void Save()
        {
            // Get our plain data and hash it.
            var plain = _data.GetBuffer();
            var hasher = new Hasher();
            hasher.Put(plain);

            // Write it to a packet, compress it, encrypt it and save it.
            using (var packet = new Packet())
            {
                // Put our hash and plain data.
                packet.Write(hasher.Value);
                packet.Write(plain);

                // Compress and encrypt, then save.
                var compressed = SimpleCompression.Compress(packet.GetBuffer());
                var encrypted = _crypto.Encrypt(compressed);

                var profilePath = GetFullProfilePath();
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
                        logger.WarnException("Failed backing-up saved profile.", ex);
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
                    logger.ErrorException("Failed saving profile.", ex);
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
                            logger.WarnException("Failed restoring backed-up profile.", ex2);
                        }
                    }
                }
            }
        }

        #endregion

        #region Snapshots

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
            var playerClass = avatar.GetComponent<PlayerClass>();
            var respawn = avatar.GetComponent<Respawn>();
            var character = avatar.GetComponent<Character<AttributeType>>();
            var equipment = avatar.GetComponent<Equipment>();
            var inventory = avatar.GetComponent<Inventory>();
            var manager = avatar.Manager;

            // Check if we have everything we need.
            if (playerClass == null ||
                respawn == null ||
                character == null ||
                equipment == null ||
                inventory == null ||
                manager == null)
            {
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }

            // Make the actual snapshot via serialization.
            _data.Dispose();
            _data = new Packet();

            // Write file version.
            _data.Write(_version);

            // Store the player class. Needed to create the actual ship when
            // loading.
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
        /// Restores a character snapshot stored in this profile.
        /// </summary>
        /// <param name="playerNumber">The number of the player in the game
        /// he is restored to.</param>
        /// <param name="manager">The entity manager to add the restored
        /// entities to.</param>
        /// <returns>The restored avatar.</returns>
        public void Restore(int playerNumber, IEntityManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("EntityManager must not be null.", "manager");
            }

            // OK, start from scratch.
            _data.Reset();

            Entity avatar;

            // Check version and use according loader, where possible.
            try
            {
                switch (_data.ReadInt32())
                {
                    case 0:
                        avatar = Restore0(playerNumber, manager);
                        break;

                    default:
                        throw new InvalidOperationException("Unknown profile version.");
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Failed restoring profile, using default.", ex);
                Initialize(PlayerClassType.Fighter);
                avatar = RestoreCurrent(playerNumber, manager);
            }

            // Add the ship to the simulation.
            manager.AddEntity(avatar);
        }

        #region Restore implementations

        /// <summary>
        /// Always points to current restore implementation.
        /// </summary>
        private Entity RestoreCurrent(int playerNumber, IEntityManager manager)
        {
            return Restore0(playerNumber, manager);
        }

        /// <summary>
        /// Load saves of version 0.
        /// </summary>
        private Entity Restore0(int playerNumber, IEntityManager manager)
        {
            // Read the player's class and create the ship.
            var playerClass = (PlayerClassType)_data.ReadByte();

            // Read the respawn position.
            var position = _data.ReadVector2();

            // Create the ship.
            var avatar = EntityFactory.CreatePlayerShip(playerClass, playerNumber, position);

            // Get the elements we need to save.
            var character = avatar.GetComponent<Character<AttributeType>>();
            var equipment = avatar.GetComponent<Equipment>();
            var inventory = avatar.GetComponent<Inventory>();

            // Check if we have everything we need.
            if (character == null ||
                equipment == null ||
                inventory == null)
            {
                throw new ArgumentException("Invalid avatar specified.", "avatar");
            }

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
                    if (item != null)
                    {
                        manager.RemoveEntity(item);
                    }
                }

                // Set restored slot count.
                equipment.SetSlotCount(itemType, slotCount);

                // Read items and equip them.
                int numItemsOfType = _data.ReadInt32();
                for (int j = 0; j < numItemsOfType; j++)
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
                // Reset id, add to our entity manager.
                item.UID = -1;
                manager.AddEntity(item);
                inventory.Add(item);
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
            return Path.Combine(_invalidCharPattern.Replace(Settings.Instance.ProfileFolder, "_"), Name + ".sav");
        }

        /// <summary>
        /// Initializes this profile to a new ship.
        /// </summary>
        private void Initialize(PlayerClassType playerClass)
        {
            // Start from scratch.
            _data.Dispose();
            _data = new Packet();

            // Write file version.
            _data.Write(_version);

            // Store the player class. Needed to create the actual ship when
            // loading.
            _data.Write((byte)playerClass);

            // Save the current spawning position.
            _data.Write(new Vector2(50000, 50000));

            // Store the character's base values. This is a little roundabout,
            // but this way it'll always be up-to-date.
            var ship = EntityFactory.CreatePlayerShip(playerClass, 0, Vector2.Zero);
            var character = ship.GetComponent<Character<AttributeType>>();
            character.PacketizeLocal(_data);

            // Store the equipment.
            var blueprint = playerClass.GetShipConstraints();

            // Number of item types.
            _data.Write(6);

            // Basic starter outfit for that class.
            _data.Write(typeof(Armor).AssemblyQualifiedName);
            _data.Write(blueprint.ArmorSlots);
            InitializeEquipment<Armor>(playerClass);

            _data.Write(typeof(Reactor).AssemblyQualifiedName);
            _data.Write(blueprint.ReactorSlots);
            InitializeEquipment<Reactor>(playerClass);

            _data.Write(typeof(Sensor).AssemblyQualifiedName);
            _data.Write(blueprint.SensorSlots);
            InitializeEquipment<Sensor>(playerClass);

            _data.Write(typeof(Shield).AssemblyQualifiedName);
            _data.Write(blueprint.ShieldSlots);
            InitializeEquipment<Shield>(playerClass);

            _data.Write(typeof(Thruster).AssemblyQualifiedName);
            _data.Write(blueprint.ThrusterSlots);
            InitializeEquipment<Thruster>(playerClass);

            _data.Write(typeof(Weapon).AssemblyQualifiedName);
            _data.Write(blueprint.WeaponSlots);
            InitializeEquipment<Weapon>(playerClass);

            // And finally, the inventory.
            // TODO: empty for now, maybe some healing items or such when we have them.
            _data.Write(0);
        }

        /// <summary>
        /// Utility method for initializing an equipment type.
        /// </summary>
        /// <typeparam name="T">The item type to initialize.</typeparam>
        /// <param name="playerClass">The player class to initialize for.</param>
        private void InitializeEquipment<T>(PlayerClassType playerClass)
        {
            var item = playerClass.GetStarterItemConstraints<T>();
            if (item == null)
            {
                _data.Write(0); // Number of items.
            }
            else
            {
                _data.Write(1); // Number of items.
                _data.Write(0); // Slot number.
                _data.Write(item.Sample(null)); // Actual item.
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
                .Write(_data);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public void Depacketize(Packet packet)
        {
            Name = packet.ReadString();
            _data.Dispose();
            _data = packet.ReadPacket();
        }

        #endregion
    }
}
