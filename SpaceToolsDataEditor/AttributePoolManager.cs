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
    internal class AttributePoolManager
    {

        /// <summary>
        /// The flat list of all AttributePools, referenced via their name.
        /// </summary>
        private static readonly Dictionary<string, AttributePool> AttributePools = new Dictionary<string, AttributePool>();

        private static string SavePath;
        /// <summary>
        /// Mapping AttributePools back to the files they came from, for saving.
        /// </summary>
        private static readonly Dictionary<AttributePool, string> AttributePoolFilenames = new Dictionary<AttributePool, string>();

        public delegate void AttributePoolChangedDelegate(AttributePool attributePool);

        public delegate void AttributePoolNameChangedDelegate(string oldName, string newName);

        public delegate void AttributePoolClearedDelegate();

        public static event AttributePoolChangedDelegate AttributePoolAdded;

        public static event AttributePoolChangedDelegate AttributePoolRemoved;


        public static event AttributePoolNameChangedDelegate AttributePoolNameChanged;

        public static event AttributePoolClearedDelegate AttributePoolCleared;
        /// <summary>
        /// Loads all factories found at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        public static void Load(string path)
        {
            SavePath = path;
            SavePath += '/';
            SavePath = SavePath.Replace('\\', '/');
            SavePath = SavePath.Substring(0, SavePath.LastIndexOf('/') + 1);
    
            Clear();
            foreach (var file in Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories))
            {
                using (var reader = new XmlTextReader(file))
                {
                    try
                    {
                        var attributePools = IntermediateSerializer.Deserialize<object>(reader, null) as AttributePool[];
                        if (attributePools != null)
                        {
                            foreach (var attributePool in attributePools)
                            {
                                // Adjust paths.
                                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(attributePool, new Attribute[] { new EditorAttribute(typeof(TextureAssetEditor), typeof(UITypeEditor)) }))
                                {
                                    var value = property.GetValue(attributePool) as string;
                                    if (!string.IsNullOrWhiteSpace(value))
                                    {
                                        property.SetValue(attributePool, value.Replace('\\', '/'));
                                    }
                                }

                                Add(attributePool);
                                AttributePoolFilenames.Add(attributePool, file);
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
            if (AttributePools.Count > 0 && AttributePoolFilenames.Count == 0)
            {
                // TODO
                //return;
                baseFolder = SavePath;
            }
            else
            {
                baseFolder = AttributePoolFilenames.Values.First().Replace('\\', '/');
                baseFolder = baseFolder.Substring(0, baseFolder.LastIndexOf('/') + 1);
            }

            object output;
            foreach (var attributePool in AttributePools.Values)
            {
                if (!AttributePoolFilenames.ContainsKey(attributePool))
                {
                    // No such factories yet, create new file.
                    var fileName = baseFolder + attributePool.GetType().Name + ".xml";
                    AttributePoolFilenames.Add(attributePool, fileName);
                }
                
                
            }
            // Yes. Create specific array.
            var array = Array.CreateInstance(AttributePools.Values.ElementAt(0).GetType(), AttributePools.Count);
            for (var i = 0; i < AttributePools.Values.Count; i++)
            {
                array.SetValue(AttributePools.Values.ElementAt(i), i);
            }
            output = array;
            using (var writer = new XmlTextWriter(AttributePoolFilenames[AttributePools.Values.ElementAt(0)], Encoding.UTF8))
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
            if (HasAttributePool(newName))
            {
                // Already taken.
                throw new ArgumentException("Factory name already taken, please choose another one.");
            }

            var AttributePool = AttributePools[oldName];
            AttributePools.Remove(oldName);
            AttributePools.Add(newName, AttributePool);

            OnAttributePoolNameChanged(oldName, newName);
        }


        /// <summary>
        /// Adds a single AttributePool to the list of known AttributePools.
        /// </summary>
        /// <param name="attributePool">The factory.</param>
        public static void Add(AttributePool attributePool)
        {
            AttributePools.Add(attributePool.Name, attributePool);
           //var type = factory.GetType();
           // FactoriesByType[type].Add(factory);
            OnAttributePoolAdded(attributePool);
        }
        /// <summary>
        /// Determines whether a Item Pool with the specified name exists.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if the Item Pool exists; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAttributePool(string name)
        {
            return AttributePools.ContainsKey(name);
        }
        /// <summary>
        /// Removes the specified item pool.
        /// </summary>
        /// <param name="AttributePool">The item pool.</param>
        public static void Remove(AttributePool AttributePool)
        {
            if (AttributePool == null || string.IsNullOrWhiteSpace(AttributePool.Name) || !AttributePools.ContainsKey(AttributePool.Name))
            {
                return;
            }

            AttributePools.Remove(AttributePool.Name);
            AttributePoolFilenames.Remove(AttributePool);

            OnAttributePoolRemoved(AttributePool);
        }
        /// <summary>
        /// Clears the lists with known factories as well as the list with
        /// factories in the GUI.
        /// </summary>
        public static void Clear()
        {
            AttributePools.Clear();

            AttributePoolFilenames.Clear();

            OnAttributePoolCleared();
        }
        private static void OnAttributePoolAdded(AttributePool AttributePool)
        {
            if (AttributePoolAdded != null)
            {
                AttributePoolAdded(AttributePool);
            }
        }

        private static void OnAttributePoolRemoved(AttributePool AttributePool)
        {
            if (AttributePoolRemoved != null)
            {
                AttributePoolRemoved(AttributePool);
            }
        }

        private static void OnAttributePoolNameChanged(string oldName, string newName)
        {
            if (AttributePoolNameChanged != null)
            {
                AttributePoolNameChanged(oldName, newName);
            }
        }

        private static void OnAttributePoolCleared()
        {
            if (AttributePoolCleared != null)
            {
                AttributePoolCleared();
            }
        }
        /// <summary>
        /// Gets the all known AttributePools.
        /// </summary>
        /// <returns>All known AttributePools.</returns>
        public static IEnumerable<AttributePool> GetAttributePools()
        {
            return AttributePools.Values;
        }

        /// <summary>
        /// Gets the AttributePool with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The AttributePool with that name.</returns>
        public static AttributePool GetAttributePool(string name)
        {
            AttributePool result;
            AttributePools.TryGetValue(name ?? string.Empty, out result);
            return result;
        }
    }
}
