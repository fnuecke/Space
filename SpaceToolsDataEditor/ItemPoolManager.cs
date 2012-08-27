using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    internal class ItemPoolManager
    {

        /// <summary>
        /// The flat list of all ItemPools, referenced via their name.
        /// </summary>
        private static readonly Dictionary<string, ItemPool> ItemPools = new Dictionary<string, ItemPool>();

        /// <summary>
        /// Mapping ItemPools back to the files they came from, for saving.
        /// </summary>
        private static readonly Dictionary<ItemPool, string> ItemPoolFilenames = new Dictionary<ItemPool, string>();

        public delegate void ItemPoolChangedDelegate(ItemPool itempool);

        public delegate void ItemPoolNameChangedDelegate(string oldName, string newName);

        public delegate void ItemPoolClearedDelegate();

        public static event ItemPoolChangedDelegate ItemPoolAdded;

        public static event ItemPoolChangedDelegate ItemPoolRemoved;


        public static event ItemPoolNameChangedDelegate ItemPoolNameChanged;

        public static event ItemPoolClearedDelegate ItemPoolCleared;
        /// <summary>
        /// Loads all factories found at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void Load(string path)
        {
            Clear();
            foreach (var file in Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories))
            {
                using (var reader = new XmlTextReader(file))
                {
                    try
                    {
                        var itempools = IntermediateSerializer.Deserialize<object>(reader, null) as ItemPool[];
                        if (itempools != null)
                        {
                            foreach (var itempool in itempools)
                            {
                                // Adjust paths.
                                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(itempool, new Attribute[] { new EditorAttribute(typeof(TextureAssetEditor), typeof(UITypeEditor)) }))
                                {
                                    var value = property.GetValue(itempool) as string;
                                    if (!string.IsNullOrWhiteSpace(value))
                                    {
                                        property.SetValue(itempool, value.Replace('\\', '/'));
                                    }
                                }

                                Add(itempool);
                                ItemPoolFilenames.Add(itempool, file);
                            }
                        }
                    }
                    catch (InvalidContentException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }


        /// <summary>
        /// Saves all known factories back to file.
        /// </summary>
        public static void Save()
        {
            // In case we need to create new files.
            string baseFolder;

            // Ask for destination folder if we didn't load before.
            if (ItemPools.Count > 0 && ItemPoolFilenames.Count == 0)
            {
                // TODO
                return;
            }
            else
            {
                baseFolder = ItemPoolFilenames.Values.First().Replace('\\', '/');
                baseFolder = baseFolder.Substring(0, baseFolder.LastIndexOf('/') + 1);
            }

            object output;
            foreach (var itemPool in ItemPools.Values)
            {
                if (!ItemPoolFilenames.ContainsKey(itemPool))
                {
                    // No such factories yet, create new file.
                    var fileName = baseFolder + itemPool.GetType().Name + ".xml";
                    ItemPoolFilenames.Add(itemPool, fileName);
                }
                
                
            }
            // Yes. Create specific array.
            var array = Array.CreateInstance(ItemPools.Values.ElementAt(0).GetType(), ItemPools.Count);
            for (var i = 0; i < ItemPools.Values.Count; i++)
            {
                array.SetValue(ItemPools.Values.ElementAt(i), i);
            }
            output = array;
            using (var writer = new XmlTextWriter(ItemPoolFilenames[ItemPools.Values.ElementAt(0)], Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                try
                {
                    IntermediateSerializer.Serialize(writer, output, null);
                }
                catch (InvalidContentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

               
            
        }

        /// <summary>
        /// Renames the item pool with the specified name.
        /// </summary>
        /// <param name="oldName">The name of the item pool to rename.</param>
        /// <param name="newName">The new name.</param>
        public static void Rename(string oldName, string newName)
        {
            // Require a name.
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Name must not be empty.");
            }

            // Skip if nothing changes.
            if (newName.Equals(oldName))
            {
                return;
            }

            // Make sure the name isn't yet taken.
            if (HasItemPool(newName))
            {
                // Already taken.
                throw new ArgumentException("Factory name already taken, please choose another one.");
            }

            var itemPool = ItemPools[oldName];
            ItemPools.Remove(oldName);
            ItemPools.Add(newName, itemPool);

            OnItemPoolNameChanged(oldName, newName);
        }


        /// <summary>
        /// Adds a single itempool to the list of known itempools.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public static void Add(ItemPool itemPool)
        {
            ItemPools.Add(itemPool.Name, itemPool);
           //var type = factory.GetType();
           // FactoriesByType[type].Add(factory);
            OnItemPoolAdded(itemPool);
        }
        /// <summary>
        /// Determines whether a Item Pool with the specified name exists.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if the Item Pool exists; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasItemPool(string name)
        {
            return ItemPools.ContainsKey(name);
        }
        /// <summary>
        /// Removes the specified item pool.
        /// </summary>
        /// <param name="itemPool">The item pool.</param>
        public static void Remove(ItemPool itemPool)
        {
            if (itemPool == null || string.IsNullOrWhiteSpace(itemPool.Name) || !ItemPools.ContainsKey(itemPool.Name))
            {
                return;
            }

            ItemPools.Remove(itemPool.Name);
            ItemPoolFilenames.Remove(itemPool);

            OnItemPoolRemoved(itemPool);
        }
        /// <summary>
        /// Clears the lists with known factories as well as the list with
        /// factories in the GUI.
        /// </summary>
        public static void Clear()
        {
            ItemPools.Clear();

            ItemPoolFilenames.Clear();

            OnItemPoolCleared();
        }
        private static void OnItemPoolAdded(ItemPool itemPool)
        {
            if (ItemPoolAdded != null)
            {
                ItemPoolAdded(itemPool);
            }
        }

        private static void OnItemPoolRemoved(ItemPool itemPool)
        {
            if (ItemPoolRemoved != null)
            {
                ItemPoolRemoved(itemPool);
            }
        }

        private static void OnItemPoolNameChanged(string oldName, string newName)
        {
            if (ItemPoolNameChanged != null)
            {
                ItemPoolNameChanged(oldName, newName);
            }
        }

        private static void OnItemPoolCleared()
        {
            if (ItemPoolCleared != null)
            {
                ItemPoolCleared();
            }
        }
        /// <summary>
        /// Gets the all known itempools.
        /// </summary>
        /// <returns>All known itempools.</returns>
        public static IEnumerable<ItemPool> GetItemPool()
        {
            return ItemPools.Values;
        }

        /// <summary>
        /// Gets the ItemPool with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The itempool with that name.</returns>
        public static ItemPool GetItemPool(string name)
        {
            ItemPool result;
            ItemPools.TryGetValue(name ?? string.Empty, out result);
            return result;
        }
    }
}
