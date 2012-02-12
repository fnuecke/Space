using System;
using System.Linq;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.Control;
using Space.Input;
using Space.Simulation.Commands;
using Space.Util;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using System.Collections.Generic;

namespace Space.ScreenManagement.Screens.Gameplay
{
    /// <summary>
    /// Handles player input and converts it to simulation commands.
    /// </summary>
    public sealed class GeneralHandler
    {

        #region Fields

        /// <summary>
        /// The game client to inject commands into.
        /// </summary>
        private GameClient _client;

        /// <summary>
        /// The ingame screen object that holds the data of all GUI elements.
        /// </summary>
        private IngameScreen _ingameScreen;

        #endregion

        #region Constructor

        public GeneralHandler(GameClient client, IngameScreen ingameScreen)
        {
            _client = client;
            _ingameScreen = ingameScreen;
        }

        #endregion

        #region Player input

        #region Keyboard

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        public void HandleKeyPressed(Keys key)
        {
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        public void HandleKeyReleased(Keys key)
        {
            KeyboardState newState = Keyboard.GetState();

            if (key == Keys.I)
            {
                if (_ingameScreen.IsInventoryVisible())
                {
                    _ingameScreen.CloseInventory();
                }
                else
                {
                    _ingameScreen.OpenInventory();
                }
            }
        }

        #endregion

        #region Mouse

        /// <summary>
        /// Handle mouse presses.
        /// </summary>
        public void HandleMousePressed(MouseButtons buttons)
        {
        }

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        public void HandleMouseReleased(MouseButtons buttons)
        {
        }

        /// <summary>
        /// Update facing direction on mouse move.
        /// </summary>
        public void HandleMouseMoved(float x, float y)
        {
        }

        #endregion

        #region Gamepad

        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        public void HandleGamePadPressed(Buttons buttons)
        {
        }

        /// <summary>
        /// Handle game pad key releases.
        /// </summary>
        public void HandleGamePadReleased(Buttons buttons)
        {
        }

        #endregion

        #endregion

    }
}
