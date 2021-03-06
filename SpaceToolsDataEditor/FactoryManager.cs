﻿using System;
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
    internal static class FactoryManager
    {
        public delegate void FactoryAddedDelegate(IFactory factory);

        public delegate void FactoryNameChangedDelegate(string oldName, string newName);

        public delegate void FactoriesClearedDelegate();

        public static event FactoryAddedDelegate FactoryAdded;

        public static event FactoryAddedDelegate FactoryRemoved;

        public static event FactoryNameChangedDelegate FactoryNameChanged;

        public static event FactoriesClearedDelegate FactoriesCleared;

        /// <summary>
        /// The flat list of all factories, referenced via their name.
        /// </summary>
        private static readonly Dictionary<string, IFactory> Factories = new Dictionary<string, IFactory>();

        /// <summary>
        /// Lists of all factories, categorized by their type.
        /// </summary>
        private static readonly Dictionary<Type, List<IFactory>> FactoriesByType = new Dictionary<Type, List<IFactory>>();

        /// <summary>
        /// Mapping factories back to the files they came from, for saving.
        /// </summary>
        private static readonly Dictionary<IFactory, string> FactoryFilenames = new Dictionary<IFactory, string>();

        /// <summary>
        /// Initializes the <see cref="FactoryManager"/> class.
        /// </summary>
        static FactoryManager()
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(typeof(IFactory).IsAssignableFrom)
                .Where(p => p.IsClass && !p.IsAbstract))
            {
                // Add list for this type.
                FactoriesByType.Add(type, new List<IFactory>());
            }
        }

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
                        var factories = IntermediateSerializer.Deserialize<object>(reader, null) as IFactory[];
                        if (factories != null)
                        {
                            foreach (var factory in factories)
                            {
                                // Adjust paths.
                                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(factory, new Attribute[]{new EditorAttribute(typeof(TextureAssetEditor),typeof(UITypeEditor))}))
                                {
                                    var value = property.GetValue(factory) as string;
                                    if (!string.IsNullOrWhiteSpace(value))
                                    {
                                        property.SetValue(factory, value.Replace('\\', '/'));
                                    }
                                }

                                Add(factory);
                                FactoryFilenames.Add(factory, file);
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
            if (Factories.Count > 0 && FactoryFilenames.Count == 0)
            {
                // TODO
                return;
            }
            else
            {
                baseFolder = FactoryFilenames.Values.First().Replace('\\', '/');
                baseFolder = baseFolder.Substring(0, baseFolder.LastIndexOf('/') + 1);
            }

            // Group factories by filename.
            var groups = new Dictionary<string, List<IFactory>>();
            foreach (var filename in FactoryFilenames.Values)
            {
                if (!groups.ContainsKey(filename))
                {
                    groups.Add(filename, new List<IFactory>());
                }
            }
            foreach (var factory in Factories.Values)
            {
                if (FactoryFilenames.ContainsKey(factory))
                {
                    groups[FactoryFilenames[factory]].Add(factory);
                }
                else
                {
                    // Try to find existing group with factory of that type.
                    var found = false;
                    foreach (var factoryByType in FactoriesByType[factory.GetType()])
                    {
                        if (FactoryFilenames.ContainsKey(factoryByType))
                        {
                            groups[FactoryFilenames[factoryByType]].Add(factory);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        // No such factories yet, create new file.
                        var fileName = baseFolder + factory.GetType().Name + ".xml";
                        FactoryFilenames.Add(factory, fileName);
                        groups.Add(fileName, new List<IFactory> {factory});
                    }
                }
            }

            // Serialize each collection.
            foreach (var group in groups)
            {
                // Skip empty groups.
                if (group.Value.Count == 0)
                {
                    continue;
                }

                // See if they're all the same.
                object output;
                if (group.Value.All(x => x.GetType() == group.Value[0].GetType()))
                {
                    // Yes. Create specific array.
                    var array = Array.CreateInstance(group.Value[0].GetType(), group.Value.Count);
                    for (var i = 0; i < group.Value.Count; i++)
                    {
                        array.SetValue(group.Value[i], i);
                    }
                    output = array;
                }
                else
                {
                    // Nope.
                    output = group.Value.ToArray();
                }

                using (var writer = new XmlTextWriter(group.Key, Encoding.UTF8))
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
        }

        /// <summary>
        /// Adds a single factory to the list of known factories.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public static void Add(IFactory factory)
        {
            Factories.Add(factory.Name, factory);
            var type = factory.GetType();
            FactoriesByType[type].Add(factory);
            if(factory is SunSystemFactory)
            {
                ResourceManager.AddResource((SunSystemFactory)factory);
            }
            OnFactoryAdded(factory);
        }

        /// <summary>
        /// Clears the lists with known factories as well as the list with
        /// factories in the GUI.
        /// </summary>
        public static void Clear()
        {
            Factories.Clear();
            foreach (var type in FactoriesByType)
            {
                type.Value.Clear();
            }
            FactoryFilenames.Clear();
            OnFactoriesCleared();
        }

        /// <summary>
        /// Determines whether a factory with the specified name exists.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if the factory exists; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasFactory(string name)
        {
            return Factories.ContainsKey(name);
        }

        /// <summary>
        /// Removes the specified factory.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public static void Remove(IFactory factory)
        {
            if (factory == null || string.IsNullOrWhiteSpace(factory.Name) || !Factories.ContainsKey(factory.Name))
            {
                return;
            }

            Factories.Remove(factory.Name);
            FactoriesByType[factory.GetType()].Remove(factory);
            FactoryFilenames.Remove(factory);

            OnFactoryRemoved(factory);
        }

        /// <summary>
        /// Renames the factory with the specified name.
        /// </summary>
        /// <param name="oldName">The name of the factory to rename.</param>
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
            if (HasFactory(newName))
            {
                // Already taken.
                throw new ArgumentException("Factory name already taken, please choose another one.");
            }

            var factory = Factories[oldName];
            Factories.Remove(oldName);
            Factories.Add(newName, factory);

            OnFactoryNameChanged(oldName, newName);
        }

        /// <summary>
        /// Gets the factory with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The factory with that name.</returns>
        public static IFactory GetFactory(string name)
        {
            IFactory result;
            Factories.TryGetValue(name ?? string.Empty, out result);
            return result;
        }

        /// <summary>
        /// Gets the all known factories.
        /// </summary>
        /// <returns>All known factories.</returns>
        public static IEnumerable<IFactory> GetFactories()
        {
            return Factories.Values;
        }

        /// <summary>
        /// Gets all known the factory types.
        /// </summary>
        /// <returns>The known factory types.</returns>
        public static IEnumerable<Type> GetFactoryTypes()
        {
            return FactoriesByType.Keys;
        }

        /// <summary>
        /// Gets all factories that came from the specified file.
        /// </summary>
        public static IEnumerable<IFactory> GetFactoriesFromFile(string assetName)
        {
            var name = assetName.Replace('/', '\\') + ".xml";
            foreach (var pair in FactoryFilenames)
            {
                if (pair.Value.EndsWith(name))
                {
                    yield return pair.Key;
                }
            }
        }

        /// <summary>
        /// Gets all item factories.
        /// </summary>
        /// <returns>All item factories.</returns>
        public static IEnumerable<ItemFactory> GetAllItems()
        {
            foreach (var factoryByType in FactoriesByType)
            {
                if (typeof(ItemFactory).IsAssignableFrom(factoryByType.Key))
                {
                    foreach (var factory in factoryByType.Value)
                    {
                        yield return factory as ItemFactory;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the factory names of all items known of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>All items of that type.</returns>
        public static IEnumerable<IFactory> GetAllItemsOfType(ItemFactory.ItemSlotInfo.ItemType type)
        {
            var factoryType = type.ToFactoryType();
            foreach (var factoryByType in FactoriesByType)
            {
                if (factoryByType.Key != factoryType)
                {
                    continue;
                }
                foreach (var factory in factoryByType.Value)
                {
                    yield return factory;
                }
            }
        }

        private static void OnFactoryAdded(IFactory factory)
        {
            if (FactoryAdded != null)
            {
                FactoryAdded(factory);
            }
        }

        private static void OnFactoryRemoved(IFactory factory)
        {
            if (FactoryRemoved != null)
            {
                FactoryRemoved(factory);
            }
        }

        private static void OnFactoryNameChanged(string oldName, string newName)
        {
            if (FactoryNameChanged != null)
            {
                FactoryNameChanged(oldName, newName);
            }
        }

        private static void OnFactoriesCleared()
        {
            if (FactoriesCleared != null)
            {
                FactoriesCleared();
            }
        }
    }
}
