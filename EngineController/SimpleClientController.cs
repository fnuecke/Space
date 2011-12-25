using Engine.ComponentSystem.Systems;
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
    /// <typeparam name="TPlayerData"></typeparam>
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
            tss.Initialize(simulation);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Session.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        public override void Update(GameTime gameTime)
        {
            Session.Update();

            if (Session.ConnectionState == ClientState.Connected)
            {
                base.Update(gameTime);
            }
        }

        public override void Draw()
        {
            if (Session.ConnectionState == ClientState.Connected)
            {
                Simulation.EntityManager.SystemManager.Update(ComponentSystemUpdateType.Display, Simulation.CurrentFrame);
            }
        }

        #endregion
    }
}
