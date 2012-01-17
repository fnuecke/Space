using System.IO;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.Control
{
    /// <summary>
    /// The game server, handling everything client logic related.
    /// </summary>
    public class GameClient : GameComponent
    {
        #region Logger
        
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

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
        /// Adds event listener for connected events, to auto execute console
        /// commands.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Controller.Session.JoinResponse += ConsoleAutoexec;
        }

        /// <summary>
        /// Kills off the emitter and controller.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            Controller.Session.JoinResponse -= ConsoleAutoexec;

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
                var avatar = avatarSystem.GetAvatar(Controller.Session.LocalPlayer.Number);
                if (avatar != null)
                {
                    return avatar.GetComponent<ShipInfo>();
                }
            }
            return null;
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

        #region Event handlers

        /// <summary>
        /// Join complete, run our autoexec file, if we have one.
        /// </summary>
        private void ConsoleAutoexec(object sender, JoinResponseEventArgs e)
        {
            var console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));
            if (console != null)
            {
                if (File.Exists(Settings.Instance.AutoexecFilename))
                {
                    logger.Info("Found autoexec file at '{0}', running it now...", Settings.Instance.AutoexecFilename);
                    try
                    {
                        using (var stream = File.OpenRead(Settings.Instance.AutoexecFilename))
                        using (var reader = new StreamReader(stream))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                console.Execute(line);
                            }
                        }
                        logger.Info("Done running autoexec file.");
                    }
                    catch (IOException)
                    {
                        logger.Info("Failed reading autoexec file, skipping.");
                    }
                }
                else
                {
                    logger.Info("No autoexec file found, skipping.");
                }
            }
        }

        #endregion
    }
}
