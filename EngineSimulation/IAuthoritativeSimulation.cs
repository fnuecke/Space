namespace Engine.Simulation
{
    /// <summary>Interface to simulations that may be requested to prune non-authoritative commands.</summary>
    public interface IAuthoritativeSimulation : ISimulation
    {
        /// <summary>
        ///     Forces the state to remove any pending commands that would be handled in the next <see cref="ISimulation.Update"/>
        ///     run.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if any commands were removed.
        /// </returns>
        bool SkipNonAuthoritativeCommands();
    }
}