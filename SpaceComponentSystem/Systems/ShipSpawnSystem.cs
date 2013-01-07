﻿using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
using Engine.Util;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Messages;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Manages spawning dynamic objects for cells, such as random ships.
    /// </summary>
    public sealed class ShipSpawnSystem : AbstractUpdatingComponentSystem<ShipSpawner>, IMessagingSystem
    {
        #region Fields

        /// <summary>
        /// Randomizer used for sampling new ships and, when it applies, their
        /// positions.
        /// </summary>
        private MersenneTwister _random = new MersenneTwister(0);

        /// <summary>
        /// Tracks remaining number of mob groups to spawn per cell
        /// (after a cell was toggled to 'living'). This is used to
        /// spread the spawning across several frames to reduce freezes.
        /// </summary>
        [CopyIgnore, PacketizerIgnore]
        private List<Tuple<ulong, int>> _cellSpawns = new List<Tuple<ulong, int>>();

        #endregion

        #region Logic

        public override void Update(long frame)
        {
            // See if we have some pending spawns.
            if (_cellSpawns.Count > 0)
            {
                // Prefer cells with players in them.
                var avatars = (AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId);
                var index = -1;
                foreach (var avatar in avatars.Avatars)
                {
                    var avatarPosition = ((Transform)Manager.GetComponent(avatar, Transform.TypeId)).Translation;
                    var avatarCell = CellSystem.GetCellIdFromCoordinates(avatarPosition);
                    index = _cellSpawns.FindIndex(x => x.Item1 == avatarCell);
                    if (index >= 0)
                    {
                        break;
                    }
                }

                // Fall back to using the first cell, if there's no player in any.
                if (index < 0)
                {
                    index = _cellSpawns.Count - 1;
                }

                // Pop the entry (tuples are immutable).
                var spawn = _cellSpawns[index];
                _cellSpawns.RemoveAt(index);

                // If the cell is still active (if it died we drop this entry) do the spawn.
                if (((CellSystem)Manager.GetSystem(CellSystem.TypeId)).IsCellActive(spawn.Item1))
                {
                    ProcessSpawn(spawn.Item1);
                    // If there's stuff left to do push it back again.
                    if (spawn.Item2 > 1)
                    {
                        _cellSpawns.Add(Tuple.Create(spawn.Item1, spawn.Item2 - 1));
                    }
                }
            }

            base.Update(frame);
        }

        /// <summary>
        /// Updates the component by checking if it's time to spawn a new entity.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ShipSpawner component)
        {
            if (component.Cooldown > 0)
            {
                --component.Cooldown;
            }
            else
            {
                var faction = ((Faction)Manager.GetComponent(component.Entity, Faction.TypeId));
                var translation = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId)).Translation;
                foreach (var target in component.Targets)
                {
                    CreateAttackingShip(ref translation, target, faction.Value);
                }

                component.Cooldown = component.SpawnInterval;
            }
        }

        /// <summary>
        /// Processes a single spawn from the top of the spawn queue.
        /// </summary>
        private void ProcessSpawn(ulong id)
        {
            // Get the cell position.
            int x, y;
            CellSystem.GetCellCoordinatesFromId(id, out x, out y);

            // Get the cell info to know what faction we're spawning for.
            var cellInfo = ((UniverseSystem)Manager.GetSystem(UniverseSystem.TypeId)).GetCellInfo(id);

            // The area covered by the cell.
            FarRectangle cellArea;
            cellArea.X = x;
            cellArea.Y = y;
            cellArea.Width = CellSystem.CellSize;
            cellArea.Height = CellSystem.CellSize;

            // Get center point for spawn group.
            FarPosition spawnPoint;
            spawnPoint.X = _random.NextInt32((int)cellArea.Left, (int)cellArea.Right);
            spawnPoint.Y = _random.NextInt32((int)cellArea.Top, (int)cellArea.Bottom);

            // Configuration for spawned ships.
            string[] ships;
            ArtificialIntelligence.AIConfiguration[] configurations = null;
            var formation = SquadSystem.Formations.None;

            // TODO different groups, based on cell info? definable via editor maybe?
            if (_random.NextDouble() < 0.5f)
            {
                ships = new[]
                {
                    "L1_AI_Ship",
                    "L1_AI_Ship",
                    "L1_AI_Ship",
                    "L1_AI_Ship",
                    "L1_AI_Ship",
                    "L1_AI_Ship"
                };
                configurations = new[]
                {
                    new ArtificialIntelligence.AIConfiguration
                    {
                        AggroRange = 600
                    }
                };
                formation = SquadSystem.Formations.Block;
            }
            else
            {
                ships = new[]
                {
                    "L1_AI_Ship",
                    "L1_AI_Ship",
                    "L1_AI_Ship",
                    "L1_AI_Ship",
                    "L1_AI_Ship"
                };
                configurations = new[]
                {
                    new ArtificialIntelligence.AIConfiguration
                    {
                        AggroRange = 800
                    }
                };
                formation = SquadSystem.Formations.Vee;
            }

            // Spawn all ships.
            Squad leaderSquad = null;
            for (var i = 0; i < ships.Length; i++)
            {
                // Get the configuration for this particular ship. If we don't have enough configurations
                // we just re-use the last existing one.
                var configuration = configurations != null && configurations.Length > 0
                                 ? configurations[Math.Min(i, configurations.Length - 1)]
                                 : null;
                
                // Get a position nearby the spawn (avoids spawning all ships in one point).
                var spawnPosition = spawnPoint;
                spawnPoint.X += _random.NextInt32(-100, 100);
                spawnPoint.Y += _random.NextInt32(-100, 100);

                // Create the ship and get the AI component.
                var ship = EntityFactory.CreateAIShip(Manager, ships[i], cellInfo.Faction, spawnPosition, _random, configuration);
                var ai = (ArtificialIntelligence)Manager.GetComponent(ship, ArtificialIntelligence.TypeId);
                
                // Push fallback roam behavior, if an area has been specified.
                ai.Roam(ref cellArea);

                // If we have a squad push the squad component.
                if (ships.Length > 1)
                {
                    var squad = Manager.AddComponent<Squad>(ship);
                    // If we're not the leader we guard him, otherwise mark us as
                    // the squad leader (ergo: first loop iteration).
                    if (leaderSquad != null)
                    {
                        leaderSquad.AddMember(ship);
                        ai.Guard(leaderSquad.Entity);
                    }
                    else
                    {
                        leaderSquad = squad;
                        squad.Formation = formation;
                    }
                }
            }
        }

        /// <summary>
        /// Creates an attacking ship.
        /// </summary>
        /// <param name="startPosition">The start position.</param>
        /// <param name="targetEntity">The target entity.</param>
        /// <param name="faction">The faction.</param>
        public void CreateAttackingShip(ref FarPosition startPosition, int targetEntity, Factions faction)
        {
            var ship = EntityFactory.CreateAIShip(Manager, "L1_AI_Ship", faction, startPosition, _random);
            var ai = ((ArtificialIntelligence)Manager.GetComponent(ship, ArtificialIntelligence.TypeId));
            ai.Attack(targetEntity);
        }

        /// <summary>
        /// Creates an attacking ship.
        /// </summary>
        /// <param name="startPosition">The start position.</param>
        /// <param name="targetPosition">The target position.</param>
        /// <param name="faction">The faction.</param>
        public void CreateAttackingShip(ref FarPosition startPosition, ref FarPosition targetPosition, Factions faction)
        {
            var ship = EntityFactory.CreateAIShip(Manager, "L1_AI_Ship", faction, startPosition, _random);
            var ai = ((ArtificialIntelligence)Manager.GetComponent(ship, ArtificialIntelligence.TypeId));
            ai.AttackMove(ref targetPosition);
        }

        /// <summary>
        /// Called by the manager when an entity was removed.
        /// </summary>
        /// <param name="entity">The entity that was removed.</param>
        public override void OnEntityRemoved(int entity)
        {
            base.OnEntityRemoved(entity);

            foreach (var shipSpawner in Components)
            {
                shipSpawner.Targets.Remove(entity);
            }
        }

        /// <summary>
        /// Checks for cells being activated and spawns some initial ships in
        /// them, to have some base population.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            var cm = message as CellStateChanged?;
            if (cm == null)
            {
                return;
            }

            var m = cm.Value;
            if (!m.IsActive)
            {
                _cellSpawns.RemoveAll(x => x.Item1 == m.Id);
                return;
            }

            _cellSpawns.Add(Tuple.Create(m.Id, 20));
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
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            packet.Write(_cellSpawns.Count);
            for (var i = 0; i < _cellSpawns.Count; i++)
            {
                packet.Write(_cellSpawns[i].Item1);
                packet.Write(_cellSpawns[i].Item2);
            }
            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            _cellSpawns.Clear();
            var spawnCount = packet.ReadInt32();
            for (var i = 0; i < spawnCount; i++)
            {
                var id = packet.ReadUInt64();
                var count = packet.ReadInt32();
                _cellSpawns.Add(Tuple.Create(id, count));
            }
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
        public override AbstractSystem NewInstance()
        {
            var copy = (ShipSpawnSystem)base.NewInstance();

            copy._random = new MersenneTwister(0);
            copy._cellSpawns = new List<Tuple<ulong, int>>();

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
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (ShipSpawnSystem)into;

            copy._cellSpawns.Clear();
            copy._cellSpawns.AddRange(_cellSpawns);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Random=" + _random + ", CellSpawns=" + _cellSpawns.Count;
        }

        #endregion
    }
}
