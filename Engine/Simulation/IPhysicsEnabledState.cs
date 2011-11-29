using System.Collections.Generic;
using Engine.Physics;
using Engine.Serialization;

namespace Engine.Simulation
{
    public interface IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData> : IState<TState, TSteppable, TCommandType, TPlayerData>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TCommandType : struct
        where TPlayerData : IPacketizable
    {

        /// <summary>
        /// A list of collideables which registered themselves in the state.
        /// </summary>
        ICollection<ICollideable> Collideables { get; }

    }
}
