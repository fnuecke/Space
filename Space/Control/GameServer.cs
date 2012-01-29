using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Constraints;
using Space.ComponentSystem.Entities;
using Space.Session;
using Space.Simulation.Commands;

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

            var random = new MersenneTwister(0);
            var ship = EntityFactory.CreatePlayerShip(
                ConstraintsLibrary.GetConstraints<ShipConstraints>("Player"),
                e.Player.Number,
                new Vector2(60000, 60000),
                random);

            // Create some basic equipment (TODO: move to player character creation, use sent data instead).
            Controller.Simulation.PushCommand(new AddItemCommand(ConstraintsLibrary.GetConstraints<ThrusterConstraints>("Starter Thruster").Sample(random)));
            Controller.Simulation.PushCommand(new AddItemCommand(ConstraintsLibrary.GetConstraints<ReactorConstraints>("Starter Reactor").Sample(random)));
            Controller.Simulation.PushCommand(new AddItemCommand(ConstraintsLibrary.GetConstraints<SensorConstraints>("Starter Sensor").Sample(random)));
            Controller.Simulation.PushCommand(new AddItemCommand(ConstraintsLibrary.GetConstraints<ArmorConstraints>("Starter Armor").Sample(random)));
            Controller.Simulation.PushCommand(new AddItemCommand(ConstraintsLibrary.GetConstraints<WeaponConstraints>("Starter Weapon").Sample(random)));

            // Back to front, because we do this in the same frame, and the
            // command are otherwise deemed equal.
            Controller.Simulation.PushCommand(new EquipCommand(4, 0));
            Controller.Simulation.PushCommand(new EquipCommand(3, 0));
            Controller.Simulation.PushCommand(new EquipCommand(2, 0));
            Controller.Simulation.PushCommand(new EquipCommand(1, 0));
            Controller.Simulation.PushCommand(new EquipCommand(0, 0));

            Controller.Simulation.EntityManager.AddEntity(ship);
        }

        #endregion
    }
}
