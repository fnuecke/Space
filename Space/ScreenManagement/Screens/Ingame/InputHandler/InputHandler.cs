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
    public sealed class InputHandler
    {

        #region Constants

        /// <summary>
        /// The interval in milliseconds in which to check for new rotation
        /// based on mouse movement.
        /// </summary>
        private const int _mousePollInterval = 50;

        #endregion

        #region Fields

        /// <summary>
        /// The game client to inject commands into.
        /// </summary>
        private GameClient _client;

        /// <summary>
        /// The ingame screen object that holds the data of all GUI elements.
        /// </summary>
        private IngameScreen _ingameScreen;

        /// <summary>
        /// The keyboard used for player input.
        /// </summary>
        private IKeyboard _keyboard;

        /// <summary>
        /// The mouse used for player input.
        /// </summary>
        private IMouse _mouse;

        /// <summary>
        /// The game pad used for player input.
        /// </summary>
        private IGamePad _gamepad;

        /// <summary>
        /// Whether we're currently enabled or not. Just used to check if it's
        /// a repetitive 'set', to avoid adding ourselves as listeners over and
        /// over again.
        /// </summary>
        private bool _enabled;

        // only temporary in use for origin input handler action !!!
        private OriginInputHandler _originInputHandler;

        #endregion

        #region Constructor

        public InputHandler(GameClient client, IngameScreen ingameScreen)
        {
            _client = client;
            _ingameScreen = ingameScreen;

            _keyboard = ((IKeyboard)client.Game.Services.GetService(typeof(IKeyboard)));
            _mouse = ((IMouse)client.Game.Services.GetService(typeof(IMouse)));
            _gamepad = ((IGamePad)client.Game.Services.GetService(typeof(IGamePad)));

            // only temporary in use for origin input handler action !!!
            _originInputHandler = new OriginInputHandler(client);
        }

        #endregion

        #region Logic (only temporary in use for origin input handler action !!!)

        public void Update()
        {
            _originInputHandler.Update();
        }

        #endregion

        #region Player input


        #region Keyboard

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        private void HandleKeyPressed(Keys key)
        {
            // get the data from the ingame screen
            var elements = _ingameScreen.GetGuiElements();

            // loop all keyboard listeners
            foreach (AbstractGuiElement e in elements)
            {
                e.HandleKeyPressed(key);
            }

            //temporal only
            _originInputHandler.HandleKeyPressed(key);
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(Keys key)
        {
            // get the data from the ingame screen
            var elements = _ingameScreen.GetGuiElements();

            // loop all keyboard listeners
            foreach (AbstractGuiElement e in elements)
            {
                e.HandleKeyReleased(key);
            }

            //temporal only
            _originInputHandler.HandleKeyReleased(key);
        }

        #endregion

        #region Mouse

        /// <summary>
        /// Handle mouse presses.
        /// </summary>
        private void HandleMousePressed(MouseButtons buttons)
        {
            // get the data from the ingame screen
            var elements = _ingameScreen.GetGuiElements();

            // loop all mouse listeners
            foreach (AbstractGuiElement e in elements)
            {
                e.HandleMousePressed(buttons);
            }

            //temporal only
            _originInputHandler.HandleMousePressed(buttons);
        }

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        private void HandleMouseReleased(MouseButtons buttons)
        {
            // get the data from the ingame screen
            var elements = _ingameScreen.GetGuiElements();

            // loop all mouse listeners
            foreach (AbstractGuiElement e in elements)
            {
                e.HandleMouseReleased(buttons);
            }

            //temporal only
            _originInputHandler.HandleMouseReleased(buttons);
        }

        /// <summary>
        /// Update facing direction on mouse move.
        /// </summary>
        private void HandleMouseMoved(float x, float y)
        {
            // get the data from the ingame screen
            var elements = _ingameScreen.GetGuiElements();

            // loop all mouse listeners
            foreach (AbstractGuiElement e in elements)
            {
                e.HandleMouseMoved(x, y);
            }

            //temporal only
            _originInputHandler.HandleMouseMoved(x, y);
        }

        #endregion

        #region Gamepad

        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        private void HandleGamePadPressed(Buttons buttons)
        {
            // get the data from the ingame screen
            var elements = _ingameScreen.GetGuiElements();

            // loop all gamepad listeners
            foreach (AbstractGuiElement e in elements)
            {
                e.HandleGamePadPressed(buttons);
            }

            //temporal only
            _originInputHandler.HandleGamePadPressed(buttons);
        }

        /// <summary>
        /// Handle game pad key releases.
        /// </summary>
        private void HandleGamePadReleased(Buttons buttons)
        {
            // get the data from the ingame screen
            var elements = _ingameScreen.GetGuiElements();

            // loop all gamepad listeners
            foreach (AbstractGuiElement e in elements)
            {
                e.HandleGamePadReleased(buttons);
            }

            //temporal only
            _originInputHandler.HandleGamePadReleased(buttons);
        }

        #endregion

        /// <summary>
        /// Set whether to accept player input or not.
        /// </summary>
        public void SetEnabled(bool value)
        {
            if (value == _enabled)
            {
                return;
            }
            _enabled = value;

            if (value)
            {
                // Register for key presses and releases (movement).
                if (_keyboard != null)
                {
                    _keyboard.KeyPressed += HandleKeyPressed;
                    _keyboard.KeyReleased += HandleKeyReleased;
                }

                // Register for mouse movement (orientation) and buttons (shooting).
                if (_mouse != null)
                {
                    _mouse.MouseMoved += HandleMouseMoved;
                    _mouse.MouseButtonPressed += HandleMousePressed;
                    _mouse.MouseButtonReleased += HandleMouseReleased;
                }

                // Register for game pad buttons. Sticks are handled in update.
                if (_gamepad != null)
                {
                    _gamepad.ButtonPressed += HandleGamePadPressed;
                    _gamepad.ButtonReleased += HandleGamePadReleased;
                }
            }
            else
            {
                if (_keyboard != null)
                {
                    _keyboard.KeyPressed -= HandleKeyPressed;
                    _keyboard.KeyReleased -= HandleKeyReleased;
                }

                if (_mouse != null)
                {
                    _mouse.MouseMoved -= HandleMouseMoved;
                    _mouse.MouseButtonPressed -= HandleMousePressed;
                    _mouse.MouseButtonReleased -= HandleMouseReleased;
                }

                if (_gamepad != null)
                {
                    _gamepad.ButtonPressed -= HandleGamePadPressed;
                    _gamepad.ButtonReleased -= HandleGamePadReleased;
                }
            }
        }

        #endregion

    }
}
