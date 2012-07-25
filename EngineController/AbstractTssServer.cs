using System;
using System.Diagnostics;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation.Commands;
using Engine.Util;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for TSS based multiplayer servers using a UDP connection.
    /// This takes care of synchronizing the game states between server and
    /// client, and getting the run speed synchronized as well.
    /// </summary>
    public abstract class AbstractTssServer : AbstractTssController<IServerSession>
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly NLog.Logger GameStateLogger = NLog.LogManager.GetLogger("GameStateDump");

        #endregion

        #region Constants

        /// <summary>
        /// The number of frames a command may be ahead of the server's current frame
        /// to still allow it to be applied. Anything further ahead is considered
        /// invalid/cheating.
        /// </summary>
        private const long MaxCommandLead = 50;

        #endregion

        #region Fields

        /// <summary>
        /// Keeping track of how stressed each client is. This is used to figure
        /// out the "weakest link" to adjust the game speed accordingly.
        /// </summary>
        private readonly float[] _clientLoads;

        /// <summary>
        /// Trailing frame we last did a hash check in, to avoid doing them twice.
        /// </summary>
        private long _lastHashedFrame;

        #endregion

        #region Constructor

        /// <summary>
        /// Base constructor, creates simulation. You'll need to initialize it
        /// by calling its <c>Initialize()</c> method yourself.
        /// </summary>
        /// <param name="session">The session.</param>
        protected AbstractTssServer(IServerSession session)
            : base(session, new[] {
                (uint)Math.Ceiling(50 / TargetElapsedMilliseconds), //< Expected case.
                (uint)Math.Ceiling(250 / TargetElapsedMilliseconds) //< To avoid discrimination of laggy connections.
            })
        {
            _clientLoads = new float[Session.MaxPlayers];

            // Update the load info (and game speed) upon player departures.
            Session.PlayerLeft += HandlePlayerLeft;
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Session.PlayerLeft -= HandlePlayerLeft;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drives the game loop, right after driving the network protocol
        /// in the base class.
        /// </summary>
        public override void Update()
        {
            // Drive game logic.
            UpdateSimulation(AdjustedSpeed);

            // The server might have been the slowing factor, so try updating
            // the game speed. This is relatively cheap, so we can just do this
            // each update (no need to check if we're actually the slowest).
            AdjustSpeed();
        }

        /// <summary>
        /// Do hash checking if the frame is one in which hashing should be performed.
        /// </summary>
        protected override void PerformAdditionalUpdateActions()
        {
            // Send hash check every now and then, to check for loss of synchronization.
            // We want to use the trailing frame for this because at this point it's
            // guaranteed not to change anymore (from incoming commands -- they will be
            // discarded now).
            if (Tss.TrailingFrame > _lastHashedFrame && ((Tss.TrailingFrame % HashInterval) == 0))
            {
                DumpGameState();
                PerformHashCheck();
            }
        }

        /// <summary>
        /// Dumps the state of all components.
        /// </summary>
        [Conditional("DEBUG")]
        private void DumpGameState()
        {
            GameStateLogger.Info("--------------------------------------------------------------------------------");
            GameStateLogger.Info("Gamestate at frame {0}:", Tss.TrailingFrame);
            GameStateLogger.Info("--------------------------------------------------------------------------------");
            foreach (var system in Tss.TrailingSimulation.Manager.Systems)
            {
                GameStateLogger.Trace(system.ToString);
            }
            foreach (var component in Tss.TrailingSimulation.Manager.Components)
            {
                GameStateLogger.Trace(component.ToString);
            }
        }

        /// <summary>
        /// Perform a hash check by hashing the local simulation and sending the values
        /// to our clients so they can compare it to the hash of their simulation at
        /// that frame.
        /// </summary>
        private void PerformHashCheck()
        {
            // Update last checked frame.
            _lastHashedFrame = Tss.TrailingFrame;

            // Generate hash.
            var hasher = new Hasher();
            Tss.TrailingSimulation.Hash(hasher);

            // Send message.
            using (var packet = new Packet())
            {
                packet
                    .Write((byte)TssControllerMessage.HashCheck)
                    .Write(Tss.TrailingFrame)
                    .Write(hasher.Value);
                Session.Send(packet);
            }
        }

        /// <summary>
        /// Adjust the game speed by finding the slowest participant (the one
        /// with the highest load), and adjusting the speed so that he will
        /// not fall behind.
        /// </summary>
        private void AdjustSpeed()
        {
            // Find the participant with the worst update load.
            var worstLoad = SafeLoad;
            for (var i = 0; i < _clientLoads.Length; i++)
            {
                if (_clientLoads[i] > worstLoad)
                {
                    worstLoad = _clientLoads[i];
                }
            }

            // Adjust speed to the worst load.
            if (worstLoad > 1f)
            {
                AdjustedSpeed = 1f / worstLoad;
            }
            else
            {
                AdjustedSpeed = 1;
            }
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        protected override void Apply(FrameCommand command)
        {
            if (command.Frame < Tss.TrailingFrame)
            {
                Logger.Trace("Client command too old: {0} < {1}. Ignoring.", command.Frame, Tss.TrailingFrame);
            }
            else if (command.Frame > Tss.CurrentFrame + MaxCommandLead)
            {
                Logger.Trace("Client command too far into the future: {0} > {1}. Ignoring.", command.Frame, Tss.CurrentFrame + MaxCommandLead);
            }
            else
            {
                // All commands we apply are authoritative.
                command.IsAuthoritative = true;
                base.Apply(command);

                // As a server we resend all commands.
                Send(command);
            }
        }

        #endregion

        #region Event handling

        /// <summary>
        /// Reset speed and load information for a client when he leaves.
        /// </summary>
        private void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;

            // Set that load to zero, to not affect calculations.
            _clientLoads[args.Player.Number] = 0f;

            // Player might have been the one slowing us down.
            AdjustSpeed();
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Takes care of server side TSS synchronization logic.
        /// </summary>
        protected override FrameCommand UnwrapDataForReceive(SessionDataEventArgs e)
        {
            var args = (ServerDataEventArgs)e;
            var type = (TssControllerMessage)args.Data.ReadByte();
            switch (type)
            {
                case TssControllerMessage.Command:
                {
                    // Normal command, forward it if it's valid.
                    var command = base.UnwrapDataForReceive(e);

                    // Validate player number (avoid command injection for
                    // other players).
                    if (command.PlayerNumber == args.Player.Number)
                    {
                        // All green.
                        return command;
                    }

                    Logger.Warn("Received invalid packet (player number mismatch).");

                    // Disconnect the player, as this might have been a
                    // hacking attempt.
                    Session.Disconnect(Session.GetPlayer(args.Player.Number));

                    // Ignore the command.
                    return null;
                }
                case TssControllerMessage.Synchronize:
                {
                    // Client re-synchronizing.

                    // Get the frame the client is at.
                    var clientFrame = args.Data.ReadInt64();

                    // Get performance information of the client.
                    _clientLoads[args.Player.Number] = args.Data.ReadSingle();

                    // Re-evaluate at what speed we want to run.
                    AdjustSpeed();

                    // Send our reply.
                    using (var packet = new Packet())
                    {
                        packet
                            // Message type.
                            .Write((byte)TssControllerMessage.Synchronize)
                            // For reference, the frame the client sent this message.
                            .Write(clientFrame)
                            // The current server frame, to allow the client to compute
                            // the round trip time.
                            .Write(Tss.CurrentFrame)
                            // The current speed the game should run at.
                            .Write((float)AdjustedSpeed);
                        Session.SendTo(args.Player, packet);
                    }
                    break;
                }

                case TssControllerMessage.GameState:
                {
                    // Client needs game state.
                    var hasher = new Hasher();
                    Tss.TrailingSimulation.Hash(hasher);
                    using (var packet = new Packet())
                    {
                        Session.SendTo(args.Player, packet
                            .Write((byte)TssControllerMessage.GameState)
                            .Write(hasher.Value)
                            .Write(Tss));
                    }
                    break;
                }
            }
            return null;
        }

        #endregion
    }
}
