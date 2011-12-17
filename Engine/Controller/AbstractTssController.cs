using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for clients and servers using the UDP protocol and a TSS state.
    /// </summary>
    public abstract class AbstractTssController<TSession, TCommand, TPlayerData>
        : AbstractController<TSession, IFrameCommand<TPlayerData>, TPlayerData>,
          IStateController<TSession, TCommand, TPlayerData>
        where TSession : ISession<TPlayerData>
        where TCommand : IFrameCommand<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        #region Types

        /// <summary>
        /// Used in abstract TSS server and client implementations.
        /// </summary>
        protected enum TssControllerMessage
        {
            /// <summary>
            /// Normal game command, handled in base class.
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
            GameStateRequest,

            /// <summary>
            /// Server sends game state to client in response to <c>GameStateRequest</c>.
            /// </summary>
            GameStateResponse,

            /// <summary>
            /// Server tells players about a new object to insert into the simulation.
            /// </summary>
            AddGameObject,

            /// <summary>
            /// Server tells players to remove an object from the simulation.
            /// </summary>
            RemoveGameObject,

            /// <summary>
            /// Compare the hash of the leading game state at a given frame. If
            /// the client fails the check, it'll have to request a new snapshot.
            /// </summary>
            HashCheck
        }

        #endregion

        #region Properties

        /// <summary>
        /// The underlying simulation used. Directly changing this is strongly
        /// discouraged, as it will lead to clients having to resynchronize
        /// themselves by getting a snapshot of the complete simulation.
        /// </summary>
        protected TSS< TPlayerData> Simulation { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The remainder of time we did not update last frame, which we'll add to the
        /// elapsed time in the next frame update.
        /// </summary>
        private double _lastUpdateRemainder;

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize session and base classes.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractTssController(Game game, TSession session, uint[] delays)
            : base(game, session)
        {
            Simulation = new TSS<TPlayerData>(delays);
        }

        protected override void Dispose(bool disposing)
        {
            Simulation = null;

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update the simulation. This adjusts the update procedure based
        /// on the selected timestep of the game. For fixed, it just does
        /// one step. For variable, it determines how many steps to perform,
        /// based on the elapsed time.
        /// </summary>
        /// <param name="gameTime">the game time information for the current
        /// update.</param>
        /// <param name="timeCorrection">some value to add to the elapsed time as
        /// a correction factor. Used by clients to better sync to the server's
        /// game speed.</param>
        protected void UpdateSimulation(GameTime gameTime, double timeCorrection = 0)
        {
            if (Game.IsFixedTimeStep)
            {
                Simulation.Update();
            }
            else
            {
                // Compensate for dynamic time step.
                double elapsed = gameTime.ElapsedGameTime.TotalMilliseconds + _lastUpdateRemainder + timeCorrection;
                if (elapsed < Game.TargetElapsedTime.TotalMilliseconds)
                {
                    // If we can't actually run to the next frame, at least update
                    // back to the current frame in case rollbacks were made to
                    // accommodate player commands.
                    Simulation.RunToFrame(Simulation.CurrentFrame);
                }
                else
                {
                    // We can run at least one frame, so do the update(s). Due to the
                    // carry there may occur more than one simulation update per xna
                    // update, but that should be below the threshold of the noticeable.
                    while (elapsed >= Game.TargetElapsedTime.TotalMilliseconds)
                    {
                        elapsed -= Game.TargetElapsedTime.TotalMilliseconds;
                        Simulation.Update();
                    }
                    _lastUpdateRemainder = elapsed;
                }
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
        /// <returns>the id the entity was assigned.</returns>
        public long AddEntity(IEntity<TPlayerData> entity)
        {
            return AddEntity(entity, Simulation.CurrentFrame);
        }

        /// <summary>
        /// Add a entity to the simulation. Will be inserted at the
        /// current leading frame. The entity will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="entity">the entity to add.</param>
        /// <param name="frame">the frame in which to add the entity.</param>
        /// <returns>the id the entity was assigned.</returns>
        public virtual long AddEntity(IEntity<TPlayerData> entity, long frame)
        {
            // Add the entity to the simulation.
            Simulation.AddEntity(entity, frame);
            return entity.UID;
        }

        /// <summary>
        /// Get a entity in this simulation based on its unique identifier.
        /// </summary>
        /// <param name="entityUid">the id of the object.</param>
        /// <returns>the object, if it exists.</returns>
        public IEntity<TPlayerData> GetEntity(long entityUid)
        {
            return Simulation.GetEntity(entityUid);
        }

        /// <summary>
        /// Removes a entity with the given id from the simulation.
        /// The entity will be removed at the current frame.
        /// </summary>
        /// <param name="entityId">the id of the entity to remove.</param>
        public void RemoveEntity(long entityUid)
        {
            RemoveEntity(entityUid, Simulation.CurrentFrame);
        }

        /// <summary>
        /// Removes a entity with the given id from the simulation.
        /// The entity will be removed at the given frame.
        /// </summary>
        /// <param name="entityId">the id of the entity to remove.</param>
        /// <param name="frame">the frame in which to remove the entity.</param>
        public virtual void RemoveEntity(long entityUid, long frame)
        {
            // Remove the entity from the simulation.
            Simulation.RemoveEntity(entityUid, frame);
        }

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        protected virtual void Apply(IFrameCommand<TPlayerData> command)
        {
            Simulation.PushCommand(command, command.Frame);
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Prepends all normal command messages with the corresponding flag.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="packet">the final packet to send.</param>
        /// <returns>the given packet, after writing.</returns>
        protected override Packet WrapDataForSend(IFrameCommand<TPlayerData> command, Packet packet)
        {
            packet.Write((byte)TssControllerMessage.Command);
            return base.WrapDataForSend(command, packet);
        }

        #endregion
    }
}
