using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.Control
{
    /// <summary>
    /// The game server, handling everything client logic related.
    /// </summary>
    public class GameClient : GameComponent
    {
        #region Properties
        
        /// <summary>
        /// The controller used by this game client.
        /// </summary>
        public IClientController<FrameCommand> Controller { get; set; }

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
        /// Kills off the emitter and controller.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Controller.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Test if this game is valid and running.
        /// </summary>
        /// <returns></returns>
        public bool IsRunning()
        {
            return Controller.Session.ConnectionState == ClientState.Connected;
        }

        /// <summary>
        /// Get the information facade for the local player's ship in the
        /// game, if possible.
        /// </summary>
        /// <returns>The local player's ship information facade.</returns>
        public ShipInfo GetPlayerShipInfo()
        {
            var avatarSystem = GetSystem<AvatarSystem>();
            if (avatarSystem != null)
            {
                return avatarSystem.GetAvatar(Controller.Session.LocalPlayer.Number).GetComponent<ShipInfo>();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the most current representation of a component system of the
        /// specified type.
        /// </summary>
        /// <typeparam name="T">The type of the component system to get.</typeparam>
        /// <returns>The component system of that type, or <c>null</c></returns>
        public T GetSystem<T>() where T : IComponentSystem
        {
            if (IsRunning())
            {
                return Controller.Simulation.EntityManager.SystemManager.GetSystem<T>();
            }
            else
            {
                return default(T);
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the controller.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Controller.Update(gameTime);
        }

        #endregion
    }
}
