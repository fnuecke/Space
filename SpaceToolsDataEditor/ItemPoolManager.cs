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

        public delegate void ItemPoolChangedDelegate(ItemPool factory);
        public static event ItemPoolChangedDelegate ItemPoolAdded;
        public static event ItemPoolChangedDelegate ItemPoolRemoved;
        /// <summary>
        /// Loads all factories found at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void Load(string path)
        {

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
