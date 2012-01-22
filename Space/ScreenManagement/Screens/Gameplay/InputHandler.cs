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
        private Directions _accelerationDirection;

        private Vector2 _directdirection;
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
            _gamepad = (IGamepadInputManager) client.Game.Services.GetService(typeof (IGamepadInputManager));
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
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Rotate, new Vector2(_targetRotation,0)));
                }
                if (_accelerationChanged > _lastUpdate)
                {
                    if(_directdirection != Vector2.Zero)
                        _directdirection.Normalize();
                    var dir = DirectionConversion.DirectionToVector(_accelerationDirection);
                    if(dir != Vector2.Zero)
                        dir.Normalize();
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Accelerate,_directdirection+ dir));
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

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        private void HandleKeyPressed(object sender, EventArgs e)
        {
            var args = (KeyboardInputEventArgs)e;

            if (args.IsRepeat)
            {
                return;
            }

            if (args.Key == Settings.Instance.MoveDown)
            {
                // Accelerate downwards.
                _accelerationDirection |= Directions.South;
            }
            else if (args.Key == Settings.Instance.MoveLeft)
            {
                // Accelerate left.
                _accelerationDirection |= Directions.West;
            }
            else if (args.Key == Settings.Instance.MoveRight)
            {
                // Accelerate right.
                _accelerationDirection |= Directions.East;
            }
            else if (args.Key == Settings.Instance.MoveUp)
            {
                // Accelerate upwards.
                _accelerationDirection |= Directions.North;
            }
            else
            {
                // Not an acceleration command, return after this, to avoid
                // setting the timestamp.
                if (args.Key == Settings.Instance.Stabilizer)
                {
                    // Toggle stabilizers.
                    _stabilizing = !_stabilizing;
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(_stabilizing ? PlayerInputCommand.PlayerInputCommandType.BeginStabilizing : PlayerInputCommand.PlayerInputCommandType.StopStabilizing));
                }
                return;
            }

            _accelerationChanged = DateTime.Now;
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(object sender, EventArgs e)
        {
            var args = (KeyboardInputEventArgs)e;

            if (args.Key == Settings.Instance.MoveDown)
            {
                _accelerationDirection &= ~Directions.South;
            }
            else if (args.Key == Settings.Instance.MoveLeft)
            {
                _accelerationDirection &= ~Directions.West;
            }
            else if (args.Key == Settings.Instance.MoveRight)
            {
                _accelerationDirection &= ~Directions.East;
            }
            else if (args.Key == Settings.Instance.MoveUp)
            {
                _accelerationDirection &= ~Directions.North;
            }
            else
            {
                return;
            }

            _accelerationChanged = DateTime.Now;
        }

        private void HandleMouseMoved(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;

            // Get angle to middle of screen (position of our ship), which
            // will be our new target rotation.
            int rx = args.X - _client.Game.GraphicsDevice.Viewport.Width / 2;
            int ry = args.Y - _client.Game.GraphicsDevice.Viewport.Height / 2;
            var mouseAngle = (float)System.Math.Atan2(ry, rx);

            _targetRotation = mouseAngle;
            _rotationChanged = DateTime.Now;
        }

        // TODO private void HandleGamePadStickMoved(object sender, EventArgs e) { update rotation }
        private void HandleGamePadPressed(object sender,EventArgs e)
        {
            var args = (GamePadInputEventArgs)e;
            if (args.IsRepeat)
            {
                return;
            }
            switch (args.Buttons)
            {
                case (Buttons.LeftThumbstickUp):
                case (Buttons.LeftThumbstickLeft):
                case (Buttons.LeftThumbstickDown):
                case (Buttons.LeftThumbstickRight):
                case (Buttons.RightThumbstickUp):
                case (Buttons.RightThumbstickLeft):
                case (Buttons.RightThumbstickDown):
                case (Buttons.RightThumbstickRight):    
                    break;
                case (Buttons.RightShoulder):
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting));
                    break;
                default:
                    break;
                    
            }
            _accelerationChanged = DateTime.Now;
        }
        private void HandleGamePadReleased(object sender, EventArgs e)
        {
            var args = (GamePadInputEventArgs)e;

            switch (args.Buttons)
            {
                case (Buttons.LeftThumbstickUp):
                case (Buttons.LeftThumbstickLeft):
                case (Buttons.LeftThumbstickDown):
                case (Buttons.LeftThumbstickRight):
                case (Buttons.RightThumbstickUp):
                case (Buttons.RightThumbstickLeft):
                case (Buttons.RightThumbstickDown):
                case (Buttons.RightThumbstickRight): 
                    
                    break;
                case (Buttons.RightShoulder):
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
                    break;
                default:
                    break;

            }
            
        }

        private void HandleGamePadLeftMoved(object sender, EventArgs e)
        {
            var args = (GamePadInputEventArgs) e;
            _directdirection = args.State.ThumbSticks.Left;
            _directdirection.Y = -_directdirection.Y;
            _accelerationChanged = DateTime.Now;
        }
        private void HandleGamePadRightMoved(object sender, EventArgs e)
        {
            var args = (GamePadInputEventArgs)e;
            var rightstick = args.State.ThumbSticks.Right;
            if (rightstick != Vector2.Zero)
            {


                var mouseAngle =
                    (float) System.Math.Atan2(-rightstick.Y, rightstick.X);
                _targetRotation = mouseAngle;
            }
            _rotationChanged = DateTime.Now;
        }
        private void HandleMousePressed(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;

            if (args.Button == MouseInputEventArgs.MouseButton.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.BeginShooting));
            }
        }

        private void HandleMouseReleased(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;

            if (args.Button == MouseInputEventArgs.MouseButton.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopShooting));
            }
        }

        #endregion
    }
}
