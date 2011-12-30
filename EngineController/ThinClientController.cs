using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    public sealed class ThinClientController<TPlayerData> : AbstractController<IClientSession, IFrameCommand>,
        IClientController<IFrameCommand>, ISimulationController<IClientSession>
        where TPlayerData : IPacketizable, new()
    {

        public ISimulation Simulation
        {
            get { return _server.Simulation; }
        }

        private ISimulationController<IServerSession> _server;

        public ThinClientController(ISimulationController<IServerSession> server, string playerName, TPlayerData playerData)
            : base(new HybridClientSession<TPlayerData>())
        {
            _server = server;
            this.Session.Join(_server.Session, playerName, playerData);
        }

        public override void Update(GameTime gameTime)
        {
            Session.Update();
        }

        public override void Draw()
        {
            if (Session.ConnectionState == ClientState.Connected)
            {
                Simulation.EntityManager.SystemManager.Update(ComponentSystemUpdateType.Display, Simulation.CurrentFrame);
            }
        }

        #region Commands

        /// <summary>
        /// Add this controller as a listener to the given emitter, handling
        /// whatever commands it produces.
        /// </summary>
        /// <param name="emitter">the emitter to attach to.</param>
        public void AddEmitter(ICommandEmitter<IFrameCommand> emitter)
        {
            emitter.CommandEmitted += HandleEmittedCommand;
        }

        /// <summary>
        /// Remove this controller as a listener from the given emitter.
        /// </summary>
        /// <param name="emitter">the emitter to detach from.</param>
        public void RemoveEmitter(ICommandEmitter<IFrameCommand> emitter)
        {
            emitter.CommandEmitted -= HandleEmittedCommand;
        }

        /// <summary>
        /// A command emitter we're attached to has generated a new event.
        /// Override this to fill in some default values in the command
        /// before it is passed on to <c>HandleLocalCommand</c>.
        /// </summary>
        private void HandleEmittedCommand(IFrameCommand command)
        {
            command.PlayerNumber = Session.LocalPlayer.Number;
            command.Frame = _server.Simulation.CurrentFrame + 1;
            Send(command);
        }

        #endregion

        #region Protocol layer

        /// <summary>
        /// Got command data from another client or the server.
        /// </summary>
        /// <param name="command">the received command.</param>
        protected override void HandleRemoteCommand(IFrameCommand command)
        {
            // Ignore, we use the server's simulation for rendering.
        }

        /// <summary>
        /// Prepends all normal command messages with the corresponding flag.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="packet">the final packet to send.</param>
        /// <returns>the given packet, after writing.</returns>
        protected override Packet WrapDataForSend(IFrameCommand command, Packet packet)
        {
            packet.Write((byte)AbstractTssController<IClientSession>.TssControllerMessage.Command);
            return base.WrapDataForSend(command, packet);
        }
        
        /// <summary>
        /// Takes care of client side TSS synchronization logic.
        /// </summary>
        protected override IFrameCommand UnwrapDataForReceive(SessionDataEventArgs e)
        {
            // Ignore, we use the server's simulation for rendering.
            return null;
        }

        #endregion
    }
}
