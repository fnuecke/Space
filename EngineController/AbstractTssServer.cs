using System;
using Engine.ComponentSystem.Entities;
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
    /// <typeparam name="TPlayerData">the tpye of the player data structure.</typeparam>
    /// <typeparam name="TPacketizerContext">the type of the packetizer context.</typeparam>
    public abstract class AbstractTssServer : AbstractTssController<IServerSession>
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// The interval in milliseconds after which to send a hash check to the clients.
        /// </summary>
        private const int HashInterval = 10000;

        /// <summary>
        /// The interval in milliseconds after which we allow resending the game state to
        /// a specific client.
        /// </summary>
        private const int GameStateResendInterval = 5000;

        #endregion

        #region Fields

        /// <summary>
        /// Last time we sent a hash check to our clients.
        /// </summary>
        private long _lastHashTime;

        /// <summary>
        /// Keeping track of how stressed each client is. If all have idle time
        /// and our speed is lowered, speed up again.
        /// </summary>
        private float[] _clientLoads;

        /// <summary>
        /// The adjusted speed we're currently running at, based on how well
        /// our clients currently fare.
        /// </summary>
        private double _adjustedSpeed = 1.0;

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
        /// <param name="game">the game this belongs to.</param>
        /// <param name="maxPlayers">the maximum number of players in the game.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        protected AbstractTssServer(IServerSession session)
            : base(session, new uint[] {
                (uint)System.Math.Ceiling(50 / _targetElapsedMilliseconds), //< Expected case.
                (uint)System.Math.Ceiling(250 / _targetElapsedMilliseconds) //< To avoid discrimination of laggy connections.
            })
        {
            _clientLoads = new float[Session.MaxPlayers];

            // To reset the speed and load info upon player leaves.
            Session.PlayerLeft += HandlePlayerLeft;
        }

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
            UpdateSimulation(gameTime, _adjustedSpeed);

            // Also take the possibility of the server slowing down the game
            // into account.
            double serverSpeed = 1f / CurrentLoad;
            if (CurrentLoad >= 1f && serverSpeed < _adjustedSpeed)
            {
                _adjustedSpeed = serverSpeed;
                _serverIsSlowing = true;
            }
            else if (_serverIsSlowing)
            {
                // We're not, but we might have been, so check if we got
                // faster again.
                AdjustSpeed();
            }

            // Send hash check every now and then, to check for loss of synchronization.
            if (new TimeSpan(DateTime.Now.Ticks - _lastHashTime).TotalMilliseconds > HashInterval)
            {
                _lastHashTime = DateTime.Now.Ticks;

                Hasher hasher = new Hasher();
                _tss.Hash(hasher);

                using (var packet = new Packet())
                {
                    Session.Send(packet
                        .Write((byte)TssControllerMessage.HashCheck)
                        .Write(_tss.TrailingFrame)
                        .Write(hasher.Value));
                }
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
            double worstLoad = CurrentLoad;
            _serverIsSlowing = (CurrentLoad >= 1f);
            for (int i = 0; i < _clientLoads.Length; i++)
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
                _adjustedSpeed = 1f / worstLoad;
            }
            else
            {
                _adjustedSpeed = 1;
            }
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// Add a entity to the simulation. Will be inserted at the
        /// current leading frame. The entity will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="entity">the entity to add.</param>
        /// <param name="frame">the frame in which to add the entity.</param>
        /// <returns>the id the entity was assigned.</returns>
        public void AddEntity(Entity entity, long frame)
        {
            // Add the entity to the simulation.
            _tss.AddEntity(entity, frame);

            // Notify all players in the game about this.
            using (var packet = new Packet())
            {
                Session.Send(packet
                    .Write((byte)TssControllerMessage.AddGameObject)
                    .Write(frame)
                    .Write(entity));
            }
        }

        /// <summary>
        /// Removes a entity with the given id from the simulation.
        /// The entity will be removed at the given frame.
        /// </summary>
        /// <param name="entityId">the id of the entity to remove.</param>
        /// <param name="frame">the frame in which to remove the entity.</param>
        public void RemoveEntity(int entityUid, long frame)
        {
            // Remove the entity from the simulation.
            _tss.RemoveEntity(entityUid, frame);

            // Notify all players in the game about this.
            using (var packet = new Packet())
            {
                Session.Send(packet
                    .Write((byte)TssControllerMessage.RemoveGameObject)
                    .Write(frame)
                    .Write(entityUid));
            }
        }

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        protected override void Apply(FrameCommand command)
        {
            if (command.Frame >= _tss.TrailingFrame)
            {
                // All commands we apply are authoritative.
                command.IsAuthoritative = true;
                base.Apply(command);

                // As a server we resend all commands.
                Send(command);
            }
            else
            {
                logger.Trace("Client command too old: {0} < {1}. Ignoring.", command.Frame, _tss.TrailingFrame);
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
                        long clientFrame = args.Data.ReadInt64();

                        // Get performance information of the client.
                        int player = args.Player.Number;
                        _clientLoads[player] = args.Data.ReadSingle();

                        // Adjust our desired game speed to accommodate slowest
                        // client machine. Is this the slowest client so far?
                        double clientSpeed = 1.0 / _clientLoads[player];
                        if (_clientLoads[player] >= 1f && clientSpeed < _adjustedSpeed)
                        {
                            _adjustedSpeed = clientSpeed;
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
                                .Write(_tss.CurrentFrame)
                                .Write((float)_adjustedSpeed));
                        }
                    }
                    break;

                case TssControllerMessage.GameStateRequest:
                    // Client needs game state.
                    var hasher = new Hasher();
                    _tss.Hash(hasher);
                    using (var packet = new Packet())
                    {
                        Session.SendTo(args.Player, packet
                            .Write((byte)TssControllerMessage.GameStateResponse)
                            .Write(hasher.Value)
                            .Write(_tss));
                    }
                    break;

                // Everything else is unhandled on the server.
                default:
                    break;
            }
            return null;
        }

        #endregion
    }
}
