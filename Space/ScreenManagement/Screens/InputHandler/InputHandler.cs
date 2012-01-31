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
using Space.ScreenManagement.Screens.Interfaces;
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
        /// A list of objects that implements the IMouseInput interface.
        /// Will be looped in each mouse input handler.
        /// </summary>
        private List<IMouseInput> _mouseInput;

        /// <summary>
        /// A list of objects that implements the IKeyboardInput interface.
        /// Will be looped in each keyboard input handler.
        /// </summary>
        private List<IKeyboardInput> _keyboardInput;

        /// <summary>
        /// A list of objects that implements the IGamepadInput interface.
        /// Will be looped in each gamepad input handler.
        /// </summary>
        private List<IGamepadInput> _gamepadInput;

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

        public InputHandler(GameClient client)
        {
            _client = client;

            _keyboard = ((IKeyboard)client.Game.Services.GetService(typeof(IKeyboard)));
            _mouse = ((IMouse)client.Game.Services.GetService(typeof(IMouse)));
            _gamepad = ((IGamePad)client.Game.Services.GetService(typeof(IGamePad)));

            _mouseInput = new List<IMouseInput>();
            _keyboardInput = new List<IKeyboardInput>();
            _gamepadInput = new List<IGamepadInput>();

            // only temporary in use for origin input handler action !!!
            _originInputHandler = new OriginInputHandler(client);
            AddMouseListener(_originInputHandler);
            AddKeyboardListener(_originInputHandler);
            AddGamepadListener(_originInputHandler);
        }

        #endregion

        #region Logic (only temporary in use for origin input handler action !!!)

        public void Update()
        {
            _originInputHandler.Update();
        }

        #endregion

        #region Player input

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

        #region Keyboard

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        private void HandleKeyPressed(Keys key)
        {
            // loop all keyboard listeners
            foreach (IKeyboardInput i in _gamepadInput)
            {
                i.HandleKeyPressed(key);
            }
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(Keys key)
        {
            // loop all keyboard listeners
            foreach (IKeyboardInput i in _gamepadInput)
            {
                i.HandleKeyReleased(key);
            }
        }

        #endregion

        #region Mouse

        /// <summary>
        /// Handle mouse presses.
        /// </summary>
        private void HandleMousePressed(MouseButtons buttons)
        {
            // loop all mouse listener
            foreach (IMouseInput i in _mouseInput)
            {
                i.HandleMousePressed(buttons);
            }
        }

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        private void HandleMouseReleased(MouseButtons buttons)
        {
            // loop all mouse listener
            foreach (IMouseInput i in _mouseInput)
            {
                i.HandleMouseReleased(buttons);
            }
        }

        /// <summary>
        /// Update facing direction on mouse move.
        /// </summary>
        private void HandleMouseMoved(float x, float y)
        {
            // loop all mouse listener
            foreach (IMouseInput i in _mouseInput)
            {
                i.HandleMouseMoved(x, y);
            }
        }

        #endregion

        #region Gamepad

        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        private void HandleGamePadPressed(Buttons buttons)
        {
            // loop all gamepad listeners
            foreach (IGamepadInput i in _gamepadInput)
            {
                i.HandleGamePadPressed(buttons);
            }
        }

        /// <summary>
        /// Handle game pad key releases.
        /// </summary>
        private void HandleGamePadReleased(Buttons buttons)
        {
            // loop all gamepad listeners
            foreach (IGamepadInput i in _gamepadInput)
            {
                i.HandleGamePadReleased(buttons);
            }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Add any object that inplements the IMouseInput interface
        /// </summary>
        /// <param name="listener"></param>
        public void AddMouseListener(IMouseInput listener)
        {
            _mouseInput.Add(listener);
        }

        /// <summary>
        /// Add any object that inplements the IGamepadInput interface
        /// </summary>
        /// <param name="listener"></param>
        public void AddGamepadListener(IGamepadInput listener)
        {
            _gamepadInput.Add(listener);
        }

        /// <summary>
        /// Add any object that inplements the IKeyboardInput interface
        /// </summary>
        /// <param name="listener"></param>
        public void AddKeyboardListener(IKeyboardInput listener)
        {
            _keyboardInput.Add(listener);
        }

        #endregion
    }
}
