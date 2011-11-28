using System;
using System.Collections.Generic;
using Engine.Commands;
using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Minimal interface to be implemented by simulation states.
    /// </summary>
    /// <typeparam name="TState">the type of state the object will be used together with.</typeparam>
    /// <typeparam name="TSteppable">the type of steppable used in the state.</typeparam>
    public interface IState<TState, TSteppable, TCommandType> : ICloneable, IPacketizable
        where TState : IState<TState, TSteppable, TCommandType>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType>
        where TCommandType : struct
    {
        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        long CurrentFrame { get; }

        /// <summary>
        /// Iterator over all updateables registered with this simulation.
        /// </summary>
        IEnumerator<TSteppable> Children { get; }

        /// <summary>
        /// Add an steppable object to the list of participants of this state.
        /// </summary>
        /// <param name="updateable">the object to add.</param>
        void Add(TSteppable steppable);

        /// <summary>
        /// Remove an steppable object to the list of participants of this state.
        /// </summary>
        /// <param name="updateable">the object to remove.</param>
        void Remove(TSteppable steppable);

        /// <summary>
        /// Remove a steppable object by its id.
        /// </summary>
        /// <param name="steppableUid">the remove object.</param>
        TSteppable Remove(long steppableUid);

        /// <summary>
        /// Get a steppable's current representation in this state by its id.
        /// </summary>
        /// <param name="steppableUid">the id of the steppable to look up.</param>
        /// <returns>the current representation in this state.</returns>
        TSteppable Get(long steppableUid);

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
        void Update();

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        void PushCommand(ISimulationCommand<TCommandType> command);
    }
}
