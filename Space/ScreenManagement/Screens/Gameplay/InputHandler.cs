using System;
using Engine.Input;
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
        /// The last registered time that the mouse has moved.
        /// </summary>
        private DateTime _rotationChanged;

        /// <summary>
        /// The target rotation based on the current mouse position.
        /// </summary>
        private float _targetRotation;

        #endregion

        #region Constructor
        
        public InputHandler(GameClient client)
        {
            _client = client;

            _keyboard = (IKeyboardInputManager)client.Game.Services.GetService(typeof(IKeyboardInputManager));
            _mouse = (IMouseInputManager)client.Game.Services.GetService(typeof(IMouseInputManager));
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
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Rotate, _targetRotation));
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

            PlayerInputCommand command = null;
            switch (args.Key)
            {
                case Keys.Down:
                case Keys.S:
                    // Accelerate downwards.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.AccelerateDown);
                    break;
                case Keys.Left:
                case Keys.A:
                    // Accelerate left.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.AccelerateLeft);
                    break;
                case Keys.Right:
                case Keys.D:
                    // Accelerate right.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.AccelerateRight);
                    break;
                case Keys.Up:
                case Keys.W:
                    // Accelerate upwards.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.AccelerateUp);
                    break;

                default:
                    return;
            }

            _client.Controller.PushLocalCommand(command);
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(object sender, EventArgs e)
        {
            var args = (KeyboardInputEventArgs)e;

            PlayerInputCommand command = null;
            switch (args.Key)
            {
                case Keys.Down:
                case Keys.S:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopDown);
                    break;
                case Keys.Left:
                case Keys.A:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopLeft);
                    break;
                case Keys.Right:
                case Keys.D:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopRight);
                    break;
                case Keys.Up:
                case Keys.W:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.StopUp);
                    break;

                default:
                    return;
            }

            _client.Controller.PushLocalCommand(command);
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

        private void HandleMousePressed(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;

            if (args.Button == MouseInputEventArgs.MouseButton.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Shoot));
            }
        }

        private void HandleMouseReleased(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;

            if (args.Button == MouseInputEventArgs.MouseButton.Left)
            {
                _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.CeaseFire));
            }
        }

        #endregion
    }
}
