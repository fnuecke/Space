using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Engine.Simulation.Commands;

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
        : AbstractController<IClientSession, FrameCommand>, IClientController<FrameCommand>
        where TPlayerData : IPacketizable, new()
    {
        #region Properties
        
        /// <summary>
        /// The actual underlying simulations, being that of the server.
        /// </summary>
        public ISimulation Simulation { get { return _server.Simulation; } }

        /// <summary>
        /// The real update load, being that of the server.
        /// </summary>
        public override float CurrentLoad { get { return _server.CurrentLoad; } }

        /// <summary>
        /// The target game speed we try to run at, if possible.
        /// </summary>
        public float TargetSpeed { get { return _server.TargetSpeed; } set { _server.TargetSpeed = value; } }
        
        /// <summary>
        /// The current actual game speed, based on possible slow-downs due
        /// to the server or other clients.
        /// </summary>
        public float ActualSpeed { get { return _server.ActualSpeed; } }

        #endregion

        #region Fields
        
        /// <summary>
        /// The server this client is coupled to.
        /// </summary>
        private readonly ISimulationController<IServerSession> _server;

        /// <summary>
        /// Next unique command ID to use. It's OK if this overflows, because any commands
        /// that old will no longer be relevant (it's unlikely this will happen in a single
        /// game, anyway).
        /// </summary>
        private int _nextCommandId = int.MinValue;

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
        public override void Update()
        {
            Session.Update();
        }

        /// <summary>
        /// Render the server game state.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public override void Draw(float elapsedMilliseconds)
        {
            if (Session.ConnectionState == ClientState.Connected)
            {
                Simulation.Manager.Draw(Simulation.CurrentFrame, elapsedMilliseconds);
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Pushes the locally emitted command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void PushLocalCommand(FrameCommand command)
        {
            command.Id = _nextCommandId++;
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
        protected override void HandleRemoteCommand(FrameCommand command)
        {
            // Ignore, we use the server's simulation for rendering.
        }

        /// <summary>
        /// Prepends all normal command messages with the corresponding flag.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="packet">the final packet to send.</param>
        /// <returns>the given packet, after writing.</returns>
        protected override Packet WrapDataForSend(FrameCommand command, Packet packet)
        {
            // Wrap it up like a TSS client would.
            packet.Write((byte)AbstractTssController<IClientSession>.TssControllerMessage.Command);
            return base.WrapDataForSend(command, packet);
        }
        
        /// <summary>
        /// Takes care of client side TSS synchronization logic.
        /// </summary>
        protected override FrameCommand UnwrapDataForReceive(SessionDataEventArgs e)
        {
            // Ignore, we use the server's simulation for rendering.
            return null;
        }

        #endregion
    }
}
