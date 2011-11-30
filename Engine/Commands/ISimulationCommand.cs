using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Type of commands that can be injected into a running simulation.
    /// </summary>
    public interface ISimulationCommand<T, TPlayerData> : ICommand<T, TPlayerData>
        where T : struct
        where TPlayerData : IPacketizable
    {
        /// <summary>
        /// The frame the command was issued in.
        /// </summary>
        long Frame { get; }
    }
}
