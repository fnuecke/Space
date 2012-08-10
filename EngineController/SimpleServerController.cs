using Engine.Serialization;
using Engine.Session;
using Engine.Simulation;

namespace Engine.Controller
{
    /// <summary>
    /// A simple default implementation of a game server, using a TSS
    /// simulation and a HybridServerSession.
    /// </summary>
    /// <typeparam name="TPlayerData">The type of player data being used.</typeparam>
    public sealed class SimpleServerController<TPlayerData> : AbstractTssServer
        where TPlayerData : IPacketizable, new()
    {
        #region Constructor

        /// <summary>
        /// Creates a new server listening on the specified port and allowing
        /// for the specified number of players.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="maxPlayers">The maximum number of players supported.</param>
        /// <param name="commandHandler">The command handler to use in the
        /// simulation.</param>
        public SimpleServerController(ushort port, int maxPlayers, CommandHandler commandHandler)
            : base(new HybridServerSession<TPlayerData>(port, maxPlayers))
        {
            var simulation = new DefaultSimulation();
            simulation.Command += commandHandler;
            Tss.Initialize(simulation);
        }

        /// <summary>
        /// Clean up, shut down session.
        /// </summary>
        /// <param name="disposing"></param>
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

        /// <summary>
        /// Update session and self.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        public override void Update(float elapsedMilliseconds)
        {
            Session.Update();
            
            base.Update(elapsedMilliseconds);
        }

        #endregion
    }
}
