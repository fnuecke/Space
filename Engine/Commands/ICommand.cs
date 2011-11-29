using System;
using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Minimal interface for commands.
    /// </summary>
    /// <typeparam name="T">the enum type to use to differentiate commands.</typeparam>
    public interface ICommand<T, TPlayerData> : IPacketizable, IEquatable<ICommand<T, TPlayerData>>
        where T : struct
        where TPlayerData : IPacketizable
    {
        /// <summary>
        /// Whether the command is signed (e.g. by a server) (<c>false</c>)
        /// or came from an untrustworthy source (e.g. another client) (<c>true</c>).
        /// </summary>
        bool IsTentative { get; set; }

        /// <summary>
        /// The player that performed the action causing the command.
        /// </summary>
        Player<TPlayerData> Player { get; set; }

        /// <summary>
        /// The type of the command, used to determine which handler to use for it.
        /// </summary>
        T Type { get; }
    }
}
