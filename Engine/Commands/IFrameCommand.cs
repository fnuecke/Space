using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Type of commands that can be injected into a running simulation.
    /// </summary>
    public interface IFrameCommand<TPlayerData> : ICommand<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        /// <summary>
        /// The frame the command was issued in.
        /// </summary>
        long Frame { get; set; }
    }
}
