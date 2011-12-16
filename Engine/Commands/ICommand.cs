using System;
using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Minimal interface for commands.
    /// </summary>
    public interface ICommand<TPlayerData, TPacketizerContext>
        : IPacketizable<TPlayerData, TPacketizerContext>, IEquatable<ICommand<TPlayerData, TPacketizerContext>>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// Whether the command is signed (e.g. by a server) (<c>true</c>)
        /// or came from an untrustworthy source (e.g. another client) (<c>false</c>).
        /// </summary>
        bool IsAuthoritative { get; set; }

        /// <summary>
        /// The player that performed the action causing the command.
        /// </summary>
        Player<TPlayerData, TPacketizerContext> Player { get; set; }

        /// <summary>
        /// The type of the command, used to determine which handler to use for it.
        /// </summary>
        Enum Type { get; }
    }
}
