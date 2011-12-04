using Engine.Commands;
using Engine.Serialization;
using Engine.Session;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for client controllers, which take some form of direct input.
    /// </summary>
    public interface IClientController<TSession, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        : IController<TSession, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
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
