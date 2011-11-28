using System.Collections.Generic;
using Engine.Physics;

namespace Engine.Simulation
{
    public interface IPhysicsEnabledState<TState, TSteppable, TCommandType> : IState<TState, TSteppable, TCommandType>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType>
        where TCommandType : struct
    {

        /// <summary>
        /// A list of collideables which registered themselves in the state.
        /// </summary>
        ICollection<ICollideable> Collideables { get; }

    }
}
