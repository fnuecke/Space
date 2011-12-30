using Engine.Controller;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;
using Space.Simulation.Commands;

namespace Space.Control
{
    /// <summary>
    /// The game server, handling everything client logic related.
    /// </summary>
    public class GameClient : DrawableGameComponent
    {
        #region Properties
        
        /// <summary>
        /// The controller used by this game client.
        /// </summary>
        public IClientController<IFrameCommand> Controller { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The command emitter in use, converting player input into simulation
        /// commands.
        /// </summary>
        private InputCommandEmitter _emitter;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new local client, which will be coupled to the given server.
        /// </summary>
        /// <param name="game">The game to create the client for.</param>
        /// <param name="server">The server to join.</param>
        public GameClient(Game game, GameServer server)
            : base(game)
        {
            Controller = ControllerFactory.CreateLocalClient(Game, server.Controller);

        }

        /// <summary>
        /// Creates a new remote client, which can connect to remote games.
        /// </summary>
        /// <param name="game">The game to create the client for.</param>
        public GameClient(Game game)
            : base(game)
        {
            Controller = ControllerFactory.CreateRemoteClient(Game);
        }

        /// <summary>
        /// Initializes the client, whether it's remote or local.
        /// </summary>
        public override void Initialize()
        {
            // Draw underneath menus etc.
            DrawOrder = -50;

            // Create our input command emitter, which is used to grab user
            // input and convert it into commands that can be injected into our
            // simulation.
            _emitter = new InputCommandEmitter(Game, Controller);
            Controller.AddEmitter(_emitter);
        }

        /// <summary>
        /// Kills off the emitter and controller.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Controller.RemoveEmitter(_emitter);
            _emitter.Dispose();

            Controller.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region Logic
        
        /// <summary>
        /// Updates the controller.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            _emitter.Update(gameTime);
            Controller.Update(gameTime);
        }

        /// <summary>
        /// Renders the game state of the controller.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            Controller.Draw();
        }

        #endregion
    }
}
