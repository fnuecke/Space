﻿using Engine.ComponentSystem.Common.Systems;
using Engine.Controller;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;
using Space.Session;
using Space.Simulation.Commands;

namespace Space.Control
{
    /// <summary>The game server, handling everything server logic related.</summary>
    internal sealed class GameServer : GameComponent
    {
        #region Properties

        /// <summary>The controller in use by this game server.</summary>
        public ISimulationController<IServerSession> Controller { get; private set; }

        /// <summary>Whether controller updating is paused or not.</summary>
        public bool Paused { get; set; }

        #endregion

        #region Constructor

        /// <summary>Creates a new game server for the specified game.</summary>
        /// <param name="game">The game to create the server for.</param>
        /// <param name="purelyLocal">Whether to create a purely local game (single player).</param>
        public GameServer(Program game, bool purelyLocal = false)
            : base(game)
        {
            // Get the controller.
            Controller = ControllerFactory.CreateServer(game, purelyLocal);

            // Add listeners.
            Controller.Session.GameInfoRequested += HandleGameInfoRequested;
            Controller.Session.PlayerLeft += HandlePlayerLeft;
            Controller.Session.JoinRequested += HandleJoinRequested;
        }

        /// <summary>Cleans up listeners and disposes the controller.</summary>
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

        /// <summary>Update the controller.</summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Paused)
            {
                Controller.Update(0.0f);
            }
            else
            {
                Controller.Update((float) gameTime.ElapsedGameTime.TotalMilliseconds);
            }
        }

        #endregion

        #region Events

        private void HandleGameInfoRequested(object sender, RequestEventArgs e)
        {
            e.Data.Write("Hello there!");
        }

        /// <summary>Remove ships of players that have left the game.</summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Used to figure out which player left.</param>
        private void HandlePlayerLeft(object sender, PlayerEventArgs e)
        {
            // Player left the game, remove his ship.
            var avatarSystem = (AvatarSystem) Controller.Simulation.Manager.GetSystem(AvatarSystem.TypeId);
            var ship = avatarSystem.GetAvatar(e.Player.Number);
            if (ship > 0)
            {
                Controller.Simulation.Manager.RemoveEntity(ship);
            }
        }

        /// <summary>Create a ship for newly joined players.</summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Used to figure out which player joined.</param>
        private void HandleJoinRequested(object sender, JoinRequestEventArgs e)
        {
            // Get the profile data of the player.
            var profile = (Profile) e.Player.Data;

            // TODO validate ship data (i.e. valid ship with valid equipment etc.)

            // Push the command to restore the player's profile. This will
            // create the player's avatar and restore his stats and items.
            // We use the bitwise complement of the player number while storing
            // it in the command, so as not to interfere with sorting (as we
            // won't assign an id to the command).
            Controller.Simulation.PushCommand(
                new RestoreProfileCommand(~e.Player.Number, profile, Controller.Simulation.CurrentFrame));
        }

        #endregion
    }
}