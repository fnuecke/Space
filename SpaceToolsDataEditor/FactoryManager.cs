using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    internal static class FactoryManager
    {
        public delegate void FactoryAddedDelegate(IFactory factory);

        public delegate void FactoriesClearedDelegate();

        public static event FactoryAddedDelegate FactoryAdded;

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
                        var factories = IntermediateSerializer.Deserialize<IFactory[]>(reader, null);
                        foreach (var factory in factories)
                        {
                            Add(factory);
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
        /// Adds a single factory to the list of known factories.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public static void Add(IFactory factory)
        {
            Factories.Add(factory.Name, factory);
            var type = factory.GetType();
            FactoriesByType[type].Add(factory);
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
            OnFactoriesCleared();
        }

        /// <summary>
        /// Gets the factory with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The factory with that name.</returns>
        public static IFactory GetFactory(string name)
        {
            IFactory result;
            Factories.TryGetValue(name, out result);
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

        private static void OnFactoriesCleared()
        {
            if (FactoriesCleared != null)
            {
                FactoriesCleared();
            }
        }
    }
}
