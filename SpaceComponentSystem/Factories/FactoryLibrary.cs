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
        private static readonly Dictionary<string, ItemFactory> ItemFactories = new Dictionary<string, ItemFactory>();

        /// <summary>
        /// Mapping of types to names to ship factories.
        /// </summary>
        private static readonly Dictionary<string, ShipFactory> ShipFactories = new Dictionary<string, ShipFactory>();

        private static bool _isInitialized;

        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes the library with the specified content manager.
        /// </summary>
        /// <param name="content">The content manager to use to load constraints.</param>
        public static void Initialize(ContentManager content)
        {
            if (_isInitialized)
            {
                return;
            }

            Initialize<ArmorFactory, ItemFactory>(ItemFactories, "Data/Armor", content);
            Initialize<ReactorFactory, ItemFactory>(ItemFactories, "Data/Reactors", content);
            Initialize<SensorFactory, ItemFactory>(ItemFactories, "Data/Sensors", content);
            Initialize<ShieldFactory, ItemFactory>(ItemFactories, "Data/Shields", content);
            Initialize<ThrusterFactory, ItemFactory>(ItemFactories, "Data/Thrusters", content);
            Initialize<WeaponFactory, ItemFactory>(ItemFactories, "Data/Weapons", content);

            Initialize<ShipFactory, ShipFactory>(ShipFactories, "Data/Ships", content);

            _isInitialized = true;
        }

        /// <summary>
        /// Helper for initializing a specific type.
        /// </summary>
        private static void Initialize<TFactory, TBase>(Dictionary<string, TBase> factories, string assetName, ContentManager content)
            where TFactory : IFactory, TBase
        {
            foreach (var factory in content.Load<TFactory[]>(assetName))
            {
                factories[factory.Name] = factory;
            }
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Samples a new item with the specified name.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the item to sample.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled item.
        /// </returns>
        public static int SampleItem(IManager manager, string name, IUniformRandom random)
        {
            return ItemFactories[name].Sample(manager, random);
        }

        /// <summary>
        /// Samples a new item with the specified name at the specified position.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the item to sample.</param>
        /// <param name="position">The position at which to spawn the item.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled item.
        /// </returns>
        public static int SampleItem(IManager manager, string name, Vector2 position, IUniformRandom random)
        {
            var item = ItemFactories[name].Sample(manager, random);
            var transform = manager.GetComponent<Transform>(item);
            if (transform != null)
            {
                transform.SetTranslation(position);
            }
            var renderer = manager.GetComponent<TextureRenderer>(item);
            if (renderer != null)
            {
                renderer.Enabled = true;
            }
            return item;
        }

        /// <summary>
        /// Samples a new ship with the specified name.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <param name="position">The initial position of the ship.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled ship.
        /// </returns>
        public static int SampleShip(IManager manager, string name, Factions faction, Vector2 position, IUniformRandom random)
        {
            return ShipFactories[name].SampleShip(manager, faction, position, random);
        }

        #endregion
    }
}
