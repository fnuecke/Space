using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Random;
using Engine.Serialization;
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

        #endregion

        #region Logic

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
                return;
            }

            // Get the cell info to know what faction we're spawning for.
            var cellInfo = ((UniverseSystem)Manager.GetSystem(UniverseSystem.TypeId)).GetCellInfo(m.Id);

            // The area covered by the cell.
            FarRectangle cellArea;
            cellArea.X = CellSystem.CellSize * m.X;
            cellArea.Y = CellSystem.CellSize * m.Y;
            cellArea.Width = CellSystem.CellSize;
            cellArea.Height = CellSystem.CellSize;

            // Create some ships at random positions.
            for (var i = 0; i < 20; ++i)
            {
                FarPosition spawnPoint;
                spawnPoint.X = _random.NextInt32((int)cellArea.Left, (int)cellArea.Right);
                spawnPoint.Y = _random.NextInt32((int)cellArea.Top, (int)cellArea.Bottom);

                var leader = EntityFactory.CreateAIShip(Manager, "L1_AI_Ship", cellInfo.Faction, spawnPoint, _random);
                ((ArtificialIntelligence)Manager.GetComponent(leader, ArtificialIntelligence.TypeId)).Roam(ref cellArea);
                var squad = Manager.AddComponent<Squad>(leader).Initialize();
                squad.Formation = Squad.FormationType.FilledWedge;
                for (var j = 0; j < 25; ++j)
                {
                    var ship = EntityFactory.CreateAIShip(Manager, "L1_AI_Ship", cellInfo.Faction, spawnPoint, _random);
                    Manager.AddComponent<Squad>(ship).Initialize();
                    squad.AddMember(ship);
                    var ai = (ArtificialIntelligence)Manager.GetComponent(ship, ArtificialIntelligence.TypeId);
                    ai.Roam(ref cellArea); // fallback for when leader dies
                    ai.Guard(leader);
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
            packet.ReadPacketizableInto(ref _random);
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
        public override AbstractSystem NewInstance()
        {
            var copy = (ShipSpawnSystem)base.NewInstance();

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
        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (ShipSpawnSystem)into;

            _random.CopyInto(copy._random);
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
            return base.ToString() + ", Random=" + _random;
        }

        #endregion
    }
}
