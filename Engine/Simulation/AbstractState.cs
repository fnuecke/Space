using System;
using System.Collections.Generic;
using Engine.Commands;

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
    public abstract class AbstractState<TState, TSteppable> : IState<TState, TSteppable>
        where TState : AbstractState<TState, TSteppable>
        where TSteppable : ISteppable<TState, TSteppable>
    {
        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        public long CurrentFrame { get; protected set; }

        /// <summary>
        /// Enumerator over all children.
        /// </summary>
        public IEnumerator<TSteppable> Children { get { return steppables.GetEnumerator(); } }

        /// <summary>
        /// Getter to return <c>this</c> pointer of actual implementation type... damn generics.
        /// </summary>
        protected abstract TState ThisState { get; }

        /// <summary>
        /// List of child updateables this state drives.
        /// </summary>
        protected List<TSteppable> steppables = new List<TSteppable>();

        /// <summary>
        /// List of queued commands to execute in the future.
        /// </summary>
        protected SortedDictionary<long, List<ISimulationCommand>> commands =
            new SortedDictionary<long, List<ISimulationCommand>>();

        /// <summary>
        /// Add an steppable object to the list of participants of this state.
        /// </summary>
        /// <param name="updateable">the object to add.</param>
        public virtual void Add(TSteppable steppable)
        {
            steppables.Add(steppable);
            steppable.State = ThisState;
        }

        /// <summary>
        /// Remove an steppable object to the list of participants of this state.
        /// </summary>
        /// <param name="updateable">the object to remove.</param>
        public virtual void Remove(TSteppable steppable)
        {
            steppables.Remove(steppable);
            steppable.State = null;
        }

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
        public virtual void Update()
        {
            // Increment frame number.
            ++CurrentFrame;

            // Execute any commands for the current frame.
            if (commands.ContainsKey(CurrentFrame))
            {
                foreach (var command in commands[CurrentFrame])
                {
                    HandleCommand(command);
                }
                commands.Remove(CurrentFrame);
            }

            // Update all objects in this state.
            foreach (var steppable in steppables)
            {
                steppable.Update();
            }
        }

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        public virtual void PushCommand(ISimulationCommand command)
        {
            if (command.Frame <= CurrentFrame)
            {
                throw new ArgumentException("Command is from a frame in the past.");
            }
            if (!commands.ContainsKey(command.Frame))
            {
                commands.Add(command.Frame, new List<ISimulationCommand>());
            }
            commands[command.Frame].Add(command);
        }

        public abstract object Clone();

        /// <summary>
        /// Call this from the implemented Clone() method to clone basic properties.
        /// </summary>
        /// <param name="clone"></param>
        protected virtual object CloneTo(AbstractState<TState, TSteppable> clone)
        {
            foreach (var steppable in steppables)
            {
                clone.steppables.Add((TSteppable)steppable.Clone());
            }
            foreach (var keyValue in commands)
            {
                clone.commands.Add(keyValue.Key, new List<ISimulationCommand>(keyValue.Value));
            }
            clone.CurrentFrame = CurrentFrame;
            return clone;
        }

        /// <summary>
        /// Implement this to handle commands. This will be called for each command
        /// at the moment it should be applied.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleCommand(ISimulationCommand command);
    }
}
