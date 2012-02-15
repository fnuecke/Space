using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Space.Data;

namespace Space.ComponentSystem.Factories
{
    /// <summary>
    /// Allows lookup of constraints by name.
    /// </summary>
    public static class FactoryLibrary
    {
        #region Fields

        /// <summary>
        /// Mapping of types to names to item factories.
        /// </summary>
        private static readonly Dictionary<string, ItemFactory> _itemFactories = new Dictionary<string, ItemFactory>();

        /// <summary>
        /// Mapping of types to names to ship factories.
        /// </summary>
        private static readonly Dictionary<string, ShipFactory> _shipFactories = new Dictionary<string, ShipFactory>();

        private static bool _isInitialized;

        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes the library with the specified content manager.
        /// </summary>
        /// <param name="content">The content manager to use to load constraints.</param>
        public static void Initialize(ContentManager content)
        {
            if (!_isInitialized)
            {
                Initialize<ArmorFactory, ItemFactory>(_itemFactories, "Data/Armor", content);
                Initialize<ReactorFactory, ItemFactory>(_itemFactories, "Data/Reactors", content);
                Initialize<SensorFactory, ItemFactory>(_itemFactories, "Data/Sensors", content);
                Initialize<ShieldFactory, ItemFactory>(_itemFactories, "Data/Shields", content);
                Initialize<ThrusterFactory, ItemFactory>(_itemFactories, "Data/Thrusters", content);
                Initialize<WeaponFactory, ItemFactory>(_itemFactories, "Data/Weapons", content);

                Initialize<ShipFactory, ShipFactory>(_shipFactories, "Data/Ships", content);

                _isInitialized = true;
            }
        }

        /// <summary>
        /// Helper for initializing a specific type.
        /// </summary>
        private static void Initialize<T, F>(Dictionary<string, F> factories, string assetName, ContentManager content)
            where T : IFactory, F
        {
            foreach (var factory in content.Load<T[]>(assetName))
            {
                factories[factory.Name] = factory;
            }
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Samples a new item with the specified name.
        /// </summary>
        /// <param name="name">The logical name of the item to sample.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled item.</returns>
        public static Entity SampleItem(string name, IUniformRandom random)
        {
            return _itemFactories[name].Sample(random);
        }

        /// <summary>
        /// Samples a new item with the specified name at the specified position.
        /// </summary>
        /// <param name="name">The logical name of the item to sample.</param>
        /// <param name="position">The position at which to spawn the item.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled item.</returns>
        public static Entity SampleItem(string name, Vector2 position, IUniformRandom random)
        {
            var item = _itemFactories[name].Sample(random);
            var transform = item.GetComponent<Transform>();
            if (transform != null)
            {
                transform.Translation = position;
            }
            var renderer = item.GetComponent<TransformedRenderer>();
            if (renderer != null)
            {
                renderer.Enabled = true;
            }
            return item;
        }

        /// <summary>
        /// Samples a new ship with the specified name.
        /// </summary>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <param name="position">The initial position of the ship.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled ship.</returns>
        public static Entity SampleShip(string name, Factions faction, Vector2 position, IUniformRandom random)
        {
            return _shipFactories[name].SampleShip(faction, position, random);
        }

        #endregion
    }
}
