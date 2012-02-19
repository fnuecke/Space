using System;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for TSS based multiplayer clients using a UDP connection.
    /// This takes care of synchronizing the game states between server and
    /// client, and getting the run speed synchronized as well.
    /// </summary>
    public abstract class AbstractTssClient
        : AbstractTssController<IClientSession>, IClientController<FrameCommand>
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// The interval in milliseconds after which to send a synchronization request to the
        /// server. The lower the value the better the synchronization, but, obviously, also
        /// more network traffic.
        /// </summary>
        private const int SyncInterval = 500;

        #endregion

        #region Fields

        /// <summary>
        /// Last time we sent a sync command to the server.
        /// </summary>
        private long _lastSyncTime;

        /// <summary>
        /// Difference in current frame to server, as determined by the
        /// last few syncs. This is used to ignore outliers (extreme delays,
        /// caused e.g. by resent TCP packets).
        /// </summary>
        private readonly IntSampling _frameDiff = new IntSampling(5);

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
        /// <param name="session">The session used.</param>
        protected AbstractTssClient(IClientSession session)
            : base(session, new[] {
                (uint)Math.Ceiling(50 / TargetElapsedMilliseconds), //< Expected case.
                (uint)Math.Ceiling(500 / TargetElapsedMilliseconds) //< High, to avoid resyncs.
            })
        {
            Session.JoinResponse += HandleJoinResponse;
            Tss.Invalidated += HandleSimulationInvalidated;
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Session.JoinResponse -= HandleJoinResponse;
                Tss.Invalidated -= HandleSimulationInvalidated;
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
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        public override void Update(GameTime gameTime)
        {
            if (Session.ConnectionState == ClientState.Connected && !Tss.WaitingForSynchronization)
            {
                // Drive game logic.
                UpdateSimulation(gameTime, AdjustedSpeed);

                // Hash test.
                if (Tss.TrailingFrame == _hashFrame)
                {
                    var hasher = new Hasher();
                    Tss.Hash(hasher);
                    if (hasher.Value != _hashValue)
                    {
                        Logger.Debug("Hash mismatch: {0} != {1} ", _hashValue, hasher.Value);
                        Tss.Invalidate();
                    }
                }

                // Send sync command every now and then, to keep game clock synchronized.
                // No need to sync for a server that runs in the same program, though.
                if (new TimeSpan(DateTime.Now.Ticks - _lastSyncTime).TotalMilliseconds > SyncInterval)
                {
                    _lastSyncTime = DateTime.Now.Ticks;
                    using (var packet = new Packet())
                    {
                        Session.Send(packet
                            .Write((byte)TssControllerMessage.Synchronize)
                            .Write(Tss.CurrentFrame)
                            .Write((float)SafeLoad));
                    }
                }
            }
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// A command emitter we're attached to has generated a new event.
        /// Override this to fill in some default values in the command
        /// before it is passed on to <c>HandleLocalCommand</c>.
        /// </summary>
        public void PushLocalCommand(FrameCommand command)
        {
            command.PlayerNumber = Session.LocalPlayer.Number;
            command.Frame = Tss.CurrentFrame + 1;
            Apply(command);
        }

        /// <summary>
        /// Apply a command. If this command was generated locally, it'll be sent to the
        /// server. Otherwise we'll simply try to push it to the simulation.
        /// </summary>
        /// <param name="command">The command to apply.</param>
        protected override void Apply(FrameCommand command)
        {
            // As a client we only send commands that are our own AND have not been sent
            // back to us by the server, acknowledging our actions. I.e. only send our
            // own, tentative commands.
            if (!command.IsAuthoritative && command.PlayerNumber == Session.LocalPlayer.Number)
            {
                // If we're waiting for a snapshot, don't continue spamming commands for
                // the very frame we're stuck in.
                if (Tss.WaitingForSynchronization)
                {
                    return;
                }

                // Send command to host.
                Send(command);
            }
            else if (Tss.WaitingForSynchronization && command.Frame <= Tss.TrailingFrame)
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
        private void HandleJoinResponse(object sender, JoinResponseEventArgs e)
        {
            // OK, we were allowed to join, invalidate our simulation to request
            // the current state.
            Tss.Invalidate();
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Takes care of client side TSS synchronization logic.
        /// </summary>
        protected override FrameCommand UnwrapDataForReceive(SessionDataEventArgs e)
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
                    if (args.IsAuthoritative && !Tss.WaitingForSynchronization)
                    {
                        // This calculation follows algorithm described here:
                        // http://www.mine-control.com/zack/timesync/timesync.html
                        var sentFrame = args.Data.ReadInt64();
                        var serverFrame = args.Data.ReadInt64();

                        // We also adjust the game speed to accommodate slow
                        // machines. That's the speed we get in this step.
                        AdjustedSpeed = args.Data.ReadSingle();

                        var latency = (Tss.CurrentFrame - sentFrame) / 2;
                        var clientServerDelta = (serverFrame - Tss.CurrentFrame);
                        var frameDelta = clientServerDelta + latency / 2;

                        _frameDiff.Put((int)frameDelta);
                        var median = _frameDiff.Median();
                        var stdDev = _frameDiff.StandardDeviation();

                        if (Math.Abs(frameDelta) > 1 && frameDelta < (int)(median + stdDev))
                        {
                            Logger.Debug("Correcting for {0} frames.", frameDelta);
                            // Adjust the current frame of the simulation.
                            Tss.RunToFrame(new GameTime(), Tss.CurrentFrame + frameDelta);
                        }
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
                        var serverHash = args.Data.ReadInt32();
                        args.Data.ReadPacketizableInto(Tss);
                        var hasher = new Hasher();
                        Tss.Hash(hasher);
                        if (hasher.Value != serverHash)
                        {
                            Logger.Error("Hash mismatch after deserialization.");
                            Session.Leave();
                        }
                    }
                    break;

                case TssControllerMessage.RemoveGameObject:
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        var removeFrame = args.Data.ReadInt64();
                        var entityUid = args.Data.ReadInt32();
                        Tss.RemoveEntity(entityUid, removeFrame);
                    }
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
            Logger.Debug("Simulation invalidated, requesting server state.");
            using (var packet = new Packet())
            {
                Session.Send(packet.Write((byte)TssControllerMessage.GameStateRequest));
            }
        }

        #endregion
    }
}
