using System;
using Engine.Input;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Space.Control;
using Space.Simulation.Commands;

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
        /// The keyboard manager we're using.
        /// </summary>
        private IKeyboardInputManager _keyboard;

        /// <summary>
        /// The mouse manager we're using.
        /// </summary>
        private IMouseInputManager _mouse;

        /// <summary>
        /// The GamepadManager we're using.
        /// </summary>
        private IGamepadInputManager _gamepad;

        /// <summary>
        /// Whether we're currently enabled or not. Just used to check if it's
        /// a repetitive 'set', to avoid adding ourselves as listeners over and
        /// over again.
        /// </summary>
        private bool _enabled;

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

        #endregion

        #region Constructor

        public InputHandler(GameClient client)
        {
            _client = client;

            _keyboard = (IKeyboardInputManager)client.Game.Services.GetService(typeof(IKeyboardInputManager));
            _mouse = (IMouseInputManager)client.Game.Services.GetService(typeof(IMouseInputManager));
            _gamepad = (IGamepadInputManager)client.Game.Services.GetService(typeof(IGamepadInputManager));
        }

        #endregion

        #region Logic

        public void Update()
        {
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
                    _keyboard.Pressed += HandleKeyPressed;
                    _keyboard.Released += HandleKeyReleased;
                }

                // Register for mouse movement (orientation) and buttons (shooting).
                if (_mouse != null)
                {
                    _mouse.Moved += HandleMouseMoved;
                    _mouse.Pressed += HandleMousePressed;
                    _mouse.Released += HandleMouseReleased;
                }
                if (_gamepad != null)
                {
                    _gamepad.Pressed += HandleGamePadPressed;
                    _gamepad.Released += HandleGamePadReleased;
                    _gamepad.LeftMoved += HandleGamePadLeftMoved;
                    _gamepad.RightMoved += HandleGamePadRightMoved;
                }
            }
            else
            {
                if (_keyboard != null)
                {
                    _keyboard.Pressed -= HandleKeyPressed;
                    _keyboard.Released -= HandleKeyReleased;
                }

                if (_mouse != null)
                {
                    _mouse.Moved -= HandleMouseMoved;
                    _mouse.Pressed -= HandleMousePressed;
                    _mouse.Released -= HandleMouseReleased;
                }
                if (_gamepad != null)
                {
                    _gamepad.Pressed -= HandleGamePadPressed;
                    _gamepad.Released -= HandleGamePadReleased;
                    _gamepad.LeftMoved -= HandleGamePadLeftMoved;
                    _gamepad.RightMoved -= HandleGamePadRightMoved;
                }
            }
        }

        #region Keyboard

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        private void HandleKeyPressed(object sender, KeyboardInputEventArgs e)
        {
            if (e.IsRepeat)
            {
                return;
            }

            if (e.Key == Settings.Instance.Stabilizer)
            {
                // Toggle stabilizers.
                _stabilizing = !_stabilizing;
                _client.Controller.PushLocalCommand(new PlayerInputCommand(_stabilizing ? PlayerInputCommand.PlayerInputCommandType.BeginStabilizing : PlayerInputCommand.PlayerInputCommandType.StopStabilizing));
            }
            else
            {
                UpdateKeyboardAcceleration(e.State);
            }
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(object sender, KeyboardInputEventArgs e)
        {
            UpdateKeyboardAcceleration(e.State);
        }

        /// <summary>
        /// Updates acceleration direction based on key presses.
        /// </summary>
        /// <param name="state">The current keyboard state.</param>
        private void UpdateKeyboardAcceleration(KeyboardState state)
        {
            Directions direction = Directions.None;
            if (state.IsKeyDown(Settings.Instance.MoveDown))
            {
                // Accelerate downwards.
                direction |= Directions.Down;
            }
            if (state.IsKeyDown(Settings.Instance.MoveLeft))
            {
                // Accelerate left.
                direction |= Directions.Left;
            }
            if (state.IsKeyDown(Settings.Instance.MoveRight))
            {
                // Accelerate right.
                direction |= Directions.Right;
            }
            if (state.IsKeyDown(Settings.Instance.MoveUp))
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
        private void HandleMousePressed(object sender, MouseInputEventArgs e)
        {
            if (e.Button == MouseInputEventArgs.MouseButton.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting));
            }
        }

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        private void HandleMouseReleased(object sender, MouseInputEventArgs e)
        {
            if (e.Button == MouseInputEventArgs.MouseButton.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
            }
        }

        /// <summary>
        /// Update facing direction on mouse move.
        /// </summary>
        private void HandleMouseMoved(object sender, MouseInputEventArgs e)
        {
            // Get angle to middle of screen (position of our ship), which
            // will be our new target rotation.
            int rx = e.X - _client.Game.GraphicsDevice.Viewport.Width / 2;
            int ry = e.Y - _client.Game.GraphicsDevice.Viewport.Height / 2;
            _targetRotation = (float)System.Math.Atan2(ry, rx);
            _rotationChanged = DateTime.Now;
        }

        #endregion

        #region Gamepad

        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        private void HandleGamePadPressed(object sender, GamepadInputEventArgs e)
        {
            switch (e.Buttons)
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
        private void HandleGamePadReleased(object sender, GamepadInputEventArgs e)
        {
            switch (e.Buttons)
            {
                case (Buttons.RightShoulder):
                    // Stop shooting.
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Handle movement of the left stick, which controls our movement.
        /// </summary>
        private void HandleGamePadLeftMoved(object sender, GamepadInputEventArgs e)
        {
            _accelerationDirection = e.Position;
            _accelerationDirection.Y = -_accelerationDirection.Y;
            _accelerationChanged = DateTime.Now;
        }

        /// <summary>
        /// Handle movement of the right stick, which controls our direction.
        /// </summary>
        private void HandleGamePadRightMoved(object sender, GamepadInputEventArgs e)
        {
            if (e.Position != Vector2.Zero)
            {
                _targetRotation = (float)System.Math.Atan2(-e.Position.Y, e.Position.X);
                _rotationChanged = DateTime.Now;
            }
        }

        #endregion

        #endregion
    }
}
