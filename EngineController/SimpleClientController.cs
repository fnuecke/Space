using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// A simple default implementation of a game client, using a TSS
    /// simulation and a HybridClientSession.
    /// </summary>
    /// <typeparam name="TPlayerData">The type of player data being used.</typeparam>
    public sealed class SimpleClientController<TPlayerData> : AbstractTssClient
        where TPlayerData : IPacketizable, new()
    {
        #region Constructor

        /// <summary>
        /// Creates a new game client, ready to connect to an open game.
        /// </summary>
        /// <param name="game"></param>
        public SimpleClientController(CommandHandler commandHandler)
            : base(new HybridClientSession<TPlayerData>())
        {
            var simulation = new DefaultSimulation();
            simulation.Command += commandHandler;
            _tss.Initialize(simulation);
        }

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

        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        public override void Update(GameTime gameTime)
        {
            Session.Update();

            if (Session.ConnectionState == ClientState.Connected)
            {
                base.Update(gameTime);
            }
        }

        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        public override void Draw(GameTime gameTime)
        {
            if (Session.ConnectionState == ClientState.Connected)
            {
                Simulation.EntityManager.SystemManager.Draw(gameTime, Simulation.CurrentFrame);
            }
        }

        #endregion
    }
}
