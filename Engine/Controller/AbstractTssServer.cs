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
        /// Counter used to distribute ids.
        /// </summary>
        private long _lastUid;

        /// <summary>
        /// Last time we sent a hash check to our clients.
        /// </summary>
        private long _lastHashTime;

        /// <summary>
        /// The last time we sent a full snapshot of the game state to certain
        /// player. We use this to avoid utterly overloading the network.
        /// </summary>
        private DateTime[] _lastGameStateSentTime;

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
        protected AbstractTssServer(Game game, IServerSession session)
            : base(game, session, new uint[] {
                (uint)System.Math.Ceiling(50 / game.TargetElapsedTime.TotalMilliseconds),
                (uint)System.Math.Ceiling(250 / game.TargetElapsedTime.TotalMilliseconds)
            })
        {
            _lastGameStateSentTime = new DateTime[Session.MaxPlayers];
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            if (Session != null)
            {
                Session.GameInfoRequested += HandleGameInfoRequested;
                Session.JoinRequested += HandleJoinRequested;
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
                Session.GameInfoRequested -= HandleGameInfoRequested;
                Session.JoinRequested -= HandleJoinRequested;
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
            UpdateSimulation(gameTime);

            // Send hash check every now and then, to check for loss of synchronization.
            if (new TimeSpan(DateTime.Now.Ticks - _lastHashTime).TotalMilliseconds > HashInterval)
            {
                _lastHashTime = DateTime.Now.Ticks;

                Hasher hasher = new Hasher();
                tss.Hash(hasher);

                Session.Send(new Packet()
                    .Write((byte)TssControllerMessage.HashCheck)
                    .Write(tss.TrailingFrame)
                    .Write(hasher.Value));
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Some remote machine sent a request for open games. Use this callback
        /// to send back some custom data.
        /// </summary>
        /// <param name="sender">the underlying session.</param>
        /// <param name="e">information of the type <c>RequestEventArgs</c>.</param>
        protected abstract void HandleGameInfoRequested(object sender, EventArgs e);

        /// <summary>
        /// A player asked to join our game. He passed the session checks (game full,
        /// data sent was valid). In this callback it is possible to override this,
        /// and forbid the joining, or send him some custom data with the response.
        /// </summary>
        /// <param name="sender">the underlying session.</param>
        /// <param name="e">information of the type <c>JoinRequestEventArgs</c>.</param>
        protected abstract void HandleJoinRequested(object sender, EventArgs e);

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
        public override long AddEntity(IEntity entity, long frame)
        {
            // Give the entity a unique id. Skip the zero to avoid
            // referencing that object with uninitialized 'pointers'.
            entity.UID = ++_lastUid;

            // Add the entity to the simulation.
            base.AddEntity(entity, frame);

            // Notify all players in the game about this.
            Packet addedInfo = new Packet()
                .Write((byte)TssControllerMessage.AddGameObject)
                .Write(frame);
            // Run it through the packetizer, because we don't know the actual type.
            Packetizer.Packetize(entity, addedInfo);
            Session.Send(addedInfo);

            return entity.UID;
        }

        /// <summary>
        /// Removes a entity with the given id from the simulation.
        /// The entity will be removed at the given frame.
        /// </summary>
        /// <param name="entityId">the id of the entity to remove.</param>
        /// <param name="frame">the frame in which to remove the entity.</param>
        public override void RemoveEntity(long entityUid, long frame)
        {
            // Remove the entity from the simulation.
            base.RemoveEntity(entityUid, frame);

            // Notify all players in the game about this.
            Session.Send(new Packet()
                .Write((byte)TssControllerMessage.RemoveGameObject)
                .Write(frame)
                .Write(entityUid));
        }

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        protected override void Apply(IFrameCommand command)
        {
            if (command.Frame >= tss.TrailingFrame)
            {
                // All commands we apply are authoritative.
                command.IsAuthoritative = true;
                base.Apply(command);

                // As a server we resend all commands.
                Send(command);
            }
            else
            {
                logger.Trace("Client command too old " + command.Frame + "<" + tss.TrailingFrame);
            }
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Takes care of server side TSS synchronization logic.
        /// </summary>
        protected override IFrameCommand UnwrapDataForReceive(SessionDataEventArgs e)
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
                        long clientFrame = args.Data.ReadInt64();
                        Session.SendTo(args.Player, new Packet()
                            .Write((byte)TssControllerMessage.Synchronize)
                            .Write(clientFrame)
                            .Write(tss.CurrentFrame));
                    }
                    break;

                case TssControllerMessage.GameStateRequest:
                    // Client needs game state.
                    if ((DateTime.Now - _lastGameStateSentTime[args.Player.Number]).TotalMilliseconds > GameStateResendInterval) {
                        _lastGameStateSentTime[args.Player.Number] = DateTime.Now;
                        Session.SendTo(args.Player, new Packet()
                            .Write((byte)TssControllerMessage.GameStateResponse)
                            .Write(tss));
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
