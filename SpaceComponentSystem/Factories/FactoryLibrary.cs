using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.FarMath;
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
        /// Mapping of names to factories.
        /// </summary>
        private static readonly Dictionary<string, IFactory> Factories = new Dictionary<string, IFactory>();

        /// <summary>
        /// Mapping of types to names to item factories.
        /// </summary>
        //private static readonly Dictionary<string, ItemFactory> ItemFactories = new Dictionary<string, ItemFactory>();

        /// <summary>
        /// Mapping of types to names to ship factories.
        /// </summary>
        //private static readonly Dictionary<string, ShipFactory> ShipFactories = new Dictionary<string, ShipFactory>();

        /// <summary>
        /// Mapping of types to names to planet factories.
        /// </summary>
        //private static readonly Dictionary<string, PlanetFactory> PlanetFactories = new Dictionary<string, PlanetFactory>();

        /// <summary>
        /// Mapping of types to names to sun factories.
        /// </summary>
        //private static readonly Dictionary<string, SunFactory> SunFactories = new Dictionary<string, SunFactory>();

        /// <summary>
        /// Mapping of types to names to sun factories.
        /// </summary>
        //private static readonly Dictionary<string, SunSystemFactory> SunSystemFactories = new Dictionary<string, SunSystemFactory>();

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

            Initialize<ArmorFactory>("Data/Armor", content);
            Initialize<ReactorFactory>("Data/Reactors", content);
            Initialize<SensorFactory>("Data/Sensors", content);
            Initialize<ShieldFactory>("Data/Shields", content);
            Initialize<ThrusterFactory>("Data/Thrusters", content);
            Initialize<WeaponFactory>("Data/Weapons", content);
            Initialize<ShipFactory>("Data/Ships", content);
            Initialize<PlanetFactory>("Data/Planets", content);
            Initialize<SunFactory>("Data/Suns", content);
            Initialize<SunSystemFactory>("Data/SunSystems", content);

            _isInitialized = true;
        }

        /// <summary>
        /// Helper for initializing a specific type.
        /// </summary>
        private static void Initialize<TFactory>(string assetName, ContentManager content)
            where TFactory : IFactory
        {
            foreach (var factory in content.Load<TFactory[]>(assetName))
            {
                Factories[factory.Name] = factory;
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
            var factory = Factories[name] as ItemFactory;
            if (factory != null)
            {
                return factory.Sample(manager, random);
            }
            return 0;
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
        public static int SampleItem(IManager manager, string name, FarPosition position, IUniformRandom random)
        {
            var factory = Factories[name] as ItemFactory;
            if (factory == null)
            {
                return 0;
            }
            var item = factory.Sample(manager, random);
            var transform = ((Transform)manager.GetComponent(item, Transform.TypeId));
            if (transform != null)
            {
                transform.SetTranslation(position);
                transform.ApplyTranslation();
            }
            var renderer = ((TextureRenderer)manager.GetComponent(item, TextureRenderer.TypeId));
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
        public static int SampleShip(IManager manager, string name, Factions faction, FarPosition position, IUniformRandom random)
        {
            var factory = Factories[name] as ShipFactory;
            return factory != null ? factory.SampleShip(manager, faction, position, random) : 0;
        }

        /// <summary>
        /// Samples a new sun with the specified name.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="center">The entity the planet should orbit around.</param>
        /// <param name="angle">The base angle for orbit ellipses.</param>
        /// <param name="radius">The base orbiting radius this planet will have.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled sun.
        /// </returns>
        public static int SamplePlanet(IManager manager, string name, int center, float angle, float radius, IUniformRandom random)
        {
            var factory = Factories[name] as PlanetFactory;
            if (factory != null)
            {
                return factory.SamplePlanet(manager, center, angle, radius, random);
            }
            return 0;
        }

        /// <summary>
        /// Samples a new sun with the specified name.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="cellCenter">The center of the cell for which the sun is created.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled sun.
        /// </returns>
        public static int SampleSun(IManager manager, string name, Vector2 cellCenter, IUniformRandom random)
        {
            var factory = Factories[name] as SunFactory;
            if (factory != null)
            {
                return factory.SampleSun(manager, cellCenter, random);
            }
            return 0;
        }

        /// <summary>
        /// Samples a new sun system with the specified name.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="name">The logical name of the ship to sample.</param>
        /// <param name="cellCenter">The center of the cell for which the sun is created.</param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>
        /// The sampled sun.
        /// </returns>
        public static void SampleSunSystem(IManager manager, string name, Vector2 cellCenter, IUniformRandom random)
        {
            var factory = Factories[name] as SunSystemFactory;
            if (factory != null)
            {
                factory.SampleSunSystem(manager, cellCenter, random);
            }
        }

        #endregion
    }
}
