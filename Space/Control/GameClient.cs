using System;
using System.IO;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.FarMath;
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
    internal sealed class GameClient : GameComponent
    {
        #region Logger
        
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// The interval in which we save the player's profile to disk.
        /// </summary>
        private const int SaveInterval = 60;

        #endregion

        #region Properties

        /// <summary>
        /// The controller used by this game client.
        /// </summary>
        public IClientController<FrameCommand> Controller { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// Whether autoexec should run as soon as we have our avatar.
        /// </summary>
        private bool _shouldRunAutoexec;

        /// <summary>
        /// The time we last saved our profile.
        /// </summary>
        private DateTime _lastSave = DateTime.UtcNow;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new local client, which will be coupled to the given server.
        /// </summary>
        /// <param name="game">The game to create the client for.</param>
        /// <param name="server">The server to join.</param>
        public GameClient(Program game, GameServer server)
            : base(game)
        {
            Controller = ControllerFactory.CreateLocalClient(game, server.Controller);
        }

        /// <summary>
        /// Creates a new remote client, which can connect to remote games.
        /// </summary>
        /// <param name="game">The game to create the client for.</param>
        public GameClient(Program game)
            : base(game)
        {
            Controller = ControllerFactory.CreateRemoteClient(game);
        }

        /// <summary>
        /// Adds event listener for connected events, to auto execute console
        /// commands.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Controller.Session.JoinResponse += HandleJoinResponse;
            Controller.Session.Disconnecting += HandleDisconnecting;
            Controller.Session.Disconnected += HandleDisconnected;
        }

        /// <summary>
        /// Kills off the emitter and controller.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Controller.Session.JoinResponse -= HandleJoinResponse;

                Controller.Dispose();
            }

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
                if (avatar.HasValue)
                {
                    return GetComponent<ShipInfo>(avatar.Value);
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
        /// <returns>Current camera position.</returns>
        public FarPosition GetCameraPosition()
        {
            var system = GetSystem<CameraSystem>();
            if (system != null)
            {
                return system.CameraPositon;
            }
            else
            {
                return FarPosition.Zero;
            }
        }

        /// <summary>
        /// Gets the current camera zoom.
        /// </summary>
        /// <returns>Current zoom level.</returns>
        public float GetCameraZoom()
        {
            var system = GetSystem<CameraSystem>();
            if (system != null)
            {
                return system.Zoom;
            }
            else
            {
                return 1.0f;
            }
        }

        /// <summary>
        /// Get the most current representation of a component system of the
        /// specified type.
        /// </summary>
        /// <typeparam name="T">The type of the component system to get.</typeparam>
        /// <returns>The component system of that type, or <c>null</c></returns>
        public T GetSystem<T>() where T : AbstractSystem
        {
            if (IsRunning())
            {
                return (T)Controller.Simulation.Manager.GetSystem(Manager.GetSystemTypeId<T>());
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Gets the component of the specified type for the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// The component.
        /// </returns>
        public T GetComponent<T>(int entity) where T : Component
        {
            if (IsRunning())
            {
                return ((T)Controller.Simulation.Manager.GetComponent(entity, Manager.GetComponentTypeId<T>()));
            }
            else
            {
                return default(T);
            }
        } 

        /// <summary>
        /// Saves the current player state (his profile) to disk.
        /// </summary>
        public void Save()
        {
            var avatar = GetPlayerShipInfo();
            if (avatar != null)
            {
                Settings.Instance.CurrentProfile.Capture(avatar.Entity, Controller.Simulation.Manager);
            }
            Settings.Instance.CurrentProfile.Save();
            _lastSave = DateTime.UtcNow;
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

            // Update our local simulation.
            Controller.Update();

            // Save periodically.
            if ((DateTime.UtcNow - _lastSave).TotalSeconds > SaveInterval)
            {
                Save();
            }

            // Should we run some console commands? If so, and we have our avatar
            // (which signifies we successfully joined) run them.
            if (_shouldRunAutoexec && GetSystem<AvatarSystem>().GetAvatar(Controller.Session.LocalPlayer.Number) != null)
            {
                ConsoleAutoexec();
            }
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Successfully joined a game, schedule console autoexec.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Engine.Session.JoinResponseEventArgs"/> instance containing the event data.</param>
        private void HandleJoinResponse(object sender, JoinResponseEventArgs e)
        {
            _shouldRunAutoexec = true;
        }

        /// <summary>
        /// We are being disconnected from the server, do a last save.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void HandleDisconnecting(object sender, EventArgs e)
        {
            _shouldRunAutoexec = false;
            Save();
        }

        /// <summary>
        /// We have been disconnected, cancel console autoexec if it was set.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void HandleDisconnected(object sender, EventArgs e)
        {
            _shouldRunAutoexec = false;
        }

        /// <summary>
        /// Run our autoexec file, if we have one.
        /// </summary>
        private void ConsoleAutoexec()
        {
            // Only run once.
            _shouldRunAutoexec = false;

            // Get the console to run the commands in.
            var console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));
            if (console != null)
            {
                // Get the file, if possible.
                if (File.Exists(Settings.Instance.AutoexecFilename))
                {
                    Logger.Info("Found autoexec file at '{0}', running it now...", Settings.Instance.AutoexecFilename);
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
                        Logger.Info("Done running autoexec file.");
                    }
                    catch (IOException)
                    {
                        Logger.Info("Failed reading autoexec file, skipping.");
                    }
                }
                else
                {
                    Logger.Info("No autoexec file found, skipping.");
                }
            }
        }

        #endregion
    }
}
