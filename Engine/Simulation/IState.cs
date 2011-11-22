using System;
using System.Collections.Generic;
using Engine.Commands;

namespace Engine.Simulation
{
    /// <summary>
    /// Minimal interface to be implemented by simulation states.
    /// </summary>
    public interface IState<TSteppable> : ICloneable where TSteppable : ICloneable
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
        /// Advance the simulation by one frame.
        /// </summary>
        void Update();

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        void PushCommand(ISimulationCommand command);

    }
}
