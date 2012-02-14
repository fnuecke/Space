using System;
using System.IO;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;
using Space.ComponentSystem.Systems;
using Space.Util;

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

        #region Construction

        /// <summary>
        /// The interval in which we save the player's profile to disk.
        /// </summary>
        private const int _saveInterval = 60;

        #endregion

        #region Properties

        /// <summary>
        /// The controller used by this game client.
        /// </summary>
        public IClientController<FrameCommand> Controller { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The time we last saved our profile.
        /// </summary>
        private DateTime _lastSave = DateTime.Now;

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
        /// Get the information facade for the ship of the player with the
        /// specified number, if possible.
        /// </summary>
        /// <param name="playerNumber">The number of the player to get the info
        /// for.</param>
        /// <returns>The player's ship information facade.</returns>
        public ShipInfo GetPlayerShipInfo(int playerNumber)
        {
            var avatarSystem = GetSystem<AvatarSystem>();
            if (avatarSystem != null)
            {
                var avatar = avatarSystem.GetAvatar(playerNumber);
                if (avatar != null)
                {
                    return avatar.GetComponent<ShipInfo>();
                }
            }
            return null;
        }

        /// <summary>
        /// Get the information facade for the ship of the specified player,
        /// if possible.
        /// </summary>
        /// <param name="player">The player to get the info from.</param>
        /// <returns>The local player's ship information facade.</returns>
        public ShipInfo GetPlayerShipInfo(Player player)
        {
            return GetPlayerShipInfo(player.Number);
        }

        /// <summary>
        /// Get the information facade for the local player's ship in the
        /// game, if possible.
        /// </summary>
        /// <returns>The local player's ship information facade.</returns>
        public ShipInfo GetPlayerShipInfo()
        {
            if (IsRunning())
            {
                return GetPlayerShipInfo(Controller.Session.LocalPlayer);
            }
            return null;
        }

        /// <summary>
        /// Gets the current camera position, i.e. the view center, in global
        /// coordinates.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCameraPosition()
        {
            var system = GetSystem<PlayerCenteredRenderSystem>();
            if (system != null)
            {
                return system.CameraPositon;
            }
            else
            {
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// Get the most current representation of a component system of the
        /// specified type.
        /// </summary>
        /// <typeparam name="T">The type of the component system to get.</typeparam>
        /// <returns>The component system of that type, or <c>null</c></returns>
        public T GetSystem<T>() where T : ISystem
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

            if ((DateTime.Now - _lastSave).TotalSeconds > _saveInterval)
            {
                Settings.Instance.CurrentProfile.Capture(GetPlayerShipInfo().Entity);
                Settings.Instance.CurrentProfile.Save();
                _lastSave = DateTime.Now;
            }
        }
        public void Save()
        {
            Settings.Instance.CurrentProfile.Capture(GetPlayerShipInfo().Entity);
            Settings.Instance.CurrentProfile.Save();
            _lastSave = DateTime.Now;
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
                        using (var reader = new StreamReader(File.OpenRead(Settings.Instance.AutoexecFilename)))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Clean up input, and skip comment lines.
                                line = line.Trim();
                                if (!line.StartsWith("#"))
                                {
                                    console.Execute(line);
                                }
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
