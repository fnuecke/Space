namespace Engine.Commands
{
    /// <summary>
    /// Type of commands that can be injected into a running simulation.
    /// </summary>
    public interface ISimulationCommand<T> : ICommand<T>
        where T : struct
    {
        /// <summary>
        /// The frame the command was issued in.
        /// </summary>
        long Frame { get; }
    }
}
