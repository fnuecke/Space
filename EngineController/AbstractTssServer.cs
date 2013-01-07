using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Engine.ComponentSystem;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation.Commands;

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

        /// <summary>
        /// Some game state dumps from the past we keep to compare them to any
        /// we receive from clients due to hash check failure.
        /// </summary>
        private readonly Dictionary<long, IManager> _gameStates = new Dictionary<long, IManager>();

        #endregion

        #region Constructor

        /// <summary>
        /// Base constructor, creates simulation. You'll need to initialize it
        /// by calling its <c>Initialize()</c> method yourself.
        /// </summary>
        /// <param name="session">The session.</param>
        protected AbstractTssServer(IServerSession session)
            : base(session, session.MaxPlayers > 1 ? new[] {
                (uint)System.Math.Ceiling(50 / TargetElapsedMilliseconds), //< Expected case.
                (uint)System.Math.Ceiling(250 / TargetElapsedMilliseconds) //< To avoid discrimination of laggy connections.
            } : new uint[0]) //< If it's single player only we don't need trailing states.
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
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        public override void Update(float elapsedMilliseconds)
        {
            // Drive game logic.
            UpdateSimulation(elapsedMilliseconds);

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
            if (Session.MaxPlayers > 1 && Tss.TrailingFrame > _lastHashedFrame && ((Tss.TrailingFrame % HashInterval) == 0))
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
            IManager copy;
            if (_gameStates.ContainsKey(Tss.TrailingFrame - HashInterval * 2))
            {
                copy = _gameStates[Tss.TrailingFrame - HashInterval * 2];
                _gameStates.Remove(Tss.TrailingFrame - HashInterval * 2);
            }
            else
            {
                copy = new Manager();
            }
            Tss.TrailingSimulation.Manager.CopyInto(copy);
            _gameStates.Add(Tss.TrailingFrame, copy);
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
            hasher.Write(Tss.TrailingSimulation);

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
                AdjustedSpeed = TargetSpeed / worstLoad;
            }
            else
            {
                AdjustedSpeed = TargetSpeed;
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
                                .Write(AdjustedSpeed);
                            Session.SendTo(args.Player, packet);
                        }
                        break;
                    }

                case TssControllerMessage.GameState:
                    {
                        // Client needs game state.
                        var hasher = new Hasher();
                        hasher.Write(Tss.TrailingSimulation);
                        using (var packet = new Packet())
                        {
                            packet
                                // Message type.
                                .Write((byte)TssControllerMessage.GameState)
                                // Hash value for validation.
                                .Write(hasher.Value)
                                // Actual game state, including the TSS wrapper.
                                .Write(Tss);
                            Session.SendTo(args.Player, packet);
                        }
                        break;
                    }

                case TssControllerMessage.GameStateDump:
                    {
                        // Got a game state dump from a client due to hash check failure.
                        var frame = args.Data.ReadInt64();
                        if (!_gameStates.ContainsKey(frame))
                        {
                            Logger.Warn("Got a game state dump for a frame we don't have the local dump for anymore.");
                            return null;
                        }

                        // Get the two correlating dumps.
                        var clientDump = args.Data.ReadString();
                        var serverDump = StringifyGameState(frame, _gameStates[frame]);

                        // Get a (relatively) unique base name for the files.
                        var dumpId = "desync_" + DateTime.UtcNow.Ticks;

                        // Write the dumps.
                        try
                        {
                            File.WriteAllText(dumpId + "_client.txt", clientDump);
                            File.WriteAllText(dumpId + "_server.txt", serverDump);
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorException("Failed writing desynchronization dumps.", ex);
                        }

                        break;
                    }
            }
            return null;
        }

        static string StringifyGameState(long frame, IManager manager)
        {
            // String builder we use to concatenate our strings.
            var sb = new StringBuilder();

            // Get some general system information, for reference.
            var assembly = Assembly.GetEntryAssembly().GetName();
#if DEBUG
            const string build = "Debug";
#else
            const string build = "Release";
#endif
            sb.Append("--------------------------------------------------------------------------------\n");
            sb.AppendFormat("{0} {1} (Attached debugger: {2}) running under {3}\n",
                            assembly.Name, build, Debugger.IsAttached, Environment.OSVersion.VersionString);
            sb.AppendFormat("Build Version: {0}\n", assembly.Version);
            sb.AppendFormat("CLR Version: {0}\n", Environment.Version);
            sb.AppendFormat("CPU Count: {0}\n", Environment.ProcessorCount);
            sb.AppendFormat("Assigned RAM: {0:0.0}MB\n", Environment.WorkingSet / 1024.0 / 1024.0);
            sb.Append("Controller Type: Server\n");
            sb.Append("--------------------------------------------------------------------------------\n");
            sb.AppendFormat("Gamestate at frame {0}\n", frame);
            sb.Append("--------------------------------------------------------------------------------\n");

            // Dump actual game state.
            foreach (var system in manager.Systems)
            {
                sb.Append(system);
                sb.AppendLine();
            }
            foreach (var component in manager.Components)
            {
                sb.Append(component);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion
    }
}
