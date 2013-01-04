using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;

namespace Engine.Controller
{
    /// <summary>
    /// A simple default implementation of a game client, using a TSS
    /// simulation and a HybridClientSession.
    /// </summary>
    /// <typeparam name="TPlayerData">The type of player data being used.</typeparam>
    public sealed class SimpleClientController<TPlayerData> : AbstractTssClient
        where TPlayerData : class, IPacketizable, new()
    {
        #region Constructor

        /// <summary>
        /// Creates a new game client, ready to connect to an open game.
        /// </summary>
        /// <param name="commandHandler">The command handler to use.</param>
        public SimpleClientController(CommandHandler commandHandler)
            : base(new HybridClientSession<TPlayerData>())
        {
            var simulation = new DefaultSimulation();
            simulation.Command += commandHandler;
            Tss.Initialize(simulation);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Session.ConnectionState != ClientState.Unconnected)
                {
                    Session.Leave();
                }

                Session.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drives the game loop, right after driving the network protocol
        /// in the base class. Also part of synchronizing run speeds on
        /// server and client by sending sync requests in certain intervals.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        public override void Update(float elapsedMilliseconds)
        {
            Session.Update();

            if (Session.ConnectionState == ClientState.Connected)
            {
                base.Update(elapsedMilliseconds);
            }
        }

        /// <summary>
        /// Draws the current state of the simulation.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        public override void Draw(float elapsedMilliseconds)
        {
            if (Session.ConnectionState == ClientState.Connected)
            {
                Simulation.Manager.Draw(Simulation.CurrentFrame, elapsedMilliseconds);
            }
        }

        #endregion
    }
}
