using System;
using Engine.Input;
using Engine.Util;
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

        private float _previousTargetRotation;

        private float _currentTargetRotation;

        private float _shipTargetRotation;

        private bool _mouseStoppedMoving = true;

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
            // This test is necessary to figure out when player has stopped
            // moving his mouse, so we can send a finalizing rotation command.
            // Otherwise the ship might stop midway in our turn, not reaching
            // the actual target we currently want. This is because we only
            // send rotation commands when we really have to (specifically:
            // we don't send commands that would only update the target angle,
            // but not the direction), which saves us quite a few commands,
            // and thus net traffic.
            if (!_mouseStoppedMoving)
            {
                // We stopped moving when last and current position are equal.
                if (_previousTargetRotation == _currentTargetRotation)
                {
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Rotate, _currentTargetRotation));
                    _shipTargetRotation = _currentTargetRotation;
                    _mouseStoppedMoving = true;
                }
                _previousTargetRotation = _currentTargetRotation;
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
            UpdateTargetRotation(mouseAngle);
        }

        // TODO private void HandleGamePadStickMoved(object sender, EventArgs e) { update rotation }

        /// <summary>
        /// This is the part of the base functionality for updating the direction
        /// we're facing.
        /// </summary>
        /// <param name="targetRotation">the new direction to face.</param>
        private void UpdateTargetRotation(float targetRotation)
        {
            var info = _client.GetPlayerShipInfo();
            if (info != null)
            {
                var transform = info.Position;
                var spin = info.RotationSpeed;

                // Get ships current orientation.
                double shipAngle =info.Rotation;

                // Remember where we'd like to rotate to (for finalizing).
                _currentTargetRotation = targetRotation;

                // Get the smaller angle between our current and our target angles.
                double deltaAngle = Angle.MinAngle(shipAngle, targetRotation);

                // Remaining rotation the ship has to perform.
                double remainingAngle = Angle.MinAngle(shipAngle, _shipTargetRotation);

                // Now, if the difference to our current rotation is large enough
                // and we're either rotating in the other direction or not at all,
                // we send a rotation command.
                // If we're rotating in that direction already, we DON'T! This is
                // the exact reason for why we need to finalize our rotations by
                // checking when the mouse stops moving. But we can save ourselves
                // a lot of superfluous input commands this way, reducing network
                // load somewhat (still pretty bad if user moves his mouse slowly,
                // but meh).
                if ((deltaAngle > 10e-3 && spin <= 0) ||
                    (deltaAngle < -10e-3 && spin >= 0) ||
                    (Math.Abs(remainingAngle) < spin))
                {
                    _client.Controller.PushLocalCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInputCommandType.Rotate, _currentTargetRotation));
                    _shipTargetRotation = _currentTargetRotation;
                }

                // Set our flag to remember we might have to finalize the movement.
                _mouseStoppedMoving = false;
            }
        }

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
