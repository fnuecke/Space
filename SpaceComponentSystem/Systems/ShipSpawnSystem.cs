using System;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Manages spawning dynamic objects for cells, such as random ships.
    /// </summary>
    public sealed class ShipSpawnSystem : AbstractComponentSystem<ShipSpawner>
    {
        #region Fields

        /// <summary>
        /// Randomizer used for sampling new ships and, when it applies, their
        /// positions.
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Logic

        protected override void UpdateComponent(GameTime gameTime, long frame, ShipSpawner component)
        {
            if (component.Cooldown > 0)
            {
                --component.Cooldown;
            }
            else
            {
                var faction = Manager.GetComponent<Faction>(component.Entity);
                var translation = Manager.GetComponent<Transform>(component.Entity).Translation;
                foreach (var target in component.Targets)
                {
                    CreateAttackingShip(ref translation, target, faction.Value);
                }

                component.Cooldown = component.SpawnInterval;
            }
        }

        public void CreateAttackingShip(ref Vector2 startPosition, int targetEntity, Factions faction)
        {
            var ship = EntityFactory.CreateAIShip(Manager, "L1_AI_Ship", faction, startPosition, _random);
            var ai = Manager.GetComponent<ArtificialIntelligence>(ship);
            ai.Attack(targetEntity);
        }

        public void CreateAttackingShip(ref Vector2 startPosition, ref Vector2 targetPosition, Factions faction)
        {
            var ship = EntityFactory.CreateAIShip(Manager, "L1_AI_Ship", faction, startPosition, _random);
            var ai = Manager.GetComponent<ArtificialIntelligence>(ship);
            ai.AttackMove(ref targetPosition);
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Checks for cells being activated and spawns some initial ships in
        /// them, to have some base population.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public override void Receive<T>(ref T message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)(ValueType)message;
                if (info.State)
                {
                    // Get the cell info to know what faction we're spawning for.
                    var cellInfo = Manager.GetSystem<UniverseSystem>().GetCellInfo(info.Id);
                    
                    // The area covered by the cell.
                    Rectangle cellArea;
                    cellArea.X = CellSystem.CellSize * info.X;
                    cellArea.Y = CellSystem.CellSize * info.Y;
                    cellArea.Width = CellSystem.CellSize;
                    cellArea.Height = CellSystem.CellSize;

                    // Create some ships at random positions.
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 spawnPoint;
                        spawnPoint.X = _random.NextInt32(cellArea.Left, cellArea.Right);
                        spawnPoint.Y = _random.NextInt32(cellArea.Top, cellArea.Bottom);
                        var ship = EntityFactory.CreateAIShip(
                            Manager, "L1_AI_Ship", cellInfo.Faction, spawnPoint, _random);
                        var ai = Manager.GetComponent<ArtificialIntelligence>(ship);
                        ai.Roam(ref cellArea);
                    }
                }
            }
            else if (message is EntityRemoved)
            {
                var entity = ((EntityRemoved)(ValueType)message).Entity;
                foreach (var shipSpawner in Components)
                {
                    shipSpawner.Targets.Remove(entity);   
                }
            }
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return packet.Write(_random);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            _random = packet.ReadPacketizableInto(_random);
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            _random.Hash(hasher);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem DeepCopy()
        {
            var copy = (ShipSpawnSystem)base.DeepCopy();

            copy._random = new MersenneTwister(0);

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the system. The passed system must be of the
        /// same type.
        /// 
        /// <para>
        /// This clones any contained data types to return an instance that
        /// represents a complete copy of the one passed in.
        /// </para>
        /// </summary>
        /// <remarks>The manager for the system to copy into must be set to the
        /// manager into which the system is being copied.</remarks>
        /// <returns>A deep copy, with a fully cloned state of this one.</returns>
        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            var copy = (ShipSpawnSystem)base.DeepCopy(into);

            copy._random = _random.DeepCopy(_random);

            return copy;
        }

        #endregion
    }
}
