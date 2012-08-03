using System;
using System.Diagnostics;
using Engine.ComponentSystem;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;
using Engine.Util;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for clients and servers using the UDP protocol and a TSS state.
    /// </summary>
    public abstract class AbstractTssController<TSession>
        : AbstractController<TSession, FrameCommand>, ISimulationController<TSession>
        where TSession : ISession
    {
        #region Types

        /// <summary>
        /// Used in abstract TSS server and client implementations.
        /// </summary>
        internal enum TssControllerMessage
        {
            /// <summary>
            /// Normal simulation command, handled in base class.
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
            GameState,

            /// <summary>
            /// Server tells players to remove an object from the simulation.
            /// </summary>
            RemoveGameObject,

            /// <summary>
            /// Compare the hash of the leading game state at a given frame. If
            /// the client fails the check, it'll have to request a new snapshot.
            /// </summary>
            HashCheck,

            /// <summary>
            /// Client sent a dump of his game state because a hash check failed.
            /// </summary>
            GameStateDump
        }

        #endregion

        #region Constants

        /// <summary>
        /// The number of milliseconds a single update should take.
        /// </summary>
        protected const double TargetElapsedMilliseconds = 1000.0 / 60.0;

        /// <summary>
        /// The interval in ticks after which to send a hash check to the clients.
        /// </summary>
        protected const int HashInterval = (int)(10000 / TargetElapsedMilliseconds); // ~10s

        /// <summary>
        /// The actual load is multiplied with this factor to provide a little
        /// buffer, allowing server/clients to react to slow downs before the
        /// game becomes unresponsive.
        /// </summary>
        private const double LoadBufferFactor = 1.8;

        #endregion

        #region Properties

        /// <summary>
        /// The underlying simulation controlled by this controller.
        /// </summary>
        public ISimulation Simulation { get { return _simulation; } }

        /// <summary>
        /// The current 'load', i.e. how much of the available time is actually
        /// needed to perform an update.
        /// </summary>
        public override double CurrentLoad { get { return _updateLoad.Mean(); } }

        /// <summary>
        /// The current actual game speed, based on possible slow-downs due
        /// to the server or other clients.
        /// </summary>
        public double ActualSpeed { get { return AdjustedSpeed; } }

        /// <summary>
        /// Adjusted load value to allow reacting to slow downs of server or
        /// clients before the game becomes unresponsive.
        /// </summary>
        protected double SafeLoad { get { return CurrentLoad * LoadBufferFactor; } }

        #endregion

        #region Fields

        /// <summary>
        /// The underlying simulation used. Directly changing this is strongly
        /// discouraged, as it will lead to clients having to resynchronize
        /// themselves by getting a snapshot of the complete simulation.
        /// </summary>
        protected TSS Tss;

        /// <summary>
        /// The adjusted speed we're currently running at, based on how well
        /// other clients (and the server) currently fare.
        /// </summary>
        protected double AdjustedSpeed = 1.0;

        /// <summary>
        /// Wrapper to restrict interaction with TSS.
        /// </summary>
        private readonly ISimulation _simulation;

        /// <summary>
        /// The time we performed our last update.
        /// </summary>
        private DateTime _lastUpdate;

        /// <summary>
        /// The remainder of time we did not update last frame, which we'll add to the
        /// elapsed time in the next frame update.
        /// </summary>
        private double _lastUpdateRemainder;

        /// <summary>
        /// Remaining time to be compensated for as requested via frame skipping.
        /// </summary>
        private double _frameskipRemainder;

        /// <summary>
        /// Used to sample how long it takes for us to perform our simulation
        /// updates in relation to the available time.
        /// </summary>
        private readonly DoubleSampling _updateLoad = new DoubleSampling(30);

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize session and base classes.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="delays">The delays.</param>
        protected AbstractTssController(TSession session, uint[] delays)
            : base(session)
        {
            Tss = new TSS(delays);
            _simulation = new TssSimulationWrapper(this);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update the simulation. This determines how many steps to perform,
        /// based on the elapsed time.
        /// </summary>
        /// <param name="targetSpeed">The target speed.</param>
        protected void UpdateSimulation(double targetSpeed = 1.0)
        {
            // We can run at least one frame, so do the update(s).
            var begin = DateTime.Now;

            // Incorporate frame skip.
            var frameSkipRemainderHalf = _frameskipRemainder * 0.5;
            _lastUpdateRemainder += frameSkipRemainderHalf;
            _frameskipRemainder -= frameSkipRemainderHalf;

            // Time we spent updating. We don't want to take longer than
            // one update should take (i.e. targetTime), to avoid the game
            // getting slower and slower (because updates take longer and
            // longer -> more and more to catch up).
            double timePassed = 0;

            // Compensate for dynamic time step.
            var elapsed = (DateTime.Now - _lastUpdate).TotalMilliseconds + _lastUpdateRemainder;
            _lastUpdate = DateTime.Now;
            var targetFrequency = TargetElapsedMilliseconds / targetSpeed;
            if (elapsed < targetFrequency)
            {
                // If we can't actually run to the next frame, at least update
                // back to the current frame in case rollbacks were made to
                // accommodate player commands.
                Tss.RunToFrame(Tss.CurrentFrame);

                // Track how much time we spent in this update.
                timePassed = (DateTime.Now - begin).TotalMilliseconds;

                // Carry this to the next frame, for uncapped frame rates.
                _lastUpdateRemainder = elapsed;
            }
            else
            {
                // This is how much time we have to catch up for. The number
                // simulation frames this equates to is:
                //     remainingTime / targetTime.
                var remainingTime = elapsed;

                // Do as many updates as we can.
                while (remainingTime >= targetFrequency && timePassed < targetFrequency)
                {
                    Tss.Update();

                    PerformAdditionalUpdateActions();

                    // One less to do.
                    remainingTime -= targetFrequency;

                    // Track how much time we spent in this update.
                    timePassed = (DateTime.Now - begin).TotalMilliseconds;
                }

                // Keep a carry for the next update. But never try to catch up
                // on frames while we took too long, as this'll lead to the
                // simulation running too fast when catching up.
                _lastUpdateRemainder = System.Math.Min(remainingTime, targetFrequency);
            }

            // Track our load, but ignore idle rounds (this is for unlocked
            // frame rates, because we'd need a much larger sampling then,
            // otherwise).
            if (timePassed > 1 && timePassed < 128)
            {
                _updateLoad.Put(timePassed / TargetElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Callback that allows subclasses to perform additional logic that
        /// should be executed per actual simulation update.
        /// </summary>
        protected virtual void PerformAdditionalUpdateActions()
        {
        }

        /// <summary>
        /// Allows subclasses to request skipping frames / waiting for a
        /// specific number of frames.
        /// </summary>
        /// <param name="frames">The number of frames to skip, positive or negative.</param>
        protected void ScheduleFrameskip(long frames)
        {
            // Try to interpolate smoothly when the gap is not too large, otherwise
            // force it to the specified frame immediately.
            if (System.Math.Abs(frames) > 10)
            {
                Tss.RunToFrame(Tss.CurrentFrame + frames);
                _frameskipRemainder = 0;
            }
            else
            {
                _frameskipRemainder = frames * TargetElapsedMilliseconds;
            }
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// Apply a command. This will apply the command to the frame it was issued in.
        /// </summary>
        /// <param name="command">The command to push.</param>
        protected virtual void Apply(FrameCommand command)
        {
            Tss.PushCommand(command, command.Frame);
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Got command data from another client or the server.
        /// </summary>
        /// <param name="command">the received command.</param>
        protected override void HandleRemoteCommand(FrameCommand command)
        {
            Apply(command);
        }

        /// <summary>
        /// Prepends all normal command messages with the corresponding flag.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="packet">the final packet to send.</param>
        /// <returns>the given packet, after writing.</returns>
        protected override Packet WrapDataForSend(FrameCommand command, Packet packet)
        {
            packet.Write((byte)TssControllerMessage.Command);
            return base.WrapDataForSend(command, packet);
        }

        #endregion

        #region Simulation wrapper

        /// <summary>
        /// Wrapper for the encapsulated simulation, to minimize points in API
        /// that could corrupt the simulation.
        /// </summary>
        private sealed class TssSimulationWrapper : ISimulation
        {
            #region Properties

            /// <summary>
            /// The current frame of the simulation the state represents.
            /// </summary>
            public long CurrentFrame { get { return _controller.Tss.CurrentFrame; } }

            /// <summary>
            /// The component system manager in use in this simulation.
            /// </summary>
            public IManager Manager { get { return _controller.Tss.Manager; } }

            #endregion

            #region Fields
            
            /// <summary>
            /// The controller this managed wrapper belongs to.
            /// </summary>
            private readonly AbstractTssController<TSession> _controller;

            #endregion

            #region Constructor
            
            /// <summary>
            /// Creates a new wrapper for the specified controller.
            /// </summary>
            /// <param name="controller">The controller.</param>
            public TssSimulationWrapper(AbstractTssController<TSession> controller)
            {
                _controller = controller;
            }

            #endregion

            #region Logic

            /// <summary>
            /// Advance the simulation by one frame.
            /// </summary>
            public void Update()
            {
                _controller.Tss.Update();
            }

            /// <summary>
            /// Apply a given command to the simulation state.
            /// </summary>
            /// <param name="command">the command to apply.</param>
            public void PushCommand(Command command)
            {
                Debug.Assert(command is FrameCommand);
                _controller.Apply((FrameCommand)command);
            }

            #endregion

            #region Serialization / Hashing

            /// <summary>
            /// Write the object's state to the given packet.
            /// </summary>
            /// <param name="packet">The packet to write the data to.</param>
            /// <returns>The packet after writing.</returns>
            public Packet Packetize(Packet packet)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Bring the object to the state in the given packet.
            /// </summary>
            /// <param name="packet">The packet to read from.</param>
            public void Depacketize(Packet packet)
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Push some unique data of the object to the given hasher,
            /// to contribute to the generated hash.
            /// </summary>
            /// <param name="hasher">The hasher to push data to.</param>
            public void Hash(Hasher hasher)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region Copying
            
            /// <summary>
            /// Creates a deep copy of the object.
            /// </summary>
            /// <returns>The copy.</returns>
            public ISimulation NewInstance()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Creates a deep copy of the object, reusing the given object.
            /// </summary>
            /// <param name="into">The object to copy into.</param>
            /// <returns>The copy.</returns>
            public void CopyInto(ISimulation into)
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        #endregion
    }
}
