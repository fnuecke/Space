using Engine.ComponentSystem;
using Engine.Serialization;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Simulation
{
    /// <summary>
    /// Minimal interface to be implemented by simulation states.
    /// </summary>
    public interface ISimulation : IPacketizable, IHashable, ICopyable<ISimulation>
    {
        /// <summary>
        /// The current frame of the simulation the state represents.
        /// </summary>
        long CurrentFrame { get; }

        /// <summary>
        /// The component system manager in use in this simulation.
        /// </summary>
        IManager Manager { get; }

        /// <summary>
        /// Advance the simulation by one frame.
        /// </summary>
        /// <param name="gameTime">The elapsed time since the last call to Update.</param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Apply a given command to the simulation state.
        /// </summary>
        /// <param name="command">the command to apply.</param>
        void PushCommand(Command command);
    }
}
