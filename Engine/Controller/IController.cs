using Engine.Commands;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;

namespace Engine.Controller
{
    /// <summary>
    /// Defines public functionality of a game controller.
    /// </summary>
    public interface IController<TSession, TProtocol, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TProtocol : IProtocol
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        TSession Session { get; }

        /// <summary>
        /// The network protocol in use by the session of this controller.
        /// </summary>
        TProtocol Protocol { get; }

        /// <summary>
        /// Add this controller as a listener to the given emitter, handling
        /// whatever commands it produces.
        /// </summary>
        /// <param name="emitter">the emitter to attach to.</param>
        void AddEmitter(ICommandEmitter<TCommand, TCommandType, TPlayerData, TPacketizerContext> emitter);

        /// <summary>
        /// Remove this controller as a listener from the given emitter.
        /// </summary>
        /// <param name="emitter">the emitter to detach from.</param>
        void RemoveEmitter(ICommandEmitter<TCommand, TCommandType, TPlayerData, TPacketizerContext> emitter);
    }
}
