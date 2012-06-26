using System;
using System.Linq;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.Input;
using Space.Simulation.Commands;
using Space.Util;

namespace Space.Control
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
        private const int MousePollInterval = 50;

        #endregion

        #region Properties

        /// <summary>
        /// The game client to inject commands into.
        /// </summary>
        public GameClient Client
        {
            private get { return _client; }
            set
            {
                DetachListeners();
                _client = value;
                if (_client != null)
                {
                    _keyboard = ((IKeyboard)Client.Game.Services.GetService(typeof(IKeyboard)));
                    _mouse = ((IMouse)Client.Game.Services.GetService(typeof(IMouse)));
                    _gamepad = ((IGamePad)Client.Game.Services.GetService(typeof(IGamePad)));
                    _client.Controller.Session.JoinResponse += (s, e) => AttachListeners();
                    _client.Controller.Session.Disconnecting += (s, e) => DetachListeners();
                    _client.Controller.Session.Disconnected += (s, e) => DetachListeners();
                }
            }
        }

        #endregion

        #region Fields

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

        /// <summary>
        /// The game client we're currently handling input for.
        /// </summary>
        private GameClient _client;

        #endregion

        /// <summary>
        /// Set whether to accept player input or not.
        /// </summary>
        private void AttachListeners()
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
                _mouse.MouseWheelRotated += HandleMouseWheelRotated;
            }

            // Register for game pad buttons. Sticks are handled in update.
            if (_gamepad != null)
            {
                _gamepad.ButtonPressed += HandleGamePadPressed;
                _gamepad.ButtonReleased += HandleGamePadReleased;
            }
        }

        private void DetachListeners()
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
                _mouse.MouseWheelRotated -= HandleMouseWheelRotated;
            }

            if (_gamepad != null)
            {
                _gamepad.ButtonPressed -= HandleGamePadPressed;
                _gamepad.ButtonReleased -= HandleGamePadReleased;
            }
        }

        #region Logic

        public void Update()
        {
            if (Client == null || Client.Controller.Session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            // Handle game pad input that we can't properly handle via events.
            if (Settings.Instance.EnableGamepad && _gamepad != null)
            {
                // Handle movement of the left stick, which controls our movement.
                var gamepadAcceleration = GamePadHelper.GetAcceleration(_gamepad);
                if (gamepadAcceleration != _previousGamepadAcceleration)
                {
                    _accelerationDirection = gamepadAcceleration;
                    _accelerationDirection.Y = -_accelerationDirection.Y;
                    _accelerationChanged = DateTime.Now;
                }
                _previousGamepadAcceleration = gamepadAcceleration;

                // Handle movement of the right stick, which controls our direction.
                var gamepadLook = GamePadHelper.GetLook(_gamepad);
                if (gamepadLook != _previousGamepadLook && gamepadLook != Vector2.Zero)
                {
                    _targetRotation = (float)Math.Atan2(gamepadLook.Y, gamepadLook.X);
                    _rotationChanged = DateTime.Now;
                }
                _previousGamepadLook = gamepadLook;
            }

            // Only check every so often, as slight delays here will not be as
            // noticeable, due to the ship's slow turn speed.
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds > MousePollInterval)
            {
                // Has the mouse moved since the last update?
                if (_rotationChanged >= _lastUpdate)
                {
                    // Yes, push command.
                    Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Rotate, new Vector2(_targetRotation, 0)));
                }
                if (_accelerationChanged >= _lastUpdate)
                {
                    Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Accelerate, _accelerationDirection));
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
        private void HandleKeyPressed(Keys key)
        {
            if (Settings.Instance.GameBindings.ContainsKey(key))
            {
                switch (Settings.Instance.GameBindings[key])
                {
                    case Settings.GameCommand.Stabilize:
                        if (Settings.Instance.ToggleStabilize)
                        {
                            // Toggle stabilizers.
                            _stabilizing = !_stabilizing;
                            Client.Controller.PushLocalCommand(new PlayerInputCommand(_stabilizing ? PlayerInputCommand.PlayerInputCommandType.BeginStabilizing : PlayerInputCommand.PlayerInputCommandType.StopStabilizing));
                        }
                        else
                        {
                            // Just enable stabilizers.
                            Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginStabilizing));
                        }
                        break;
                    case Settings.GameCommand.PickUp:
                        Client.Controller.PushLocalCommand(new PickUpCommand());
                        break;
                    default:
                        UpdateKeyboardAcceleration();
                        break;
                }
            }
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(Keys key)
        {
            if (Settings.Instance.GameBindings.ContainsKey(key))
            {
                switch (Settings.Instance.GameBindings[key])
                {
                    case Settings.GameCommand.Stabilize:
                        if (!Settings.Instance.ToggleStabilize)
                        {
                            // Disable stabilizers if not toggling.
                            Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopStabilizing));
                        }
                        break;
                    default:
                        UpdateKeyboardAcceleration();
                        break;
                }
            }
        }

        /// <summary>
        /// Updates acceleration direction based on key presses.
        /// </summary>
        private void UpdateKeyboardAcceleration()
        {
            var state = _keyboard.GetState();
            var direction = Directions.None;
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Down].Any(state.IsKeyDown))
            {
                // Accelerate downwards.
                direction |= Directions.Down;
            }
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Left].Any(state.IsKeyDown))
            {
                // Accelerate left.
                direction |= Directions.Left;
            }
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Right].Any(state.IsKeyDown))
            {
                // Accelerate right.
                direction |= Directions.Right;
            }
            if (Settings.Instance.InverseGameBindings[Settings.GameCommand.Up].Any(state.IsKeyDown))
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
        private void HandleMousePressed(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting));
            }
        }

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        private void HandleMouseReleased(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
            }
        }

        /// <summary>
        /// Update facing direction on mouse move.
        /// </summary>
        private void HandleMouseMoved(float x, float y)
        {
            // Get angle to middle of screen (position of our ship), which
            // will be our new target rotation.
            var rx = x - Client.Game.GraphicsDevice.Viewport.Width / 2f;
            var ry = y - Client.Game.GraphicsDevice.Viewport.Height / 2f;
            _targetRotation = (float)Math.Atan2(ry, rx);
            _rotationChanged = DateTime.Now;
        }

        /// <summary>
        /// Update zoom on mouse wheel rotation.
        /// </summary>
        /// <param name="ticks"></param>
        private void HandleMouseWheelRotated(float ticks)
        {
            if (ticks > 0)
            {
                Client.GetCameraSystem().ZoomIn();
            }
            else
            {
                Client.GetCameraSystem().ZoomOut();
            }
            
        }

        #endregion

        #region Gamepad

        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        private void HandleGamePadPressed(Buttons buttons)
        {
            switch (buttons)
            {
                case (Buttons.RightShoulder):
                    // Shoot.
                    Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting));
                    break;
            }
        }

        /// <summary>
        /// Handle game pad key releases.
        /// </summary>
        private void HandleGamePadReleased(Buttons buttons)
        {
            switch (buttons)
            {
                case (Buttons.RightShoulder):
                    // Stop shooting.
                    Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
                    break;
            }
        }

        #endregion

        #endregion
    }
}
