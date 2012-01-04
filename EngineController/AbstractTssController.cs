using System;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;
using Engine.Util;
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
        internal enum TssControllerMessage
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
        public ISimulation Simulation { get { return _tss; } }

        /// <summary>
        /// The actual current game speed, which may be influenced by clients
        /// not being able to keep up with the computations needed, but which
        /// will be at maximum the <c>TargetSpeed</c>.
        /// </summary>
        public override double CurrentSpeed { get { return _updateTimes.Mean(); } }

        #endregion

        #region Fields

        /// <summary>
        /// The underlying simulation used. Directly changing this is strongly
        /// discouraged, as it will lead to clients having to resynchronize
        /// themselves by getting a snapshot of the complete simulation.
        /// </summary>
        protected TSS _tss;

        /// <summary>
        /// The remainder of time we did not update last frame, which we'll add to the
        /// elapsed time in the next frame update.
        /// </summary>
        private double _lastUpdateRemainder;

        /// <summary>
        /// Used to sample how long it takes for us to perform our simulation
        /// updates. Used to determine the current simulation speed.
        /// </summary>
        private DoubleSampling _updateTimes = new DoubleSampling(30);

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
            _tss = new TSS(delays);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update the simulation. This determines how many steps to perform,
        /// based on the elapsed time.
        /// </summary>
        /// <param name="gameTime">the game time information for the current
        /// update.</param>
        protected void UpdateSimulation(GameTime gameTime, double targetSpeed = 1)
        {
            // Compensate for dynamic time step.
            double elapsed = gameTime.ElapsedGameTime.TotalMilliseconds + _lastUpdateRemainder;
            double targetTime = _targetElapsedMilliseconds * targetSpeed;
            if (elapsed < targetTime)
            {
                // If we can't actually run to the next frame, at least update
                // back to the current frame in case rollbacks were made to
                // accommodate player commands.
                _tss.RunToFrame(_tss.CurrentFrame);

                // Carry this to the next frame, for uncapped frame rates.
                _lastUpdateRemainder = elapsed;
            }
            else
            {
                // We can run at least one frame, so do the update(s).
                long begin = DateTime.Now.Ticks;

                // This is how much time we have to catch up for. The number
                // simulation frames this equates to is:
                //     remainingTime / targetTime.
                double remainingTime = elapsed;

                // Time we spent updating. We don't want to take longer than
                // one update should take (i.e. targetTime), to avoid the game
                // getting slower and slower (because updates take longer and
                // longer -> more and more to catch up).
                double timePassed = 0;

                // Do as many updates as we can.
                while (remainingTime >= targetTime && timePassed < targetTime)
                {
                    _tss.Update();

                    // One less to do.
                    remainingTime -= targetTime;

                    // Track how much time we spent in this update.
                    timePassed = new TimeSpan(DateTime.Now.Ticks - begin).TotalMilliseconds;
                }

                // If we had to skip updates because we're running slowly,
                // compute the rate at which we're operating and push it to
                // our sampling.
                if (remainingTime >= targetTime)
                {
                    _updateTimes.Put((elapsed - remainingTime) / elapsed);

                    // Make sure we run at least one update next frame, but
                    // don't try to "catch up" to additional frames, as this
                    // a) results in off numbers for our current speed.
                    // b) will make the simulation run ridiculously fast once
                    //    we recover and get to a point where we *could* catch
                    //    up. We don't want that.
                    _lastUpdateRemainder = targetTime;
                }
                else
                {
                    // Otherwise we're on time, so we're running at full speed.
                    _updateTimes.Put(1);

                    // Remember how much we have to catch up next update.
                    _lastUpdateRemainder = remainingTime;
                }
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
            _tss.PushCommand(command, command.Frame);
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
