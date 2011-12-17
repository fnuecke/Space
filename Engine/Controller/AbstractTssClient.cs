using System;
using Engine.Commands;
using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for TSS based multiplayer clients using a UDP connection.
    /// This takes care of synchronizing the game states between server and
    /// client, and getting the run speed synchronized as well.
    /// </summary>
    /// <typeparam name="TPlayerData">the tpye of the player data structure.</typeparam>
    /// <typeparam name="TPacketizerContext">the type of the packetizer context.</typeparam>
    public abstract class AbstractTssClient<TCommand>
        : AbstractTssController<IClientSession, TCommand>,
          IClientController<IClientSession, TCommand>
        where TCommand : IFrameCommand
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// The interval in milliseconds after which to send a synchronization request to the
        /// server. The lower the value the better the synchronization, but, obviously, also
        /// more network traffic.
        /// </summary>
        private const int SyncInterval = 1000;

        #endregion

        #region Fields

        /// <summary>
        /// Last time we sent a sync command to the server.
        /// </summary>
        private long _lastSyncTime = 0;

        /// <summary>
        /// Keep track of the average number of frames we had to sync.
        /// </summary>
        private DoubleSampling _syncDiff = new DoubleSampling(5);

        /// <summary>
        /// Difference in current frame to server, as determined by the
        /// last few syncs.
        /// </summary>
        private IntSampling _frameDiff = new IntSampling(5);

        /// <summary>
        /// The last frame we know the server's state hash of.
        /// </summary>
        private long _hashFrame = -1;

        /// <summary>
        /// The hash value of the server's state.
        /// </summary>
        private int _hashValue;

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize session and base classes.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractTssClient(Game game, IClientSession session)
            : base(game, session, new uint[] {
                (uint)System.Math.Ceiling(50 / game.TargetElapsedTime.TotalMilliseconds),
                (uint)System.Math.Ceiling(150 / game.TargetElapsedTime.TotalMilliseconds),
                (uint)System.Math.Ceiling(300 / game.TargetElapsedTime.TotalMilliseconds)
            })
        {
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            if (Session != null)
            {
                Session.JoinResponse += HandleJoinResponse;
            }

            if (Simulation != null)
            {
                Simulation.Invalidated += HandleSimulationInvalidated;
            }

            base.Initialize();
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (Session != null)
            {
                Session.JoinResponse -= HandleJoinResponse;
            }

            if (Simulation != null)
            {
                Simulation.Invalidated -= HandleSimulationInvalidated;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drives the game loop, right after driving the network protocol
        /// in the base class. Also part of synchronizing run speeds on
        /// server and client by sending sync requests in certain intervals.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (Session.ConnectionState == ClientState.Connected && !Simulation.WaitingForSynchronization)
            {
                // Drive game logic.
                UpdateSimulation(gameTime, _syncDiff.Mean());

                // Hash test.
                if (Simulation.TrailingFrame == _hashFrame)
                {
                    Hasher hasher = new Hasher();
                    Simulation.Hash(hasher);
                    if (hasher.Value != _hashValue)
                    {
                        logger.Debug("Hash mismatch: {0} != {1} ", _hashValue, hasher.Value);
                        Simulation.Invalidate();
                    }
                }

                // Send sync command every now and then, to keep game clock synchronized.
                // No need to sync for a server that runs in the same program, though.
                if (new TimeSpan(DateTime.Now.Ticks - _lastSyncTime).TotalMilliseconds > SyncInterval)
                {
                    _lastSyncTime = DateTime.Now.Ticks;
                    Session.Send(new Packet()
                        .Write((byte)TssControllerMessage.Synchronize)
                        .Write(Simulation.CurrentFrame));
                }
            }

            base.Update(gameTime);
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// Add this controller as a listener to the given emitter, handling
        /// whatever commands it produces.
        /// </summary>
        /// <param name="emitter">the emitter to attach to.</param>
        public void AddEmitter(ICommandEmitter<TCommand> emitter)
        {
            emitter.CommandEmitted += HandleEmittedCommand;
        }

        /// <summary>
        /// Remove this controller as a listener from the given emitter.
        /// </summary>
        /// <param name="emitter">the emitter to detach from.</param>
        public void RemoveEmitter(ICommandEmitter<TCommand> emitter)
        {
            emitter.CommandEmitted -= HandleEmittedCommand;
        }

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        protected override void Apply(IFrameCommand command)
        {
            // As a client we only send commands that are our own AND have not been sent
            // back to us by the server, acknowledging our actions. I.e. only send our
            // own, tentative commands.
            if (!command.IsAuthoritative && command.Player.Equals(Session.LocalPlayer))
            {
                // If we're waiting for a snapshot, don't continue spamming commands for
                // the very frame we're stuck in.
                if (Simulation.WaitingForSynchronization)
                {
                    return;
                }

                // Send command to host.
                Send(command);
            }
            else if (Simulation.WaitingForSynchronization && command.Frame <= Simulation.TrailingFrame)
            {
                // We're waiting for a sync, and our trailing frame wasn't enough, so
                // we just skip any commands whatsoever that are from before it.
                return;
            }
            base.Apply(command);
        }

        #endregion

        #region Events

        /// <summary>
        /// A server sent us a response to our request to join his game.
        /// </summary>
        /// <param name="sender">the underlying session.</param>
        /// <param name="e">information of the type <c>JoinResponseEventArgs</c>.</param>
        protected abstract void HandleJoinResponse(object sender, EventArgs e);

        /// <summary>
        /// A command emitter we're attached to has generated a new event.
        /// Override this to fill in some default values in the command
        /// before it is passed on to <c>HandleLocalCommand</c>.
        /// </summary>
        private void HandleEmittedCommand(TCommand command)
        {
            command.Player = Session.LocalPlayer;
            command.Frame = Simulation.CurrentFrame + 1;
            HandleLocalCommand(command);
        }

        /// <summary>
        /// Implement in subclasses to handle commands generated locally.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleLocalCommand(TCommand command);

        #endregion

        #region Protocol layer

        /// <summary>
        /// Takes care of client side TSS synchronization logic.
        /// </summary>
        protected override IFrameCommand UnwrapDataForReceive(SessionDataEventArgs e)
        {
            var args = (ClientDataEventArgs)e;
            var type = (TssControllerMessage)args.Data.ReadByte();
            switch (type)
            {
                case TssControllerMessage.Command:
                    // Normal command, forward it.
                    var command = base.UnwrapDataForReceive(e);
                    // Test if we got the message from the server, to mark the command accordingly.
                    command.IsAuthoritative = args.IsAuthoritative;
                    return command;

                case TssControllerMessage.Synchronize:
                    // Answer to a synchronization request.
                    // Only accept these when they come from the server, and disregard if
                    // we're waiting for a snapshot of the simulation.
                    if (args.IsAuthoritative && !Simulation.WaitingForSynchronization)
                    {
                        // This calculation follows algorithm described here:
                        // http://www.mine-control.com/zack/timesync/timesync.html
                        long sentFrame = args.Data.ReadInt64();
                        long serverFrame = args.Data.ReadInt64();

                        long latency = (Simulation.CurrentFrame - sentFrame) / 2;
                        long clientServerDelta = (serverFrame - Simulation.CurrentFrame);
                        long frameDelta = clientServerDelta + latency / 2;

                        _frameDiff.Put((int)frameDelta);
                        int median = _frameDiff.Median();
                        double stdDev = _frameDiff.StandardDeviation();

                        if (System.Math.Abs(frameDelta) > 1 && frameDelta < (int)(median + stdDev))
                        {
                            logger.Debug("Correcting for {0} frames.", frameDelta);
                            // Adjust the current frame of the simulation.
                            Simulation.RunToFrame(Simulation.CurrentFrame + frameDelta);
                        }
                        // Push our average delay plus the delta! Otherwise we'd loose the
                        // running ('constant') delta we accumulated.
                        _syncDiff.Put(frameDelta * Game.TargetElapsedTime.TotalMilliseconds / SyncInterval);
                    }
                    break;

                case TssControllerMessage.HashCheck:
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        _hashFrame = args.Data.ReadInt64();
                        _hashValue = args.Data.ReadInt32();
                    }
                    break;

                case TssControllerMessage.GameStateResponse:
                    // Got a simulation snap shot (normally after requesting it due to
                    // our simulation going out of scope for an older event).
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        Simulation.Depacketize(args.Data);
                    }
                    break;

                case TssControllerMessage.AddGameObject:
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        long addFrame = args.Data.ReadInt64();
                        IEntity entity = Packetizer.Depacketize<IEntity>(args.Data);
                        Simulation.AddEntity(entity, addFrame);
                    }
                    break;

                case TssControllerMessage.RemoveGameObject:
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        long removeFrame = args.Data.ReadInt64();
                        long entityUid = args.Data.ReadInt64();
                        Simulation.RemoveEntity(entityUid, removeFrame);
                    }
                    break;

                // Everything else is unhandled on the client.
                default:
                    break;
            }
            return null;
        }

        #endregion

        #region Events handled internally

        /// <summary>
        /// Called when our simulation cannot accommodate an update or rollback,
        /// meaning we have to get a server snapshot.
        /// </summary>
        private void HandleSimulationInvalidated(object sender, EventArgs e)
        {
            // So we request it.
            logger.Debug("Simulation invalidated, requesting server state.");
            Session.Send(new Packet().Write((byte)TssControllerMessage.GameStateRequest));
        }

        #endregion
    }
}
