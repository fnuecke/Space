using System;
using System.Collections.Generic;
using Engine.Commands;
using Engine.Physics;

namespace Engine.Simulation
{

    /// <summary>
    /// Base class for states that takes care of some common functionality.
    /// 
    /// State implementations sub-classing this base class must take care of
    /// (at least) two things:
    /// - Handling of commands (via the HandleCommand function).
    /// - Cloning of the state (may use CloneTo to take care of the basics).
    /// </summary>
    public abstract class PhysicsEnabledState<TSteppable> : IPhysicsEnabledState<TSteppable>
        where TSteppable : IPhysicsSteppable<TSteppable>
    {

        /// <summary>
        /// Enumerator over all children.
        /// </summary>
        public IEnumerator<TSteppable> Children { get { return steppables.GetEnumerator(); } }

        /// <summary>
        /// List of child updateables this state drives.
        /// </summary>
        protected List<TSteppable> steppables = new List<TSteppable>();

        /// <summary>
        /// List of queued commands to execute in the future.
        /// </summary>
        protected SortedDictionary<long, List<ISimulationCommand>> commands =
            new SortedDictionary<long, List<ISimulationCommand>>();

        protected PhysicsEnabledState()
        {
            Collideables = new List<ICollideable>();
        }

        /// <summary>
        /// Implement this to handle commands. This will be called for each command
        /// at the moment it should be applied.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleCommand(ISimulationCommand command);

        /// <summary>
        /// Call this from the implemented Clone() method to clone basic properties.
        /// </summary>
        /// <param name="clone"></param>
        protected object CloneTo(PhysicsEnabledState<TSteppable> clone)
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
            // Collideables will be filled automatically by clones of updateables.
            return clone;
        }

#region IState implementation

        public long CurrentFrame { get; protected set; }

        public ICollection<ICollideable> Collideables { get; private set; }

        public void Add(TSteppable steppable)
        {
            steppables.Add(steppable);
            steppable.State = this;
        }

        public void Remove(TSteppable steppable)
        {
            steppables.Remove(steppable);
            steppable.State = null;
        }

        public void Update()
        {
            if (commands.ContainsKey(CurrentFrame))
            {
                foreach (var command in commands[CurrentFrame])
                {
                    HandleCommand(command);
                }
                commands.Remove(CurrentFrame);
            }
            foreach (var steppable in steppables)
            {
                steppable.PreUpdate();
            }
            foreach (var steppable in steppables)
            {
                steppable.Update();
            }
            foreach (var steppable in steppables)
            {
                steppable.PostUpdate();
            }
            ++CurrentFrame;
        }

        public void PushCommand(ISimulationCommand command)
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

#endregion

    }
}
