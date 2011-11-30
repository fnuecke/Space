using System.Collections.Generic;
using Engine.Physics;
using Engine.Serialization;

namespace Engine.Simulation
{
    public interface IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : IState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IPhysicsEnabledState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : IPhysicsSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
    {

        /// <summary>
        /// A list of collideables which registered themselves in the state.
        /// </summary>
        ICollection<ICollideable> Collideables { get; }

    }
}
