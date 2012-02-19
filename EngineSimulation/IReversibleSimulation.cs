using System;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;

namespace Engine.Simulation
{
    /// <summary>
    /// A special kind of state which allows rolling back the simulation to a
    /// previous state. It will automatically try to roll back if a command is
    /// pushed that was issued earlier than the current frame (if the command
    /// was authoritative, i.e. not tentative).
    /// </summary>
    interface IReversibleSimulation : ISimulation
    {
        /// <summary>
        /// Dispatched when the state needs to roll back further than it can
        /// to accommodate a server command. The handler must trigger the
        /// process of getting a valid snapshot, and feed it back to the
        /// state (e.g. using Depacketize()).
        /// </summary>
        event EventHandler<EventArgs> Invalidated;

        /// <summary>
        /// Tells if the state is currently waiting to be synchronized.
        /// </summary>
        bool WaitingForSynchronization { get; }

        /// <summary>
        /// Mark the state as invalid (desynchronized). Will trigger a new
        /// <c>ThresholdExceeded</c> event.
        /// </summary>
        void Invalidate();

        /// <summary>
        /// Run the simulation to the given frame, which may be in the past.
        /// </summary>
        /// <param name="gameTime">The elapsed time since the last call.</param>
        /// <param name="frame">the frame to run to.</param>
        void RunToFrame(GameTime gameTime, long frame);

        /// <summary>
        /// Push a command to be executed at the given frame.  This will roll
        /// back, if necessary, to remove the object, meaning it can trigger
        /// desyncs.
        /// </summary>
        /// <param name="command">the command to push.</param>
        /// <param name="frame">the frame in which to execute the command.</param>
        void PushCommand(Command command, long frame);
    }
}
