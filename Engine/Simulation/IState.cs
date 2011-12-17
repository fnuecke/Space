using System;
using System.Collections.Generic;
using Engine.Commands;
using Engine.ComponentSystem;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Minimal interface to be implemented by simulation states.
    /// </summary>
    public interface IState : ICloneable, IPacketizable, IHashable
    {
        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        long CurrentFrame { get; }

        /// <summary>
        /// Iterator over all entities registered with this simulation.
        /// </summary>
        IEnumerable<IEntity> Children { get; }

        /// <summary>
        /// Packetizer used for serialization purposes.
        /// </summary>
        IPacketizer Packetizer { get; }

        /// <summary>
        /// Add an entity object to the list of participants of this state.
        /// </summary>
        /// <param name="entity">the object to add.</param>
        void AddEntity(IEntity entity);

        /// <summary>
        /// Get a entity's current representation in this state by its id.
        /// </summary>
        /// <param name="entityUid">the id of the entity to look up.</param>
        /// <returns>the current representation in this state.</returns>
        IEntity GetEntity(long entityUid);

        /// <summary>
        /// Remove an entity object to the list of participants of this state.
        /// </summary>
        /// <param name="updateable">the object to remove.</param>
        void RemoveEntity(IEntity entity);

        /// <summary>
        /// Remove a entity object by its id.
        /// </summary>
        /// <param name="entityUid">the remove object.</param>
        IEntity RemoveEntity(long entityUid);
        
        /// <summary>
        /// Register a component system with this simulation.
        /// </summary>
        /// <param name="system">the system to register.</param>
        void AddSystem(IComponentSystem system);

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
        void Update();

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        void PushCommand(ICommand command);
    }
}
