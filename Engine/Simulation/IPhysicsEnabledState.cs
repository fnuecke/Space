using System.Collections.Generic;
using Engine.Physics;

namespace Engine.Simulation
{
    public interface IPhysicsEnabledState<TState, TSteppable> : IState<TState, TSteppable>
        where TState : IPhysicsEnabledState<TState, TSteppable>
        where TSteppable : IPhysicsSteppable<TState, TSteppable>
    {

        /// <summary>
        /// A list of collideables which registered themselves in the state.
        /// </summary>
        ICollection<ICollideable> Collideables { get; }

    }
}
