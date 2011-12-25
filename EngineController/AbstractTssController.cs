using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for clients and servers using the UDP protocol and a TSS state.
    /// </summary>
    public abstract class AbstractTssController<TSession>
        : AbstractController<TSession, IFrameCommand>, ISimulationController<TSession>
        where TSession : ISession
    {
        #region Types

        /// <summary>
        /// Used in abstract TSS server and client implementations.
        /// </summary>
        protected enum TssControllerMessage
        {
            /// <summary>
            /// Normal game command, handled in base class.
            /// </summary>
            Command,

            /// <summary>
            /// Server sends current frame to clients, used to synchronize
            /// run speeds of clients to server.
            /// </summary>
            Synchronize,

            /// <summary>
            /// Client requested the game state, e.g. because it could not
            /// roll back to a required state.
            /// </summary>
            GameStateRequest,

            /// <summary>
            /// Server sends game state to client in response to <c>GameStateRequest</c>.
            /// </summary>
            GameStateResponse,

            /// <summary>
            /// Server tells players about a new object to insert into the simulation.
            /// </summary>
            AddGameObject,

            /// <summary>
            /// Server tells players to remove an object from the simulation.
            /// </summary>
            RemoveGameObject,

            /// <summary>
            /// Compare the hash of the leading game state at a given frame. If
            /// the client fails the check, it'll have to request a new snapshot.
            /// </summary>
            HashCheck
        }

        #endregion

        #region Constants

        /// <summary>
        /// The number of milliseconds a single update should take.
        /// </summary>
        protected const double _targetElapsedMilliseconds = 1000.0 / 60.0;

        #endregion

        #region Properties

        /// <summary>
        /// The underlying simulation controlled by this controller.
        /// </summary>
        public ISimulation Simulation { get { return tss; } }

        #endregion

        #region Fields

        /// <summary>
        /// The underlying simulation used. Directly changing this is strongly
        /// discouraged, as it will lead to clients having to resynchronize
        /// themselves by getting a snapshot of the complete simulation.
        /// </summary>
        protected TSS tss;

        /// <summary>
        /// The remainder of time we did not update last frame, which we'll add to the
        /// elapsed time in the next frame update.
        /// </summary>
        private double _lastUpdateRemainder;

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize session and base classes.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractTssController(TSession session, uint[] delays)
            : base(session)
        {
            tss = new TSS(delays);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update the simulation. This determines how many steps to perform,
        /// based on the elapsed time.
        /// </summary>
        /// <param name="gameTime">the game time information for the current
        /// update.</param>
        /// <param name="timeCorrection">some value to add to the elapsed time as
        /// a correction factor. Used by clients to better sync to the server's
        /// game speed.</param>
        protected void UpdateSimulation(GameTime gameTime, double timeCorrection = 0)
        {
            // Already disposed. Thanks, XNA.
            if (tss == null)
            {
                return;
            }

            // Compensate for dynamic time step.
            double elapsed = gameTime.ElapsedGameTime.TotalMilliseconds + _lastUpdateRemainder + timeCorrection;
            if (elapsed < _targetElapsedMilliseconds)
            {
                // If we can't actually run to the next frame, at least update
                // back to the current frame in case rollbacks were made to
                // accommodate player commands.
                tss.RunToFrame(tss.CurrentFrame);
            }
            else
            {
                // We can run at least one frame, so do the update(s). Due to the
                // carry there may occur more than one simulation update per XNA
                // update, but that should be below the threshold of the noticeable.
                while (elapsed >= _targetElapsedMilliseconds)
                {
                    elapsed -= _targetElapsedMilliseconds;
                    tss.Update();
                }
                _lastUpdateRemainder = elapsed;
            }
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// Apply a command. This will apply the command to the frame it was issued in.
        /// </summary>
        /// <param name="command">The command to push.</param>
        protected virtual void Apply(IFrameCommand command)
        {
            tss.PushCommand(command, command.Frame);
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Got command data from another client or the server.
        /// </summary>
        /// <param name="command">the received command.</param>
        protected override void HandleRemoteCommand(IFrameCommand command)
        {
            Apply(command);
        }

        /// <summary>
        /// Prepends all normal command messages with the corresponding flag.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="packet">the final packet to send.</param>
        /// <returns>the given packet, after writing.</returns>
        protected override Packet WrapDataForSend(IFrameCommand command, Packet packet)
        {
            packet.Write((byte)TssControllerMessage.Command);
            return base.WrapDataForSend(command, packet);
        }

        #endregion
    }
}
