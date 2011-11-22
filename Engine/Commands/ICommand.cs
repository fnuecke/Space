using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Minimal interface for commands.
    /// </summary>
    public interface ICommand
    {

        /// <summary>
        /// The type of the command, used to determine which handler to use for it.
        /// </summary>
        uint Type { get; }

        /// <summary>
        /// Whether the command is signed (e.g. by a server) (<code>false</code>)
        /// or came from an untrustworthy source (e.g. another client) (<code>true</code>).
        /// </summary>
        bool IsTentative { get; set; }

        /// <summary>
        /// The player that performed the action causing the command. Only set
        /// for received commands.
        /// </summary>
        int Player { get; set; }

        /// <summary>
        /// Serialize the command to the given packet.
        /// </summary>
        /// <param name="packet">the packet to serialize the command to.</param>
        void Write(Packet packet);

    }
}
