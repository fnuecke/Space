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
    public abstract class AbstractState : IState
    {
        #region Properties

        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        public long CurrentFrame { get; protected set; }

        /// <summary>
        /// Enumerator over all children.
        /// </summary>
        public IEnumerable<IEntity> Children { get { return entities; } }

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
        protected IList<IEntity> entities = new List<IEntity>();

        #endregion

        #region Constructor

        protected AbstractState()
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
            entities.Add(entity);
            SystemManager.AddEntity(entity);
        }

        /// <summary>
        /// Get a entity's current representation in this state by its id.
        /// </summary>
        /// <param name="entityUid">the id of the entity to look up.</param>
        /// <returns>the current representation in this state.</returns>
        public IEntity GetEntity(long entityUid)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].UID == entityUid)
                {
                    return entities[i];
                }
            }
            return null;
        }
        
        /// <summary>
        /// Remove an entity object to the list of participants of this state.
        /// </summary>
        /// <param name="entity">the object to remove.</param>
        public void RemoveEntity(IEntity entity)
        {
            RemoveEntity(entity.UID);
        }

        /// <summary>
        /// Remove a entity object by its id.
        /// </summary>
        /// <param name="entityUid">the remove object.</param>
        public IEntity RemoveEntity(long entityUid)
        {
            if (entityUid >= 0)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i].UID == entityUid)
                    {
                        IEntity entity = entities[i];
                        SystemManager.RemoveEntity(entity);
                        entities.RemoveAt(i);
                        return entity;
                    }
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
        public virtual void Update()
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
            SystemManager.Update();
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

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(entities.Count));
            List<IEntity> withId = new List<IEntity>();
            if (entities.Count > 0)
            {
                foreach (var entity in entities)
                {
                    if (entity.UID > 0)
                    {
                        withId.Add(entity);
                    }
                }
            }
            withId.Sort((a, b) => a.UID.CompareTo(b.UID));
            foreach (var entity in withId)
            {
                entity.Hash(hasher);
            }
        }

        public abstract object Clone();

        public virtual void Packetize(Packet packet)
        {
            packet.Write(CurrentFrame);

            packet.Write(commands.Count);
            foreach (var command in commands)
            {
                Packetizer.Packetize(command, packet);
            }

            packet.Write(entities.Count);
            foreach (var entity in entities)
            {
                Packetizer.Packetize(entity, packet);
            }
        }

        public virtual void Depacketize(Packet packet)
        {
            // Get the current frame of the simulation.
            CurrentFrame = packet.ReadInt64();

            // Continue with reading the list of commands.
            commands.Clear();
            int numCommands = packet.ReadInt32();
            for (int j = 0; j < numCommands; ++j)
            {
                PushCommand(Packetizer.Depacketize<ICommand>(packet));
            }

            // And finally the objects. Remove the one we know before that.
            entities.Clear();
            int numEntitys = packet.ReadInt32();
            for (int i = 0; i < numEntitys; ++i)
            {
                var entity = Packetizer.Depacketize<IEntity>(packet);
                entities.Add(entity);
            }
        }

        /// <summary>
        /// Call this from the implemented Clone() method to clone basic properties.
        /// </summary>
        /// <param name="clone"></param>
        protected virtual object CloneTo(AbstractState clone)
        {
            clone.CurrentFrame = CurrentFrame;

            // Commands are immutable, so just copy the reference.
            clone.commands.Clear();
            clone.commands.AddRange(commands);

            // Object however need to add clones!
            clone.entities.Clear();
            foreach (var entity in entities)
            {
                clone.AddEntity((IEntity)entity.Clone());
            }

            return clone;
        }

        #endregion
    }
}
