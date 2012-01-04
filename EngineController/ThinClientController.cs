using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// This is a thin client, in the sense that it does not keep its own game
    /// state, but uses the one of the server specified in the constructor
    /// instead. It still sends and receives commands normally, although all
    /// received commands will simply be ignored.
    /// 
    /// <para>
    /// Like the <c>SimpleClientController</c> it uses a HybridClientSession,
    /// and sends commands wrapped for a TSS controller, so it is compatible
    /// only with implementations of the AbstractTssController (e.g.
    /// SimpleServerController)
    /// </para>
    /// </summary>
    /// <typeparam name="TPlayerData">The type of player data being used.</typeparam>
    public sealed class ThinClientController<TPlayerData>
        : AbstractController<IClientSession, IFrameCommand>, IClientController<IFrameCommand>, ISimulationController<IClientSession>
        where TPlayerData : IPacketizable, new()
    {
        #region Properties
        
        /// <summary>
        /// The actual underlying simulations, being that of the server.
        /// </summary>
        public ISimulation Simulation { get { return _server.Simulation; } }

        /// <summary>
        /// The real running speed, being that of the server.
        /// </summary>
        public override double CurrentSpeed { get { return _server.CurrentSpeed; } }

        #endregion

        #region Fields
        
        /// <summary>
        /// The server this client is coupled to.
        /// </summary>
        private ISimulationController<IServerSession> _server;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new thin client, instantly joining the specified server.
        /// </summary>
        /// <param name="server">The local server to join.</param>
        /// <param name="playerName">The player name to use.</param>
        /// <param name="playerData">The player data to use.</param>
        public ThinClientController(ISimulationController<IServerSession> server,
            string playerName, TPlayerData playerData)
            : base(new HybridClientSession<TPlayerData>())
        {
            _server = server;

            this.Session.Join(_server.Session, playerName, playerData);
        }

        /// <summary>
        /// Cleans up, sending the server the info that we left.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Session.ConnectionState != ClientState.Unconnected)
                {
                    this.Session.Leave();
                }

                Session.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic
        
        /// <summary>
        /// Update the underlying session. Incoming stuff isn't used, but
        /// avoids clotting of the received message buffer.
        /// </summary>
        /// <param name="gameTime">Unused.</param>
        public override void Update(GameTime gameTime)
        {
            Session.Update();
        }

        /// <summary>
        /// Render the server game state.
        /// </summary>
        public override void Draw()
        {
            if (Session.ConnectionState == ClientState.Connected)
            {
                Simulation.EntityManager.SystemManager.Draw(Simulation.CurrentFrame);
            }
        }

        #endregion

        #region Commands

        public void PushLocalCommand(IFrameCommand command)
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
            // Wrap it up like a TSS client would.
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
