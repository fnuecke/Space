using Engine.Serialization;

namespace Engine.Simulation
{
    public interface IReversibleSubstate<TPlayerData> : IState<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        /// <summary>
        /// Forces the state to remove any pending commands that
        /// would be handled in the next <c>Update()</c> run.
        /// </summary>
        /// <returns><c>true</c> if any commands were removed.</returns>
        bool SkipTentativeCommands();
    }
}
