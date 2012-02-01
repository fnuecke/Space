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
    public sealed class OriginInputHandler
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
        /// The time at which we last check whether the mouse had moved.
        /// </summary>
        private DateTime _lastUpdate;

        /// <summary>
        /// The current player acceleration.
        /// </summary>
        private Vector2 _accelerationDirection;

        /// <summary>
        /// The last registered time that the mouse has moved.
        /// </summary>
        private DateTime _accelerationChanged;

        /// <summary>
        /// Whether we're currently stabilizing our position or not.
        /// </summary>
        private bool _stabilizing;

        /// <summary>
        /// The target rotation based on the current mouse position.
        /// </summary>
        private float _targetRotation;

        /// <summary>
        /// The last registered time that the mouse has moved.
        /// </summary>
        private DateTime _rotationChanged;

        /// <summary>
        /// Left game pad stick state from the last update, to check for
        /// changes.
        /// </summary>
        private Vector2 _previousGamepadAcceleration;

        /// <summary>
        /// Right game pad stick state from the last update, to check for
        /// changes.
        /// </summary>
        private Vector2 _previousGamepadLook;

        #endregion

        #region Constructor

        public OriginInputHandler(GameClient client)
        {
            _client = client;

            _keyboard = ((IKeyboard)client.Game.Services.GetService(typeof(IKeyboard)));
            _mouse = ((IMouse)client.Game.Services.GetService(typeof(IMouse)));
            _gamepad = ((IGamePad)client.Game.Services.GetService(typeof(IGamePad)));
        }

        #endregion

        #region Logic

        public void Update()
        {
            // Handle game pad input that we can't properly handle via events.
            if (Settings.Instance.EnableGamepad && _gamepad != null)
            {
                // Handle movement of the left stick, which controls our movement.
                var currentGamePadState = _gamepad.GetExtendedState();
                Vector2 gamepadAcceleration = GamePadHelper.GetAcceleration(_gamepad);
                if (gamepadAcceleration != _previousGamepadAcceleration)
                {
                    _accelerationDirection = gamepadAcceleration;
                    _accelerationDirection.Y = -_accelerationDirection.Y;
                    _accelerationChanged = DateTime.Now;
                }
                _previousGamepadAcceleration = gamepadAcceleration;

                // Handle movement of the right stick, which controls our direction.
                Vector2 gamepadLook = GamePadHelper.GetLook(_gamepad);
                if (gamepadLook != _previousGamepadLook && gamepadLook != Vector2.Zero)
                {
                    _targetRotation = (float)System.Math.Atan2(gamepadLook.Y, gamepadLook.X);
                    _rotationChanged = DateTime.Now;
                }
                _previousGamepadLook = gamepadLook;
            }

            // Only check every so often, as slight delays here will not be as
            // noticeable, due to the ship's slow turn speed.
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds > _mousePollInterval)
            {
                // Has the mouse moved since the last update?
                if (_rotationChanged > _lastUpdate)
                {
                    // Yes, push command.
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Rotate, new Vector2(_targetRotation, 0)));
                }
                if (_accelerationChanged > _lastUpdate)
                {
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Accelerate, _accelerationDirection));
                }
                _lastUpdate = DateTime.Now;
            }
        }

        #endregion

        #region Player input

        #region Keyboard

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        public void HandleKeyPressed(Keys key)
        {
            if (Settings.Instance.GameBindings.ContainsKey(key))
            {
                if (Settings.Instance.GameBindings[key] == Settings.GameCommand.Stabilize)
                {
                    if (Settings.Instance.ToggleStabilize)
                    {
                        // Toggle stabilizers.
                        _stabilizing = !_stabilizing;
                        _client.Controller.PushLocalCommand(new PlayerInputCommand(_stabilizing ? PlayerInputCommand.PlayerInputCommandType.BeginStabilizing : PlayerInputCommand.PlayerInputCommandType.StopStabilizing));
                    }
                    else
                    {
                        // Just enable stabilizers.
                        _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginStabilizing));
                    }
                }
                else
                {
                    UpdateKeyboardAcceleration();
                }
            }
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        public void HandleKeyReleased(Keys key)
        {
            if (Settings.Instance.GameBindings.ContainsKey(key))
            {
                if (Settings.Instance.GameBindings[key] == Settings.GameCommand.Stabilize)
                {
                    if (!Settings.Instance.ToggleStabilize)
                    {
                        // Disable stabilizers if not toggling.
                        _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopStabilizing));
                    }
                }
                else
                {
                    UpdateKeyboardAcceleration();
                }
            }
        }

        /// <summary>
        /// Updates acceleration direction based on key presses.
        /// </summary>
        /// <param name="state">The current keyboard state.</param>
        private void UpdateKeyboardAcceleration()
        {
            var state = _keyboard.GetState();
            Directions direction = Directions.None;
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Down].Any(key => state.IsKeyDown(key)))
            {
                // Accelerate downwards.
                direction |= Directions.Down;
            }
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Left].Any(key => state.IsKeyDown(key)))
            {
                // Accelerate left.
                direction |= Directions.Left;
            }
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Right].Any(key => state.IsKeyDown(key)))
            {
                // Accelerate right.
                direction |= Directions.Right;
            }
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Up].Any(key => state.IsKeyDown(key)))
            {
                // Accelerate upwards.
                direction |= Directions.Up;
            }

            var acceleration = DirectionConversion.DirectionToVector(direction);
            if (acceleration != _accelerationDirection)
            {
                // Only update if something changed.
                _accelerationDirection = acceleration;
                _accelerationChanged = DateTime.Now;
            }
        }

        #endregion

        #region Mouse

        /// <summary>
        /// Handle mouse presses.
        /// </summary>
        public void HandleMousePressed(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting));
            }
        }

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        public void HandleMouseReleased(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
            }
        }

        /// <summary>
        /// Update facing direction on mouse move.
        /// </summary>
        public void HandleMouseMoved(float x, float y)
        {
            // Get angle to middle of screen (position of our ship), which
            // will be our new target rotation.
            float rx = x - _client.Game.GraphicsDevice.Viewport.Width / 2;
            float ry = y - _client.Game.GraphicsDevice.Viewport.Height / 2;
            _targetRotation = (float)System.Math.Atan2(ry, rx);
            _rotationChanged = DateTime.Now;
        }

        #endregion

        #region Gamepad

        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        public void HandleGamePadPressed(Buttons buttons)
        {
            switch (buttons)
            {
                case (Buttons.RightShoulder):
                    // Shoot.
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting));
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Handle game pad key releases.
        /// </summary>
        public void HandleGamePadReleased(Buttons buttons)
        {
            switch (buttons)
            {
                case (Buttons.RightShoulder):
                    // Stop shooting.
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
                    break;

                default:
                    break;
            }
        }

        #endregion

        #endregion
    }
}
