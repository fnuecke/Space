using Engine.Commands;
using Engine.Serialization;
using Engine.Session;

namespace Engine.Controller
{
    /// <summary>
    /// Defines public functionality of a game controller.
    /// </summary>
    public interface IController<TSession, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        TSession Session { get; }
    }
}
