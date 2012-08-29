using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.FarMath;
using Engine.Random;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Systems;
using Space.ComponentSystem.Util;
using Space.Data;
using Space.Util;

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
        public static int CreatePlayerShip(IManager manager, PlayerClassType playerClass, int playerNumber, FarPosition position)
        {
            // Player ships must be 'static', i.e. not have random attributes, so we don't need a randomizer.
            var entity = FactoryLibrary.SampleShip(manager, playerClass.GetShipFactoryName(), playerNumber.ToFaction(), position, null);

            // Remember the class.
            manager.AddComponent<PlayerClass>(entity).Initialize(playerClass);

            // Mark it as the player's avatar.
            manager.AddComponent<Avatar>(entity).Initialize(playerNumber);

            // Make it respawn (after 5 seconds).
            manager.AddComponent<Respawn>(entity).Initialize((int)(5 * Settings.TicksPerSecond), new HashSet<Type> {
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

            // Allow leveling up.
            manager.AddComponent<Experience>(entity).Initialize(100, 100f, 2.15f);

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
        public static int CreateAIShip(IManager manager, string blueprint, Factions faction, FarPosition position, IUniformRandom random)
        {
            var entity = FactoryLibrary.SampleShip(manager, blueprint, faction, position, random);

            // Add to the index from which entities will automatically removed
            // on cell death and mark it (for translation checks into empty space).
            var index = ((Index)manager.GetComponent(entity, Index.TypeId));
            index.SetIndexGroupsMask(index.IndexGroupsMask | CellSystem.CellDeathAutoRemoveIndexGroupMask);
            manager.AddComponent<CellDeath>(entity);

            var input = (ShipControl)manager.GetComponent(entity, ShipControl.TypeId);
            input.Stabilizing = true;
            manager.AddComponent<ArtificialIntelligence>(entity).Initialize(random.NextUInt32());

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
            manager.AddComponent<Transform>(entity).Initialize(((Transform)manager.GetComponent(center, Transform.TypeId)).Translation);
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
