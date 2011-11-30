using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Type of commands that can be injected into a running simulation.
    /// </summary>
    public interface ISimulationCommand<T, TPlayerData, TPacketizerContext> : ICommand<T, TPlayerData, TPacketizerContext>
        where T : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
    {
        /// <summary>
        /// The frame the command was issued in.
        /// </summary>
        long Frame { get; }
    }
}
