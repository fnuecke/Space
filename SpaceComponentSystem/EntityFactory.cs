using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Systems;
using Space.ComponentSystem.Util;
using Space.Data;

namespace Space.ComponentSystem
{
    public static class EntityFactory
    {
        /// <summary>
        /// Creates a new, player controlled ship.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="playerClass">The player's class, which determines the blueprint.</param>
        /// <param name="playerNumber">The player for whom to create the ship.</param>
        /// <param name="position">The position at which to spawn and respawn the ship.</param>
        /// <returns>
        /// The new ship.
        /// </returns>
        public static int CreatePlayerShip(IManager manager, PlayerClassType playerClass, int playerNumber, Vector2 position)
        {
            // Player ships must be 'static', i.e. not have random attributes, so we don't need a randomizer.
            var entity = FactoryLibrary.SampleShip(manager, playerClass.GetShipFactoryName(), playerNumber.ToFaction(), position, null);

            // Remember the class.
            manager.AddComponent<PlayerClass>(entity).Initialize(playerClass);

            // Mark it as the player's avatar.
            manager.AddComponent<Avatar>(entity).Initialize(playerNumber);

            // Make it respawn.
            manager.AddComponent<Respawn>(entity).Initialize(300, new HashSet<Type> {
                // Make ship uncontrollable.
                typeof(ShipControl),
                typeof(WeaponControl),
                // Disable collisions.
                typeof(CollidableSphere),
                // And movement.
                typeof(Acceleration),
                typeof(Gravitation),
                // Hide it.
                typeof(TextureRenderer),
                typeof(ParticleEffects),
                // And don't regenerate.
                typeof(Health),
                typeof(Energy)
            }, position);

            return entity;
        }

        /// <summary>
        /// Creates a new, AI controlled ship.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="blueprint">The blueprint.</param>
        /// <param name="faction">The faction the ship will belong to.</param>
        /// <param name="position">The position.</param>
        /// <param name="random">The random.</param>
        /// <returns>
        /// The new ship.
        /// </returns>
        public static int CreateAIShip(IManager manager, string blueprint, Factions faction, Vector2 position, IUniformRandom random)
        {
            var entity = FactoryLibrary.SampleShip(manager, blueprint, faction, position, random);

            // Add to the index from which entities will automatically removed on cell death.
            manager.GetComponent<Index>(entity).IndexGroups |=  CellSystem.CellDeathAutoRemoveIndexGroupMask;

            var input = manager.GetComponent<ShipControl>(entity);
            input.Stabilizing = true;
            manager.AddComponent<ArtificialIntelligence>(entity);

            var equipment = manager.GetComponent<Equipment>(entity);

            var item = FactoryLibrary.SampleItem(manager, "L1_AI_Thruster", random);
            equipment.Equip(0, item);

            item = FactoryLibrary.SampleItem(manager, "L1_AI_Reactor", random);
            equipment.Equip(0, item);

            item = FactoryLibrary.SampleItem(manager, "L1_AI_Armor", random);
            equipment.Equip(0, item);

            item = FactoryLibrary.SampleItem(manager, "L1_AI_Weapon", random);
            equipment.Equip(0, item);

            return entity;
        }

        /// <summary>
        /// Creates a new space station.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="texture">The texture to use for rending the station.</param>
        /// <param name="center">The center entity we're orbiting.</param>
        /// <param name="orbitRadius">The orbit radius of the station.</param>
        /// <param name="period">The orbiting period.</param>
        /// <param name="faction">The faction the station belongs to.</param>
        /// <returns></returns>
        public static int CreateStation(IManager manager, string texture, int center, float orbitRadius, float period, Factions faction)
        {
            var entity = manager.AddEntity();

            manager.AddComponent<Faction>(entity).Initialize(faction);
            manager.AddComponent<Transform>(entity).Initialize(manager.GetComponent<Transform>(center).Translation);
            manager.AddComponent<Spin>(entity).Initialize(((float)Math.PI) / period);
            manager.AddComponent<EllipsePath>(entity).Initialize(center, orbitRadius, orbitRadius, 0, period, 0);
            manager.AddComponent<Index>(entity).Initialize(DetectableSystem.IndexGroupMask | CellSystem.CellDeathAutoRemoveIndexGroupMask);
            manager.AddComponent<Detectable>(entity).Initialize("Textures/Stolen/Ships/sensor_array_dish");
            manager.AddComponent<ShipSpawner>(entity);
            manager.AddComponent<TextureRenderer>(entity).Initialize(texture, Color.Lerp(Color.White, faction.ToColor(), 0.5f));

            return entity;
        }
    }
}
