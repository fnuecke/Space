using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Engine.Math;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation.Commands;

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
        /// <remarks>
        /// This refers to the synchronization of game time, i.e. it ensures the server and
        /// client run at approximately the same speed / the times do not diverge.
        /// </remarks>
        private const int SyncInterval = 1000;

        #endregion

        #region Fields

        /// <summary>
        /// Next unique command ID to use. It's OK if this overflows, because any commands
        /// that old will no longer be relevant (it's unlikely this will happen in a single
        /// game, anyway).
        /// </summary>
        private int _nextCommandId = int.MinValue;

        /// <summary>
        /// Last time we sent a sync command to the server.
        /// </summary>
        private DateTime _lastSyncTime;

        /// <summary>
        /// Difference in current frame to server, as determined by the
        /// last few syncs. This is used to ignore outliers (extreme delays,
        /// caused e.g. by resent TCP packets).
        /// </summary>
        private readonly IntSampling _frameDiff = new IntSampling(5);

        /// <summary>
        /// The frame of the latest hash we got from the server (don't store
        /// hashes from before that).
        /// </summary>
        private long _lastServerHashedFrame = 1;

        /// <summary>
        /// Keeping track of hashes to compare to reference hashes from
        /// server (to detect desyncs).
        /// </summary>
        private readonly Dictionary<long, uint> _hashes = new Dictionary<long, uint>();

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize session and base classes.
        /// </summary>
        /// <param name="session">The session used.</param>
        protected AbstractTssClient(IClientSession session)
            : base(session, new[] {
                (uint)System.Math.Ceiling(50 / TargetElapsedMilliseconds), //< Expected case.
                (uint)System.Math.Ceiling(500 / TargetElapsedMilliseconds) //< High, to avoid full resyncs.
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
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        public override void Update(float elapsedMilliseconds)
        {
            // Skip updates if we're waiting for the game state or were disconnected.
            if (Tss.WaitingForSynchronization || Session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            // Drive game logic.
            UpdateSimulation(elapsedMilliseconds);

            // We might have disconnected due to a desync.
            if (Session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            // Send sync command every now and then, to keep game clock synchronized.
            // No need to sync for a server that runs in the same program, though.
            if (!Tss.WaitingForSynchronization && (DateTime.UtcNow - _lastSyncTime).TotalMilliseconds > SyncInterval)
            {
                PerformSync();
            }
        }

        /// <summary>
        /// Do hash checking if the frame is frame in which hash checking should be applied.
        /// </summary>
        protected override void PerformAdditionalUpdateActions()
        {
            // Hash test.
            if (Tss.TrailingFrame > 0 && (Tss.TrailingFrame % HashInterval) == 0)
            {
                // Generate hash.
                var hasher = new Hasher();
                hasher.Write(Tss.TrailingSimulation);
                PerformHashCheck(hasher.Value, Tss.TrailingFrame, Tss.TrailingFrame >= _lastServerHashedFrame);
            }
        }

        /// <summary>
        /// Dumps the state of all components.
        /// </summary>
        [Conditional("DEBUG")]
        private void SendGameStateDump()
        {
            // Get a (relatively) unique base name for the files.
            var dumpId = "desync_" + DateTime.UtcNow.Ticks;

            try
            {
                // Create actual game dump and write it to file.
                var clientDump = StringifyGameState(Tss.TrailingFrame, Tss.TrailingSimulation.Manager);
                File.WriteAllText(dumpId + "_client.txt", clientDump);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed writing client desynchronization dump.", ex);
                return;
            }

            // Send the dump to the server.
            using (var packet = new Packet())
            {
                packet
                    // Message type.
                    .Write((byte)TssControllerMessage.GameStateDump)
                    // Frame this dump applies to.
                    .Write(Tss.TrailingFrame)
                    // Write the dump id, to make it easier to match files.
                    .Write(dumpId);
                Session.Send(packet);
            }
        }

        /// <summary>
        /// Perform a hash check by hashing the local simulation and testing against
        /// the value of our server (or vice versa).
        /// </summary>
        /// <returns>Whether the hash was <b>stored</b> or not.</returns>
        private void PerformHashCheck(uint hashValue, long hashFrame, bool store)
        {
            // See if we have that frame, meaning we have to compare to it now.
            if (_hashes.ContainsKey(hashFrame))
            {
                // Got a hash, check it.
                if (hashValue != _hashes[hashFrame])
                {
                    Logger.Warn("Hash mismatch!");

                    SendGameStateDump();

                    Session.Leave();
                }
                _hashes.Remove(hashFrame);
            }
            else if (store)
            {
                // Otherwise store it, but only if it's newer than what we
                // got from the server.
                _hashes[hashFrame] = hashValue;
            }
        }
        
        /// <summary>
        /// Initialize a synchronization with the server by sending our current
        /// frame and load.
        /// </summary>
        private void PerformSync()
        {
            _lastSyncTime = DateTime.UtcNow;
            using (var packet = new Packet())
            {
                packet
                    .Write((byte)TssControllerMessage.Synchronize)
                    .Write(Tss.CurrentFrame)
                    .Write(SafeLoad);
                Session.Send(packet);
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
            // Fill in / override data to be correct.
            command.Id = _nextCommandId++;
            command.PlayerNumber = Session.LocalPlayer.Number;
            // Push the command back a bit to allow remote simulations a better
            // chance of receiving the command before they reach the frame it
            // applies to. This is used reduce the likelihood of rollbacks.
            command.Frame = Tss.CurrentFrame + 1 + (int)System.Math.Max(0, System.Math.Round(_frameDiff.Mean()));
            Apply(command);
        }

        /// <summary>
        /// Apply a command. If this command was generated locally, it'll be sent to the
        /// server. Otherwise we'll simply try to push it to the simulation.
        /// </summary>
        /// <param name="command">The command to apply.</param>
        protected override void Apply(FrameCommand command)
        {
            // If we're waiting for a sync, so we just skip any commands whatsoever
            // because the server will send them to us again after completing the
            // transfer of the game state. Also don't issue new commands ourselves
            // during that waiting period.
            if (Tss.WaitingForSynchronization)
            {
                return;
            }

            // As a client we only send commands that are our own AND have not been sent
            // back to us by the server, acknowledging our actions. I.e. only send our
            // own, tentative commands.
            if (!command.IsAuthoritative && command.PlayerNumber == Session.LocalPlayer.Number)
            {
                // Send command to host.
                Send(command);
            }

            // Apply the command to our simulation.
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
                {
                    // Normal command, forward it.
                    var command = base.UnwrapDataForReceive(e);

                    // Test if we got the message from the server, to mark the command accordingly.
                    command.IsAuthoritative = args.IsAuthoritative;

                    // Return the deserialized command.
                    return command;
                }
                case TssControllerMessage.Synchronize:
                {
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

                        if (System.Math.Abs(frameDelta) > 1 && frameDelta < (int)(median + stdDev))
                        {
                            Logger.Trace("Correcting for {0} frames.", frameDelta);

                            // Adjust the current frame of the simulation.
                            ScheduleFrameskip(frameDelta);
                        }
                    }
                    break;
                }
                case TssControllerMessage.HashCheck:
                {
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        // Get the frame this hash data is for.
                        var hashFrame = args.Data.ReadInt64();
                        Debug.Assert(hashFrame > _lastServerHashedFrame);
                        _lastServerHashedFrame = hashFrame;

                        // Read hash values.
                        var hashValue = args.Data.ReadUInt32();

                        // And perform hash check.
                        PerformHashCheck(hashValue, hashFrame, Tss.TrailingFrame < _lastServerHashedFrame);
                    }
                    break;
                }
                case TssControllerMessage.GameState:
                {
                    // Got a simulation snap shot (normally after requesting it due to
                    // our simulation going out of scope for an older event).
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        // Read data.
                        var serverHash = args.Data.ReadUInt32();
                        args.Data.ReadPacketizableInto(Tss);

                        // Validate the data we got.
                        var hasher = new Hasher();
                        hasher.Write(Tss.TrailingSimulation);
                        if (hasher.Value != serverHash)
                        {
                            Logger.Error("Hash mismatch after deserialization.");
                            Session.Leave();
                        }

                        // Run to current frame to avoid slow interpolation to current frame.
                        // Take into account the time we need to get there.
                        var delta = 0L;
                        do
                        {
                            // Remember when we started, see below.
                            var started = DateTime.UtcNow;

                            // Do the actual update, run to where we want to be.
                            Tss.RunToFrame(Tss.CurrentFrame + delta);

                            // See how long we took for this update, and compute the number
                            // of updates we'd otherwise have made in that time.
                            delta = (long)((DateTime.UtcNow - started).TotalMilliseconds / TargetElapsedMilliseconds);

                            // Continue until we're close enough with the *actual* current
                            // frame to what we want to be at.
                        } while (delta > 10);
                    }
                    break;
                }
                case TssControllerMessage.RemoveGameObject:
                {
                    // Only accept these when they come from the server.
                    if (args.IsAuthoritative)
                    {
                        var removeFrame = args.Data.ReadInt64();
                        var entityUid = args.Data.ReadInt32();
                        Tss.RemoveEntity(entityUid, removeFrame);
                    }
                    break;
                }
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
                packet.Write((byte)TssControllerMessage.GameState);
                Session.Send(packet);
            }
        }

        #endregion
    }
}
