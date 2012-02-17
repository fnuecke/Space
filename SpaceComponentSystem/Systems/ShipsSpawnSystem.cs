using System;
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
    public sealed class ShipsSpawnSystem : AbstractSystem
    {
        #region Fields

        /// <summary>
        /// Randomizer used for sampling new ships and, when it applies, their
        /// positions.
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        #endregion

        #region Constructor

        public ShipsSpawnSystem()
        {
            // We want to sync our randomizer.
            ShouldSynchronize = true;
        }

        #endregion

        #region Logic

        public void CreateAttackingShip(ref Vector2 startPosition, int targetEntity, Factions faction)
        {
            var aicommand = new AiComponent.AiCommand(targetEntity, 2000, AiComponent.Order.Move);
            var ship = EntityFactory.CreateAIShip("L1_AI_Ship",
                faction, startPosition, Manager.EntityManager, _random, aicommand);

            Manager.EntityManager.AddEntity(ship);
        }

        public void CreateAttackingShip(ref Vector2 startPosition, ref Vector2 targetPosition, Factions faction)
        {
            var aicommand = new AiComponent.AiCommand(targetPosition, 2000, AiComponent.Order.Move);
            var ship = EntityFactory.CreateAIShip("L1_AI_Ship",
                faction, startPosition, Manager.EntityManager, _random, aicommand);

            Manager.EntityManager.AddEntity(ship);
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Checks for cells being activated and spawns some initial ships in
        /// them, to have some base population.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public override void HandleMessage<T>(ref T message)
        {
            if (message is CellStateChanged)
            {
                var info = (CellStateChanged)(ValueType)message;
                if (info.State)
                {
                    const int cellSize = CellSystem.CellSize;

                    // Get cell position to offset spawn positions.
                    Vector2 cellPosition;
                    cellPosition.X = cellSize * info.X;
                    cellPosition.Y = cellSize * info.Y;

                    // Get the cell info to know what faction we're spawning for.
                    var cellInfo = Manager.GetSystem<UniverseSystem>().GetCellInfo(info.Id);
                    
                    // Create some ships at random positions.
                    Vector2 spawnPoint;
                    for (int i = 0; i < 10; i++)
                    {
                        spawnPoint.X = (float)(_random.NextDouble() * cellSize + cellPosition.X);
                        spawnPoint.Y = (float)(_random.NextDouble() * cellSize + cellPosition.Y);
                        var order = new AiComponent.AiCommand(spawnPoint, cellSize >> 1, AiComponent.Order.Guard);
                        var ship = EntityFactory.CreateAIShip(
                            "L1_AI_Ship",
                            cellInfo.Faction,
                            spawnPoint,
                            Manager.EntityManager,
                            _random,
                            order);

                        Manager.EntityManager.AddEntity(ship);
                    }
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

        public override ISystem DeepCopy(ISystem into)
        {
            var copy = (ShipsSpawnSystem)base.DeepCopy(into);

            if (copy == into)
            {
                copy._random = _random.DeepCopy(_random);
            }
            else
            {
                copy._random = _random.DeepCopy();
            }

            return copy;
        }

        #endregion
    }
}
