using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Engine.ComponentSystem;
using Engine.Math;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;

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
        protected const float TargetElapsedMilliseconds = 1000f / 20f;

        /// <summary>
        /// The interval in ticks after which to send a hash check to the clients.
        /// </summary>
        protected const int HashInterval = (int)(10000f / TargetElapsedMilliseconds); // ~10s

        /// <summary>
        /// The actual load is multiplied with this factor to provide a little
        /// buffer, allowing server/clients to react to slow downs before the
        /// game becomes unresponsive.
        /// </summary>
        private const float LoadBufferFactor = 1.75f;

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
        public override float CurrentLoad { get { return (float)_updateLoad.Mean(); } }

        /// <summary>
        /// The target game speed we try to run at, if possible.
        /// </summary>
        public float TargetSpeed { get; set; }

        /// <summary>
        /// The current actual game speed, based on possible slow-downs due
        /// to the server or other clients.
        /// </summary>
        public float ActualSpeed { get { return AdjustedSpeed; } }

        /// <summary>
        /// Adjusted load value to allow reacting to slow downs of server or
        /// clients before the game becomes unresponsive.
        /// </summary>
        protected float SafeLoad { get { return CurrentLoad * LoadBufferFactor; } }

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
        protected float AdjustedSpeed;

        /// <summary>
        /// Wrapper to restrict interaction with TSS.
        /// </summary>
        private readonly ISimulation _simulation;

        /// <summary>
        /// Keeps track of time elapsed inside one update run.
        /// </summary>
        private readonly Stopwatch _updateElapsed = new Stopwatch();

        /// <summary>
        /// The remainder of time we did not update last frame, which we'll add to the
        /// elapsed time in the next frame update.
        /// </summary>
        private float _lastUpdateRemainder;

        /// <summary>
        /// Remaining time to be compensated for as requested via frame skipping.
        /// </summary>
        private float _frameskipRemainder;

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
            TargetSpeed = AdjustedSpeed = 1.0f;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update the simulation. This determines how many steps to perform,
        /// based on the elapsed time.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        protected void UpdateSimulation(float elapsedMilliseconds)
        {
            // We can run at least one frame, so do the update(s).
            _updateElapsed.Restart();

            // Incorporate frame skip.
            var frameSkipRemainderHalf = _frameskipRemainder * 0.5f;
            _lastUpdateRemainder += frameSkipRemainderHalf;
            _frameskipRemainder -= frameSkipRemainderHalf;

            // Time we spent updating. We don't want to take longer than
            // one update should take (i.e. targetTime), to avoid the game
            // getting slower and slower (because updates take longer and
            // longer -> more and more to catch up).
            float timePassed = 0;

            // Compensate for dynamic time step.
            var elapsed = elapsedMilliseconds + _lastUpdateRemainder;
            var targetFrequency = TargetElapsedMilliseconds / AdjustedSpeed;
            if (elapsed < targetFrequency)
            {
                // If we can't actually run to the next frame, at least update
                // back to the current frame in case rollbacks were made to
                // accommodate player commands.
                Tss.RunToFrame(Tss.CurrentFrame);

                // Track how much time we spent in this update.
                timePassed = (float)_updateElapsed.Elapsed.TotalMilliseconds;

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
                    timePassed = (float)_updateElapsed.Elapsed.TotalMilliseconds;
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
        protected override IWritablePacket WrapDataForSend(FrameCommand command, IWritablePacket packet)
        {
            return base.WrapDataForSend(command, packet.Write((byte)TssControllerMessage.Command));
        }

        #endregion

        #region Utility
        
        protected static void WriteGameState(long frame, IManager manager, string filename)
        {
            // Get some general system information, for reference.
            var assembly = Assembly.GetEntryAssembly().GetName();
#if DEBUG
            const string build = "Debug";
#else
            const string build = "Release";
#endif

            using (var w = new StreamWriter(filename))
            {
                w.Write("--------------------------------------------------------------------------------\n");
                w.Write("{0} {1} (Attached debugger: {2}) running under {3}\n",
                        assembly.Name, build, Debugger.IsAttached, Environment.OSVersion.VersionString);
                w.Write("Build Version: {0}\n", assembly.Version);
                w.Write("CLR Version: {0}\n", Environment.Version);
                w.Write("CPU Count: {0}\n", Environment.ProcessorCount);
                w.Write("Assigned RAM: {0:0.0}MB\n", Environment.WorkingSet / 1024.0 / 1024.0);
                w.Write("Controller Type: Server\n");
                w.Write("--------------------------------------------------------------------------------\n");
                w.Write("Gamestate at frame {0}\n", frame);
                w.Write("--------------------------------------------------------------------------------\n");

                // Dump actual game state.
                w.Write("Manager = "); w.Dump(manager);
            }
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
            [PacketizerIgnore]
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
                throw new NotSupportedException();
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

            #region Serialization

            [OnStringify]
            public StreamWriter Dump(StreamWriter w, int indent)
            {
                w.Write("TrailingState = "); w.Dump(_controller.Tss.TrailingSimulation, indent);
                return w;
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
