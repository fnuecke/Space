using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Entities;
using Space.Session;

namespace Space.Control
{
    /// <summary>
    /// The game server, handling everything server logic related.
    /// </summary>
    public class GameServer : GameComponent
    {
        #region Properties
        
        /// <summary>
        /// The controller in use by this game server.
        /// </summary>
        public ISimulationController<IServerSession> Controller { get; set; }

        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new game server for the specified game.
        /// </summary>
        /// <param name="game">The game to create the server for.</param>
        public GameServer(Game game)
            : base(game)
        {
            // Get the controller.
            Controller = ControllerFactory.CreateServer(game);

            // Add listeners.
            Controller.Session.GameInfoRequested += HandleGameInfoRequested;
            Controller.Session.PlayerLeft += HandlePlayerLeft;
            Controller.Session.JoinRequested += HandleJoinRequested;
        }

        /// <summary>
        /// Cleans up listeners and disposes the controller.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Remove listeners.
                Controller.Session.GameInfoRequested -= HandleGameInfoRequested;
                Controller.Session.PlayerLeft -= HandlePlayerLeft;
                Controller.Session.JoinRequested -= HandleJoinRequested;

                // Kill controller.
                Controller.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update the controller.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            Controller.Update(gameTime);
        }
        
        #endregion

        #region Events

        private void HandleGameInfoRequested(object sender, RequestEventArgs e)
        {
            e.Data.Write("Hello there!");
        }

        /// <summary>
        /// Remove ships of players that have left the game.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Used to figure out which player left.</param>
        private void HandlePlayerLeft(object sender, PlayerEventArgs e)
        {
            // Player left the game, remove his ship.
            var avatarSystem = Controller.Simulation.EntityManager.SystemManager.GetSystem<AvatarSystem>();
            var ship = avatarSystem.GetAvatar(e.Player.Number);
            Controller.Simulation.EntityManager.RemoveEntity(ship.UID);
        }
        
        /// <summary>
        /// Create a ship for newly joined players.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Used to figure out which player joined.</param>
        private void HandleJoinRequested(object sender, JoinRequestEventArgs e)
        {
            // Create a ship for the player.
            // TODO validate ship data (i.e. valid ship with valid equipment etc.)
            var playerData = (PlayerData)e.Player.Data;
            var ship = EntityFactory.CreatePlayerShip(playerData.Ship, e.Player.Number);
            Controller.Simulation.EntityManager.AddEntity(ship);
        }

        #endregion
    }
}
