using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Spatial.Components;
using Engine.FarMath;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>Allows lookup of constraints by name.</summary>
    public static class FactoryLibrary
    {
        #region Fields

        /// <summary>Mapping of names to factories.</summary>
        private static readonly Dictionary<string, IFactory> Factories = new Dictionary<string, IFactory>();

        /// <summary>Mapping of names to item pools.</summary>
        private static readonly Dictionary<string, ItemPool> ItemPools = new Dictionary<string, ItemPool>();

        /// <summary>Mapping of names to attribute pools.</summary>
        private static readonly Dictionary<string, AttributePool> AttributePools =
            new Dictionary<string, AttributePool>();

        #endregion

        #region Initialization

        /// <summary>Initializes the library with the specified content manager.</summary>
        /// <param name="content">The content manager to use to load constraints.</param>
        public static void LoadContent(ContentManager content)
        {
            Factories.Clear();
            ItemPools.Clear();
            AttributePools.Clear();

            foreach (var factory in new[]
            {
                "Data/FuselageFactory",
                "Data/ReactorFactory",
                "Data/SensorFactory",
                "Data/ShieldFactory",
                "Data/ShipFactory",
                "Data/ThrusterFactory",
                "Data/WeaponFactory",
                "Data/WingFactory",
                "Data/PlanetFactory",
                "Data/SunFactory",
                "Data/SunSystemFactory"
            })
            {
                Load(factory, content);
            }

            foreach (var pool in content.Load<ItemPool[]>("Data/ItemPool"))
            {
                ItemPools.Add(pool.Name, pool);
            }

            foreach (var pool in content.Load<AttributePool[]>("Data/AttributePool"))
            {
                AttributePools.Add(pool.Name, pool);
            }
        }

        /// <summary>Helper for initializing a specific type.</summary>
        private static void Load(string assetName, ContentManager content)
        {
            foreach (var factory in content.Load<IFactory[]>(assetName))
            {
                Factories.Add(factory.Name, factory);
            }
        }

        #endregion

        #region Accessors

        /// <summary>Gets the factory with the specified name, or null if no such factory exists.</summary>
        /// <param name="name">The name of the factory.</param>
        /// <returns>The factory with that name, or null.</returns>
        public static IFactory GetFactory(string name)
        {
            IFactory factory;
            Factories.TryGetValue(name, out factory);
            return factory;
        }

        /// <summary>Samples a new item with the specified name.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the item to sample.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled item.</returns>
        public static int SampleItem(IManager manager, string name, IUniformRandom random)
        {
            if (string.IsNullOrWhiteSpace(name) || !Factories.ContainsKey(name))
            {
                return 0;
            }
            var factory = Factories[name] as ItemFactory;
            if (factory != null)
            {
                return factory.Sample(manager, random);
            }
            return 0;
        }

        /// <summary>Samples a new item with the specified name at the specified position.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the item to sample.</param>
        /// <param name="position">The position at which to spawn the item.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled item.</returns>
        public static int SampleItem(IManager manager, string name, FarPosition position, IUniformRandom random)
        {
            if (string.IsNullOrWhiteSpace(name) || !Factories.ContainsKey(name))
            {
                return 0;
            }
            var factory = Factories[name] as ItemFactory;
            if (factory == null)
            {
                return 0;
            }
            var item = factory.Sample(manager, random);
            var transform = ((Transform) manager.GetComponent(item, Transform.TypeId));
            if (transform != null)
            {
                transform.Position = position;
                transform.Update();
            }
            return item;
        }

        /// <summary>Samples a new ship with the specified name.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <param name="position">The initial position of the ship.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled ship.</returns>
        public static int SampleShip(
            IManager manager, string name, Factions faction, FarPosition position, IUniformRandom random)
        {
            if (string.IsNullOrWhiteSpace(name) || !Factories.ContainsKey(name))
            {
                return 0;
            }
            var factory = Factories[name] as ShipFactory;
            return factory != null ? factory.Sample(manager, faction, position, random) : 0;
        }

        /// <summary>Samples a new sun with the specified name.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled sun.</returns>
        public static int SamplePlanet(IManager manager, string name, IUniformRandom random)
        {
            if (string.IsNullOrWhiteSpace(name) || !Factories.ContainsKey(name))
            {
                return 0;
            }
            var factory = Factories[name] as PlanetFactory;
            if (factory != null)
            {
                return factory.Sample(manager, random);
            }
            return 0;
        }

        /// <summary>Samples a new sun with the specified name.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="cellCenter">The center of the cell for which the sun is created.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled sun.</returns>
        public static int SampleSun(IManager manager, string name, FarPosition cellCenter, IUniformRandom random)
        {
            if (string.IsNullOrWhiteSpace(name) || !Factories.ContainsKey(name))
            {
                return 0;
            }
            var factory = Factories[name] as SunFactory;
            if (factory != null)
            {
                return factory.Sample(manager, cellCenter, random);
            }
            return 0;
        }

        /// <summary>Samples a new sun system with the specified name.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="cellCenter">The center of the cell for which the sun is created.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled sun.</returns>
        public static void SampleSunSystem(IManager manager, string name, FarPosition cellCenter, IUniformRandom random)
        {
            if (string.IsNullOrWhiteSpace(name) || !Factories.ContainsKey(name))
            {
                return;
            }
            var factory = Factories[name] as SunSystemFactory;
            if (factory != null)
            {
                factory.SampleSunSystem(manager, cellCenter, random);
            }
        }

        /// <summary>Samples a new sun system with the specified name.</summary>
        /// <param name="manager">The manager.</param>
        /// <param name="cellCenter">The center of the cell for which the sun is created.</param>
        /// <param name="random">The randomizer to use.</param>
        public static void SampleTestObject(IManager manager, FarPosition cellCenter, IUniformRandom random)
        {
            var radius = 10f;
            var entity = manager.AddEntity();
            manager.AddComponent<Transform>(entity).Initialize(
                new FarRectangle(-radius, -radius, radius * 2, radius * 2),
                cellCenter,
                0,
                // Add to indexes for lookup.
                DetectableSystem.IndexGroupMask | // Can be detected.
                CellSystem.CellDeathAutoRemoveIndexGroupMask | // Will be removed when out of bounds.
                CameraSystem.IndexGroupMask); // Must be detectable by the camera.
                
            // Make it detectable.
            manager.AddComponent<Detectable>(entity).Initialize("Textures/Radar/Icons/radar_sun");
            // Make it glow.
            manager.AddComponent<TestObjectRenderer>(entity).Initialize(radius * 0.95f, Color.White);
        }

        /// <summary>Gets the item pool with the specified name.</summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static ItemPool GetItemPool(string name)
        {
            ItemPool result;
            ItemPools.TryGetValue(name, out result);
            return result;
        }

        /// <summary>Gets the attribute pool with the specified name.</summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static AttributePool GetAttributePool(string name)
        {
            AttributePool result;
            AttributePools.TryGetValue(name, out result);
            return result;
        }

        #endregion
    }
}