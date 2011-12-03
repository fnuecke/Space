using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Type of commands that can be injected into a running simulation.
    /// </summary>
    public interface IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The frame the command was issued in.
        /// </summary>
        long Frame { get; set; }
    }
}
