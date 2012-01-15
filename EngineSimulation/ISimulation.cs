using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Simulation.Commands;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Minimal interface to be implemented by simulation states.
    /// </summary>
    public interface ISimulation : ICopyable<ISimulation>, IPacketizable, IHashable
    {
        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        long CurrentFrame { get; }

        /// <summary>
        /// The entity manager in use in this simulation.
        /// </summary>
        IEntityManager EntityManager { get; }

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
        void Update();

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        void PushCommand(Command command);
    }
}
