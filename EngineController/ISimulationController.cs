using Engine.Session;
using Engine.Simulation;

namespace Engine.Controller
{
    /// <summary>
    /// Public interface for controllers managing a game state.
    /// </summary>
    public interface ISimulationController<out TSession> : IController<TSession>
        where TSession : ISession
    {
        /// <summary>
        /// The underlying simulation controlled by this controller.
        /// </summary>
        ISimulation Simulation { get; }

        /// <summary>
        /// The target game speed we try to run at, if possible.
        /// </summary>
        float TargetSpeed { get; set; }

        /// <summary>
        /// The current actual game speed, based on possible slow-downs due
        /// to the server or other clients.
        /// </summary>
        float ActualSpeed { get; }
    }
}
