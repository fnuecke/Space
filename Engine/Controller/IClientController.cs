using Engine.Commands;
using Engine.Session;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for client controllers, which take some form of direct input.
    /// </summary>
    public interface IClientController<TCommand> : IController<IClientSession>
        where TCommand : ICommand
    {
        /// <summary>
        /// Add this controller as a listener to the given emitter, handling
        /// whatever commands it produces.
        /// </summary>
        /// <param name="emitter">the emitter to attach to.</param>
        void AddEmitter(ICommandEmitter<TCommand> emitter);

        /// <summary>
        /// Remove this controller as a listener from the given emitter.
        /// </summary>
        /// <param name="emitter">the emitter to detach from.</param>
        void RemoveEmitter(ICommandEmitter<TCommand> emitter);
    }
}
