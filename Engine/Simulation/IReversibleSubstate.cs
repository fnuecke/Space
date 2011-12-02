using Engine.Serialization;

namespace Engine.Simulation
{
    public interface IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : IState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Forces the state to remove any pending commands that
        /// would be handled in the next <c>Update()</c> run.
        /// </summary>
        /// <returns><c>true</c> if any commands were removed.</returns>
        bool SkipTentativeCommands();
    }
}
