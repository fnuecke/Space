using System.Collections.Generic;
using Engine.Physics;

namespace Engine.Simulation
{
    public interface IPhysicsEnabledState<TSteppable> : IState<TSteppable>
        where TSteppable : IPhysicsSteppable<TSteppable>
    {

        /// <summary>
        /// A list of collideables which registered themselves in the state.
        /// </summary>
        ICollection<ICollideable> Collideables { get; }

    }
}
