using System;
using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Minimal interface for commands.
    /// </summary>
    public interface ICommand : IPacketizable, IEquatable<ICommand>
    {
        /// <summary>
        /// Whether the command is signed (e.g. by a server) (<c>true</c>)
        /// or came from an untrustworthy source (e.g. another client) (<c>false</c>).
        /// </summary>
        bool IsAuthoritative { get; set; }

        /// <summary>
        /// The number of the player that issued the command.
        /// </summary>
        int PlayerNumber { get; set; }

        /// <summary>
        /// The type of the command, used to determine which handler to use for it.
        /// </summary>
        Enum Type { get; }
    }
}
