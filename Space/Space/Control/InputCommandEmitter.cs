using System;
using Engine.Commands;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Systems;
using Engine.Controller;
using Engine.Input;
using Engine.Math;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Space.Commands;

namespace Space.Control
{
    class InputCommandEmitter : GameComponent, ICommandEmitter<IFrameCommand>
    {
        /// <summary>
        /// Event dispatched whenever a new command was generated. This command
        /// will be injected into the simulation at it's current frame.
        /// 
        /// The dispatched events must be of type <c>CommandEmittedEventArgs</c>,
        /// with the proper generics as to match the controller it'll be registered
        /// with.
        /// </summary>
        public event CommandEmittedEventHandler<IFrameCommand> CommandEmitted;

        public bool IsEnabled { get; set; }

        private IClientSession _session;
        private ISimulation _simulation;
        private Fixed _previousTargetRotation;
        private Fixed _currentTargetRotation;
        private bool _rotationFinished = true;

        public InputCommandEmitter(Game game, IClientSession session, ISimulation simulation)
            : base(game)
        {
            this._session = session;
            this._simulation = simulation;
            
            // Register for key presses and releases (movement).
            var keyboard = (IKeyboardInputManager)game.Services.GetService(typeof(IKeyboardInputManager));
            keyboard.Pressed += HandleKeyPressed;
            keyboard.Released += HandleKeyReleased;

            // Register for mouse movement (orientation) and buttons (shooting).
            var mouse = (IMouseInputManager)game.Services.GetService(typeof(IMouseInputManager));
            mouse.Moved += HandleMouseMoved;
            mouse.Pressed += HandleMousePressed;
            mouse.Released += HandleMouseReleased;
        }

        // TODO waiting for completion to re-fire causes mini-lags, which feel awkward...
        public override void Update(GameTime gameTime)
        {
            if (_session.ConnectionState == ClientState.Connected)
            {
                // This test is necessary to figure out when player has stopped
                // moving his mouse, so we can send a finalizing rotation command.
                // Otherwise the ship might stop midway in our turn, not reaching
                // the actual target we currently want. This is because we only
                // send rotation commands when we really have to (specifically:
                // we don't send commands that would only update the target angle,
                // but not the direction), which saves us quite a few commands,
                // and thus net traffic.
                if (!_rotationFinished)
                {
                    // We stopped moving when last and current position are equal.
                    if (_previousTargetRotation == _currentTargetRotation)
                    {
                        OnCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInput.Rotate, _currentTargetRotation));
                        _rotationFinished = true;
                    }
                    _previousTargetRotation = _currentTargetRotation;
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Player pressed a key.
        /// </summary>
        private void HandleKeyPressed(object sender, EventArgs e)
        {
            if (_session.ConnectionState != ClientState.Connected)
            {
                return;
            }

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
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.AccelerateDown);
                    break;
                case Keys.Left:
                case Keys.A:
                    // Accelerate left.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.AccelerateLeft);
                    break;
                case Keys.Right:
                case Keys.D:
                    // Accelerate right.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.AccelerateRight);
                    break;
                case Keys.Up:
                case Keys.W:
                    // Accelerate upwards.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.AccelerateUp);
                    break;

                default:
                    return;
            }

            OnCommand(command);
        }

        /// <summary>
        /// Player released a key.
        /// </summary>
        private void HandleKeyReleased(object sender, EventArgs e)
        {
            if (_session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            var args = (KeyboardInputEventArgs)e;

            PlayerInputCommand command = null;
            switch (args.Key)
            {
                case Keys.Down:
                case Keys.S:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopDown);
                    break;
                case Keys.Left:
                case Keys.A:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopLeft);
                    break;
                case Keys.Right:
                case Keys.D:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopRight);
                    break;
                case Keys.Up:
                case Keys.W:
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopUp);
                    break;

                default:
                    return;
            }

            OnCommand(command);
        }

        private void HandleMouseMoved(object sender, EventArgs e)
        {
            if (_session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            var args = (MouseInputEventArgs)e;

            // Get angle to middle of screen (position of our ship), which
            // will be our new target rotation.
            int rx = args.X - Game.GraphicsDevice.Viewport.Width / 2;
            int ry = args.Y - Game.GraphicsDevice.Viewport.Height / 2;
            double mouseAngle = System.Math.Atan2(ry, rx);
            UpdateTargetRotation(mouseAngle);
        }

        // TODO private void HandleGamePadStickMoved(object sender, EventArgs e) { update rotation }

        /// <summary>
        /// This is the part of the base functionality for updating the direction
        /// we're facing.
        /// </summary>
        /// <param name="targetRotation">the new direction to face.</param>
        private void UpdateTargetRotation(double targetRotation)
        {
            IEntity avatar = GetLocalAvatar();
            if (avatar != null)
            {
                var transform = avatar.GetComponent<Transform>();
                var spin = avatar.GetComponent<Spin>();

                // Get ships current orientation.
                double shipAngle = (double)transform.Rotation;

                // Remember where we'd like to rotate to (for finalizing).
                _currentTargetRotation = Fixed.Create(targetRotation);

                // Get the smaller angle between our current and our target angles.
                double deltaAngle = Angle.MinAngle(shipAngle, targetRotation);

                // Now, if the difference to our current rotation is large enough
                // and we're either rotating in the other direction or not at all,
                // we send a rotation command.
                // If we're rotating in that direction already, we DON'T! This is
                // the exact reason for why we need to finalize our rotations by
                // checking when the mouse stops moving. But we can save ourselves
                // a lot of superfluous input commands this way, reducing network
                // load somewhat (still pretty bad if user moves his mouse slowly,
                // but meh).
                if ((deltaAngle > 10e-3 && spin.Value <= Fixed.Zero) ||
                    (deltaAngle < -10e-3 && spin.Value >= Fixed.Zero))
                {
                    OnCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInput.Rotate, _currentTargetRotation));
                }

                // Set our flag to remember we might have to finalize the movement.
                _rotationFinished = false;
            }
        }

        void HandleMousePressed(object sender, EventArgs e)
        {
            if (_session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            var args = (MouseInputEventArgs)e;

            if (args.Button == MouseInputEventArgs.MouseButton.Left)
            {
                OnCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInput.Shoot));
            }
        }

        void HandleMouseReleased(object sender, EventArgs e)
        {
            if (_session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            var args = (MouseInputEventArgs)e;

            if (args.Button == MouseInputEventArgs.MouseButton.Left)
            {
                OnCommand(new PlayerInputCommand(PlayerInputCommand.PlayerInput.CeaseFire));
            }
        }

        private IEntity GetLocalAvatar()
        {
            return _simulation.SystemManager.GetSystem<AvatarSystem>().GetAvatar(_session.LocalPlayer.Number);
        }

        /// <summary>
        /// Use this to dispatch new command events.
        /// </summary>
        /// <param name="e">the command that was generated.</param>
        protected void OnCommand(GameCommand command)
        {
            if (CommandEmitted != null)
            {
                CommandEmitted(command);
            }
        }
    }
}
