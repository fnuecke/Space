using System;
using Engine.Session;
using Engine.Simulation.Commands;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.ComponentSystem.Systems;
using Space.Input;
using Space.Simulation.Commands;
using Space.Util;

namespace Space.Control
{
    /// <summary>
    /// Handles player input and converts it to simulation commands.
    /// </summary>
    internal sealed class InputHandler : GameComponent
    {
        #region Constants

        /// <summary>
        /// The interval in milliseconds in which to check for new rotation
        /// based on mouse movement.
        /// </summary>
        private const int AnalogPollInterval = 50;

        /// <summary>
        /// The interval in seconds in which to scan input devices.
        /// </summary>
        private const int InputDeviceScanInterval = 5;

        #endregion

        #region Properties

        /// <summary>
        /// Returns whether we are currently in a game.
        /// </summary>
        private bool IsConnected
        {
            get { return _game.Client != null && _game.Client.Controller.Session.ConnectionState == ClientState.Connected; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The game we belong to.
        /// </summary>
        private readonly Program _game;

        /// <summary>
        /// The current primary gamepad.
        /// </summary>
        private IGamePad _gamePad;

        /// <summary>
        /// The time at which we last scanned for input devices.
        /// </summary>
        private DateTime _lastInputDeviceScan;

        /// <summary>
        /// The time at which we last check whether the mouse had moved.
        /// </summary>
        private DateTime _lastUpdate;

        /// <summary>
        /// The current player acceleration direction.
        /// </summary>
        private Directions _accelerationDirection;

        /// <summary>
        /// The current player acceleration vector (converted from
        /// direction or read from gamepad).
        /// </summary>
        private Vector2 _accelerationVector;

        /// <summary>
        /// The last registered time that the mouse has moved.
        /// </summary>
        private DateTime _accelerationChanged;

        /// <summary>
        /// Whether we're currently stabilizing our position or not (avoid resends).
        /// </summary>
        private bool _stabilizing;

        /// <summary>
        /// Whether we're currently shooting (avoid resends).
        /// </summary>
        private bool _shooting;

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

        public InputHandler(Program game)
            : base(game)
        {
            _game = game;
        }

        #endregion

        #region Logic

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Rescan input devices periodically, e.g. to accept newly connected gamepads.
            if ((DateTime.UtcNow - _lastInputDeviceScan).TotalSeconds > InputDeviceScanInterval)
            {
                AttachListeners();
                _lastInputDeviceScan = DateTime.UtcNow;
            }

            // Disregard the rest if we're not connected.
            if (!IsConnected)
            {
                // Reset periodic update values.
                _accelerationDirection = Directions.None;
                _accelerationVector = Vector2.Zero;
                _accelerationChanged = DateTime.MinValue;
                _targetRotation = 0;
                _rotationChanged = DateTime.MinValue;
                _lastUpdate = DateTime.MinValue;
                return;
            }

            // Handle game pad input that we can't properly handle via events.
            if (Settings.Instance.EnableGamepad && _gamePad != null && _gamePad.IsAttached)
            {
                // Handle movement of the left stick, which controls our movement.
                var gamepadAcceleration = GamePadHelper.GetAcceleration(_gamePad);
                if (gamepadAcceleration != _previousGamepadAcceleration)
                {
                    _accelerationVector = gamepadAcceleration;
                    _accelerationChanged = DateTime.UtcNow;
                }
                _previousGamepadAcceleration = gamepadAcceleration;

                // Handle movement of the right stick, which controls our direction.
                var gamepadLook = GamePadHelper.GetLook(_gamePad);
                if (gamepadLook != _previousGamepadLook && gamepadLook != Vector2.Zero)
                {
                    _targetRotation = (float)Math.Atan2(gamepadLook.Y, gamepadLook.X);
                    _rotationChanged = DateTime.UtcNow;
                }
                _previousGamepadLook = gamepadLook;
            }

            // See if we want to re-orientate the ship and whether the acceleration
            // changed. Only check every so often, as slight delays here will not be
            // as noticeable, due to the ship's slow turn/acceleration speed.
            if ((DateTime.UtcNow - _lastUpdate).TotalMilliseconds > AnalogPollInterval)
            {
                // Has the mouse moved since the last update?
                if (_rotationChanged >= _lastUpdate)
                {
                    // Yes, push command.
                    _game.Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Rotate, new Vector2(_targetRotation, 0)));
                }
                // Has the acceleration changed since the last update?
                if (_accelerationChanged >= _lastUpdate)
                {
                    // Yes, push command.
                    _game.Client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Accelerate, _accelerationVector));
                }
                _lastUpdate = DateTime.UtcNow;
            }
        }

        #endregion

        #region Listener registration/removal

        /// <summary>
        /// Attach listeners to all known devices.
        /// </summary>
        private void AttachListeners()
        {
            // Detach first, to avoid multiple registrations.
            DetachListeners();

            var im = _game.InputManager;

            // Register for key presses and releases (movement).
            if (im.Keyboards != null)
            {
                foreach (var keyboard in im.Keyboards)
                {
                    if (keyboard.IsAttached)
                    {
                        keyboard.KeyPressed += HandleKeyPressed;
                        keyboard.KeyReleased += HandleKeyReleased;
                    }
                }
            }

            // Register for mouse movement (orientation) and buttons (shooting).
            if (im.Mice != null)
            {
                foreach (var mouse in im.Mice)
                {
                    if (mouse.IsAttached)
                    {
                        mouse.MouseMoved += HandleMouseMoved;
                        mouse.MouseButtonPressed += HandleMousePressed;
                        mouse.MouseButtonReleased += HandleMouseReleased;
                        mouse.MouseWheelRotated += HandleMouseWheelRotated;
                    }
                }
            }

            // Register for game pad buttons. Sticks are handled in update.
            _gamePad = null;
            if (Settings.Instance.EnableGamepad && im.GamePads != null)
            {
                foreach (var gamepad in im.GamePads)
                {
                    if (gamepad.IsAttached)
                    {
                        _gamePad = gamepad;
                        gamepad.ButtonPressed += HandleGamePadPressed;
                        gamepad.ButtonReleased += HandleGamePadReleased;

                        // Only use the first gamepad.
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Remove listeners from all known devices.
        /// </summary>
        private void DetachListeners()
        {
            var im = _game.InputManager;

            if (im.Keyboards != null)
            {
                foreach (var keyboard in im.Keyboards)
                {
                    if (keyboard.IsAttached)
                    {
                        keyboard.KeyPressed -= HandleKeyPressed;
                        keyboard.KeyReleased -= HandleKeyReleased;
                    }
                }
            }

            if (im.Mice != null)
            {
                foreach (var mouse in im.Mice)
                {
                    if (mouse.IsAttached)
                    {
                        mouse.MouseMoved -= HandleMouseMoved;
                        mouse.MouseButtonPressed -= HandleMousePressed;
                        mouse.MouseButtonReleased -= HandleMouseReleased;
                        mouse.MouseWheelRotated -= HandleMouseWheelRotated;
                    }
                }
            }

            _gamePad = null;
            if (im.GamePads != null)
            {
                foreach (var gamepad in im.GamePads)
                {
                    if (gamepad.IsAttached)
                    {
                        gamepad.ButtonPressed -= HandleGamePadPressed;
                        gamepad.ButtonReleased -= HandleGamePadReleased;
                    }
                }
            }
        }

        #endregion

        #region Keyboard

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        private void HandleKeyPressed(Keys key)
        {
            BeginCommand(Settings.Instance.GameBindings.GetCommand(key));
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(Keys key)
        {
            EndCommand(Settings.Instance.GameBindings.GetCommand(key));
        }

        #endregion

        #region Mouse

        /// <summary>
        /// Handle mouse presses.
        /// </summary>
        private void HandleMousePressed(MouseButtons buttons)
        {
            BeginCommand(Settings.Instance.GameBindings.GetCommand(buttons));
        }

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        private void HandleMouseReleased(MouseButtons buttons)
        {
            EndCommand(Settings.Instance.GameBindings.GetCommand(buttons));
        }

        /// <summary>
        /// Handle mouse wheel rotation.
        /// </summary>
        /// <param name="ticks"></param>
        private void HandleMouseWheelRotated(float ticks)
        {
            BeginCommand(Settings.Instance.GameBindings.GetCommand(ticks > 0 ? MouseWheel.Up : MouseWheel.Down));
        }

        /// <summary>
        /// Update facing direction on mouse move.
        /// </summary>
        private void HandleMouseMoved(float x, float y)
        {
            // Get angle to middle of screen (position of our ship), which
            // will be our new target rotation.
            var rx = x - _game.GraphicsDevice.Viewport.Width / 2f;
            var ry = y - _game.GraphicsDevice.Viewport.Height / 2f;
            _targetRotation = (float)Math.Atan2(ry, rx);
            _rotationChanged = DateTime.UtcNow;
        }

        #endregion

        #region Gamepad

        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        private void HandleGamePadPressed(Buttons buttons)
        {
            BeginCommand(Settings.Instance.GameBindings.GetCommand(buttons));
        }

        /// <summary>
        /// Handle game pad key releases.
        /// </summary>
        private void HandleGamePadReleased(Buttons buttons)
        {
            EndCommand(Settings.Instance.GameBindings.GetCommand(buttons));
        }

        #endregion

        #region Command handling
        
        private void BeginCommand(GameCommand gameCommand)
        {
            // Figure out what to do.
            FrameCommand command = null;
            switch (gameCommand)
            {
                case GameCommand.Up:
                    // Started accelerating upwards, update directions if something changed.
                    AddAccelerationDirection(Directions.Up);
                    break;

                case GameCommand.Down:
                    // Started accelerating downwards, update directions if something changed.
                    AddAccelerationDirection(Directions.Down);
                    break;

                case GameCommand.Left:
                    // Started accelerating leftwards, update directions if something changed.
                    AddAccelerationDirection(Directions.Left);
                    break;

                case GameCommand.Right:
                    // Started accelerating rightwards, update directions if something changed.
                    AddAccelerationDirection(Directions.Right);
                    break;

                case GameCommand.Stabilize:
                    // Enable stabilizers if not toggling.
                    if (!Settings.Instance.StabilizeToggles && !_stabilizing)
                    {
                        command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginStabilizing);
                        _stabilizing = true;
                    }
                    break;

                case GameCommand.ZoomIn:
                    // Zoom camera in.
                    if (IsConnected)
                    {
                        _game.Client.GetSystem<CameraSystem>().ZoomIn();
                    }
                    break;

                case GameCommand.ZoomOut:
                    // Zoom camera out.
                    if (IsConnected)
                    {
                        _game.Client.GetSystem<CameraSystem>().ZoomOut();
                    }
                    break;

                case GameCommand.Shoot:
                    // Shoot.
                    if (!_shooting)
                    {
                        command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting);
                        _shooting = true;
                    }
                    break;

                case GameCommand.Use:
                    // TODO
                    break;

                case GameCommand.PickUp:
                    // Pick up nearby items.
                    command = new PickUpCommand();
                    break;

                case GameCommand.Back:
                    // TODO
                    break;

                case GameCommand.Menu:
                    // TODO
                    break;

                case GameCommand.Inventory:
                    // TODO
                    break;

                case GameCommand.Character:
                    // TODO
                    break;

                case GameCommand.ToggleGraphs:
                    _game.GraphsVisible = !_game.GraphsVisible;
                    break;

                case GameCommand.Console:
                    // Toggle console.
                    _game.GameConsole.IsOpen = !_game.GameConsole.IsOpen;
                    break;
            }

            // If we did something, push it.
            if (command != null && IsConnected)
            {
                _game.Client.Controller.PushLocalCommand(command);
            }
        }

        private void EndCommand(GameCommand gameCommand)
        {
            // Figure out what to do.
            FrameCommand command = null;
            switch (gameCommand)
            {
                case GameCommand.Up:
                    // Stopped accelerating upwards, update directions if something changed.
                    RemoveAccelerationDirection(Directions.Up);
                    break;

                case GameCommand.Down:
                    // Stopped accelerating downwards, update directions if something changed.
                    RemoveAccelerationDirection(Directions.Down);
                    break;

                case GameCommand.Left:
                    // Stopped accelerating leftwards, update directions if something changed.
                    RemoveAccelerationDirection(Directions.Left);
                    break;

                case GameCommand.Right:
                    // Stopped accelerating rightwards, update directions if something changed.
                    RemoveAccelerationDirection(Directions.Right);
                    break;

                case GameCommand.Stabilize:
                    // Stopped stabilizing, should we toggle?
                    if (Settings.Instance.StabilizeToggles)
                    {
                        // Toggle stabilizers.
                        command = new PlayerInputCommand(_stabilizing ? PlayerInputCommand.PlayerInputCommandType.BeginStabilizing : PlayerInputCommand.PlayerInputCommandType.StopStabilizing);
                        _stabilizing = !_stabilizing;
                    }
                    else
                    {
                        // Disable stabilizers if not toggling.
                        command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopStabilizing);
                        _stabilizing = false;
                    }
                    break;

                case GameCommand.Shoot:
                    // Stop shooting.
                    if (_shooting)
                    {
                        command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting);
                        _shooting = false;
                    }
                    break;
            }

            // If we did something, push it.
            if (command != null && IsConnected)
            {
                _game.Client.Controller.PushLocalCommand(command);
            }
        }

        /// <summary>
        /// Begin accelerating in the specified direction.
        /// </summary>
        /// <param name="direction">The direcion to begin accelerating into.</param>
        private void AddAccelerationDirection(Directions direction)
        {
            if ((_accelerationDirection & direction) == Directions.None)
            {
                _accelerationDirection = (_accelerationDirection | direction);
                _accelerationVector = DirectionConversion.DirectionToVector(_accelerationDirection);
                _accelerationChanged = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Stop accelerating in the specified direction.
        /// </summary>
        /// <param name="direction">The direcion to stop accelerating into.</param>
        private void RemoveAccelerationDirection(Directions direction)
        {
            if ((_accelerationDirection & direction) != Directions.None)
            {
                _accelerationDirection = (_accelerationDirection & ~direction);
                _accelerationVector = DirectionConversion.DirectionToVector(_accelerationDirection);
                _accelerationChanged = DateTime.UtcNow;
            }
        }

        #endregion
    }
}
