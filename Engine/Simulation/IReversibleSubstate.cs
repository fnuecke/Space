using Engine.Serialization;

namespace Engine.Simulation
{
    public interface IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData> : IState<TState, TSteppable, TCommandType, TPlayerData>
        where TState : IState<TState, TSteppable, TCommandType, TPlayerData>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TCommandType : struct
        where TPlayerData : IPacketizable
    {
        /// <summary>
        /// Forces the state to remove any pending commands that
        /// would be handled in the next <c>Update()</c> run.
        /// </summary>
        /// <returns><c>true</c> if any commands were removed.</returns>
        bool SkipTentativeCommands();
    }
}
