using System;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;

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

        #region Fields

        /// <summary>
        /// Trailing we last did a hash check in, to avoid doing them twice.
        /// </summary>
        private long _lastHashCheck;

        /// <summary>
        /// Keeping track of how stressed each client is. If all have idle time
        /// and our speed is lowered, speed up again.
        /// </summary>
        private readonly float[] _clientLoads;

        /// <summary>
        /// Keeping track of whether we might be the one slowing the game.
        /// </summary>
        private bool _serverIsSlowing;

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

            // To reset the speed and load info upon player leaves.
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
        public override void Update(GameTime gameTime)
        {
            // Drive game logic.
            UpdateSimulation(gameTime, AdjustedSpeed);

            // Also take the possibility of the server slowing down the game
            // into account.
            var serverSpeed = 1f / SafeLoad;
            if (SafeLoad >= 1f && serverSpeed < AdjustedSpeed)
            {
                AdjustedSpeed = serverSpeed;
                _serverIsSlowing = true;
            }
            else if (_serverIsSlowing)
            {
                // We're not, but we might have been, so check if we got
                // faster again.
                AdjustSpeed();
            }

            // Send hash check every now and then, to check for loss of synchronization.
            // We want to use the trailing frame for this because at this point it's
            // guaranteed not to change anymore (from incoming commands -- they will be
            // discarded now).
            if (Tss.TrailingFrame <= _lastHashCheck || ((Tss.TrailingFrame % HashInterval) != 0))
            {
                return;
            }

            // Update last checked frame.
            _lastHashCheck = Tss.TrailingFrame;

            // Generate hash.
            var hasher = new Hasher();
            Tss.Hash(hasher);

            // Send message.
            using (var packet = new Packet())
            {
                Session.Send(packet
                                 .Write((byte)TssControllerMessage.HashCheck)
                                 .Write(Tss.TrailingFrame)
                                 .Write(hasher.Value));
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
            _serverIsSlowing = (SafeLoad >= 1f);
            for (var i = 0; i < _clientLoads.Length; i++)
            {
                if (_clientLoads[i] > worstLoad)
                {
                    worstLoad = _clientLoads[i];
                    _serverIsSlowing = false;
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
            if (command.Frame >= Tss.TrailingFrame)
            {
                // All commands we apply are authoritative.
                command.IsAuthoritative = true;
                base.Apply(command);

                // As a server we resend all commands.
                Send(command);
            }
            else
            {
                Logger.Trace("Client command too old: {0} < {1}. Ignoring.", command.Frame, Tss.TrailingFrame);
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
                    // Normal command, forward it.
                    var command = base.UnwrapDataForReceive(e);
                    // We're the server and we received it, so it's definitely not authoritative.
                    command.IsAuthoritative = false;
                    // Validate player number (avoid command injection for other players).
                    command.PlayerNumber = args.Player.Number;
                    return command;

                case TssControllerMessage.Synchronize:
                    // Client re-synchronizing.
                    {
                        // Get the frame the client's at.
                        var clientFrame = args.Data.ReadInt64();

                        // Get performance information of the client.
                        var player = args.Player.Number;
                        _clientLoads[player] = args.Data.ReadSingle();

                        // Adjust our desired game speed to accommodate slowest
                        // client machine. Is this the slowest client so far?
                        var clientSpeed = 1.0 / _clientLoads[player];
                        if (_clientLoads[player] >= 1f && clientSpeed < AdjustedSpeed)
                        {
                            AdjustedSpeed = clientSpeed;
                            _serverIsSlowing = false;
                        }
                        else
                        {
                            // We potentially got faster as a collective,
                            // re-evaluate at what speed we want to run.
                            AdjustSpeed();
                        }

                        // Send our reply.
                        using (var packet = new Packet())
                        {
                            Session.SendTo(args.Player, packet
                                .Write((byte)TssControllerMessage.Synchronize)
                                .Write(clientFrame)
                                .Write(Tss.CurrentFrame)
                                .Write((float)AdjustedSpeed));
                        }
                    }
                    break;

                case TssControllerMessage.GameStateRequest:
                    // Client needs game state.
                    var hasher = new Hasher();
                    Tss.Hash(hasher);
                    using (var packet = new Packet())
                    {
                        Session.SendTo(args.Player, packet
                            .Write((byte)TssControllerMessage.GameStateResponse)
                            .Write(hasher.Value)
                            .Write(Tss));
                    }
                    break;
            }
            return null;
        }

        #endregion
    }
}
