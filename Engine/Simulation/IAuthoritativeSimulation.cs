namespace Engine.Simulation
{
    /// <summary>
    /// Interface to simulations that may be requested to prune non-authoritative
    /// commands.
    /// </summary>
    public interface IAuthoritativeSimulation : ISimulation
    {
        /// <summary>
        /// Forces the state to remove any pending commands that
        /// would be handled in the next <c>Update()</c> run.
        /// </summary>
        /// <returns><c>true</c> if any commands were removed.</returns>
        bool SkipTentativeCommands();
    }
}
