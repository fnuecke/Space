using System;
using System.Collections.Generic;
using Engine.Commands;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Base class for state implementation.
    /// 
    /// <para>
    /// State implementations sub-classing this base class must take care of
    /// (at least) two things:
    /// - Handling of commands (via the HandleCommand function).
    /// - Cloning of the state (may use CloneTo to take care of the basics).
    /// </para>
    /// </summary>
    public abstract class AbstractSimulation : ISimulation
    {
        #region Properties

        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        public long CurrentFrame { get; protected set; }

        /// <summary>
        /// Enumerator over all children.
        /// </summary>
        public IEnumerable<IEntity> Entities { get { return _entities; } }

        /// <summary>
        /// The component system manager in use in this simulation.
        /// </summary>
        public IComponentSystemManager SystemManager { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// List of queued commands to execute in the next step.
        /// </summary>
        protected List<ICommand> commands = new List<ICommand>();

        /// <summary>
        /// List of child entities this state drives.
        /// </summary>
        private IList<IEntity> _entities = new List<IEntity>();

        #endregion

        #region Constructor

        protected AbstractSimulation()
        {
            this.SystemManager = new CompositeComponentSystem();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Add an entity object to the list of participants of this state.
        /// </summary>
        /// <param name="entity">the object to add.</param>
        public void AddEntity(IEntity entity)
        {
            _entities.Add(entity);
            foreach (var component in entity.Components)
            {
                SystemManager.AddComponent(component);
            }
        }

        /// <summary>
        /// Remove an entity object to the list of participants of this state.
        /// </summary>
        /// <param name="entity">the object to remove.</param>
        public void RemoveEntity(IEntity entity)
        {
            if (_entities.Remove(entity))
            {
                foreach (var component in entity.Components)
                {
                    SystemManager.RemoveComponent(component);
                }
            }
        }

        /// <summary>
        /// Remove a entity object by its id.
        /// </summary>
        /// <param name="entityUid">the remove object.</param>
        public IEntity RemoveEntity(long entityUid)
        {
            if (entityUid >= 0)
            {
                for (int i = 0; i < _entities.Count; i++)
                {
                    if (_entities[i].UID == entityUid)
                    {
                        IEntity entity = _entities[i];
                        _entities.RemoveAt(i);
                        foreach (var component in entity.Components)
                        {
                            SystemManager.RemoveComponent(component);
                        }
                        return entity;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get a entity's current representation in this state by its id.
        /// </summary>
        /// <param name="entityUid">the id of the entity to look up.</param>
        /// <returns>the current representation in this state.</returns>
        public IEntity GetEntity(long entityUid)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].UID == entityUid)
                {
                    return _entities[i];
                }
            }
            return null;
        }
        
        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        public virtual void PushCommand(ICommand command)
        {
            // There's a chance we have that command in a tentative version. Let's check.
            int known = commands.FindIndex(x => x.Equals(command));
            if (known >= 0)
            {
                // Already there! Use the authoritative one (or if neither is do nothing).
                if (!commands[known].IsAuthoritative && command.IsAuthoritative)
                {
                    commands.RemoveAt(known);
                    commands.Insert(known, command);
                }
            }
            else
            {
                // New one, append.
                commands.Add(command);
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
        public void Update()
        {
            // Increment frame number.
            ++CurrentFrame;

            // Execute any commands for the current frame.
            foreach (var command in commands)
            {
                HandleCommand(command);
            }
            commands.Clear();

            // Update all systems.
            SystemManager.Update(ComponentSystemUpdateType.Logic);
        }

        /// <summary>
        /// Implement this to handle commands. This will be called for each command
        /// at the moment it should be applied. The implementation must be done in
        /// a way that behaves the same for any permutation of a given set of non-equal
        /// commands. I.e. the order of the command execution must not make a difference.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleCommand(ICommand command);

        #endregion

        #region Hashing / Cloning / Serialization

        public virtual void Packetize(Packet packet)
        {
            // Write the frame number we're currently in.
            packet.Write(CurrentFrame);

            // Then serialize all pending commands for the next frame.
            packet.Write(commands.Count);
            foreach (var command in commands)
            {
                Packetizer.Packetize(command, packet);
            }

            // And eventually, the list of entities we track in this simulation.
            packet.Write(_entities.Count);
            foreach (var entity in _entities)
            {
                Packetizer.Packetize(entity, packet);
            }
        }

        public virtual void Depacketize(Packet packet)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Clear component lists. Just do a clone, which preserves the
            // non-entity bound components for us.
            SystemManager = (IComponentSystemManager)SystemManager.Clone();

            // Continue with reading the list of commands.
            commands.Clear();
            int numCommands = packet.ReadInt32();
            for (int j = 0; j < numCommands; ++j)
            {
                PushCommand(Packetizer.Depacketize<ICommand>(packet));
            }

            // And finally the objects. Remove the one we know before that.
            _entities.Clear();
            int numEntitys = packet.ReadInt32();
            for (int i = 0; i < numEntitys; ++i)
            {
                AddEntity(Packetizer.Depacketize<IEntity>(packet));
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            // Write the number of entities, and then get all entities with a
            // unique id. We only use those, because we can sort them. We cannot
            // guarantee the order of the other entities, so we cannot guarantee
            // a deterministic hash.
            hasher.Put(BitConverter.GetBytes(_entities.Count));

            // Get entities with an id.
            List<IEntity> withId = new List<IEntity>();
            foreach (var entity in _entities)
            {
                if (entity.UID > 0)
                {
                    withId.Add(entity);
                }
            }
            // Sort 'em and hash 'em.
            withId.Sort((a, b) => a.UID.CompareTo(b.UID));
            foreach (var entity in withId)
            {
                entity.Hash(hasher);
            }
        }

        /// <summary>
        /// Implements deep cloning.
        /// </summary>
        /// <returns>A deep copy of this simulation.</returns>
        public virtual object Clone()
        {
            var copy = (AbstractSimulation)MemberwiseClone();

            // Clone system manager.
            copy.SystemManager = (IComponentSystemManager)SystemManager.Clone();

            // Copy commands directly (they are immutable).
            copy.commands = new List<ICommand>(commands);

            // Clone all entities.
            copy._entities = new List<IEntity>();
            foreach (var entity in _entities)
            {
                copy.AddEntity((IEntity)entity.Clone());
            }

            return copy;
        }

        #endregion
    }
}
