using System.Collections.Generic;
using Engine.Physics;

namespace Engine.Simulation
{
    /// <summary>
    /// Base class for states that takes care of some common functionality.
    /// </summary>
    public abstract class PhysicsEnabledState<TState, TSteppable> : AbstractState<TState, TSteppable>, IPhysicsEnabledState<TState, TSteppable>
        where TState : PhysicsEnabledState<TState, TSteppable>
        where TSteppable : IPhysicsSteppable<TState, TSteppable>
    {

        /// <summary>
        /// List of collideables in the state (they register themselves upon being adding).
        /// </summary>
        public ICollection<ICollideable> Collideables { get; private set; }

        protected PhysicsEnabledState()
        {
            Collideables = new List<ICollideable>();
        }

        /// <summary>
        /// Override update to introduce pre- and post-update steppes for resolving collisions.
        /// </summary>
        public override void Update()
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
        }
    }
}
