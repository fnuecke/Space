using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    public abstract class AbstractTssUdpController<TSession, TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        : AbstractUdpController<TSession, IFrameCommand<TCommandType, TPlayerData, TPacketizerContext>, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TState : IReversibleSubstate<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Fields

        /// <summary>
        /// The underlying simulation used.
        /// </summary>
        protected TSS<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> simulation;

        /// <summary>
        /// The remainder of time we did not update last frame, which we'll add to the
        /// elapsed time in the next frame update.
        /// </summary>
        private double lastUpdateRemainder;

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initiliaze session and base classes.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractTssUdpController(Game game, ushort port, string header, uint[] delays)
            : base(game, port, header)
        {
            simulation = new TSS<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>(delays);
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
        /// udpate.</param>
        protected void UpdateSimulation(GameTime gameTime)
        {
            if (Game.IsFixedTimeStep)
            {
                simulation.Update();
            }
            else
            {
                // Compensate for dynamic timestep.
                double elapsed = gameTime.ElapsedGameTime.TotalMilliseconds + lastUpdateRemainder;
                double target = Game.TargetElapsedTime.TotalMilliseconds;
                while (elapsed > target)
                {
                    elapsed -= target;
                    simulation.Update();
                }
                lastUpdateRemainder = elapsed;
            }
        }

        #endregion

        #region Modify simulation

        /// <summary>
        /// Apply a command.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived (if sent).</param>
        public virtual void Apply(IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> command, uint pollRate = 0)
        {
            simulation.PushCommand(command, command.Frame);
        }

        /// <summary>
        /// Add a steppable to the simulation. Will be inserted at the
        /// current leading frame. The steppable will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="steppable">the steppable to add.</param>
        /// <returns>the id the steppable was assigned.</returns>
        public long AddSteppable(TSteppable steppable)
        {
            return AddSteppable(steppable, simulation.CurrentFrame);
        }

        /// <summary>
        /// Add a steppable to the simulation. Will be inserted at the
        /// current leading frame. The steppable will be given a unique
        /// id, by which it may later be referenced for removals.
        /// </summary>
        /// <param name="steppable">the steppable to add.</param>
        /// <param name="frame">the frame in which to add the steppable.</param>
        /// <returns>the id the steppable was assigned.</returns>
        public virtual long AddSteppable(TSteppable steppable, long frame)
        {
            // Add the steppable to the simulation.
            simulation.Add(steppable, frame);
            return steppable.UID;
        }

        /// <summary>
        /// Removes a steppable with the given id from the simulation.
        /// The steppable will be removed at the current frame.
        /// </summary>
        /// <param name="steppableId">the id of the steppable to remove.</param>
        public void RemoveSteppable(long steppableUid)
        {
            RemoveSteppable(steppableUid, simulation.CurrentFrame);
        }

        /// <summary>
        /// Removes a steppable with the given id from the simulation.
        /// The steppable will be removed at the given frame.
        /// </summary>
        /// <param name="steppableId">the id of the steppable to remove.</param>
        /// <param name="frame">the frame in which to remove the steppable.</param>
        public virtual void RemoveSteppable(long steppableUid, long frame)
        {
            // Remove the steppable from the simulation.
            simulation.Remove(steppableUid, frame);
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Prepends all normal command messages with the corresponding flag.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="packet">the final packet to send.</param>
        /// <returns>the given packet, after writing.</returns>
        protected override Packet WrapDataForSend(IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> command, Packet packet)
        {
            packet.Write((byte)TssUdpControllerMessage.Command);
            return base.WrapDataForSend(command, packet);
        }

        #endregion
    }
}
