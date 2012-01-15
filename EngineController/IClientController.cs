using Engine.Session;
using Engine.Simulation.Commands;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for client controllers, which take some form of direct input.
    /// </summary>
    public interface IClientController<TCommand> : ISimulationController<IClientSession>
        where TCommand : Command
    {
        /// <summary>
        /// Inject a simulation command that was generated via some external
        /// source (e.g. player input).
        /// </summary>
        /// <param name="emitter">The command to inject.</param>
        void PushLocalCommand(TCommand command);
    }
}
