using System;
using Engine.Commands;
using Engine.Controller;
using Engine.Input;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Space.Commands;
using Space.Model;

namespace Space.Control
{
    class InputCommandEmitter : AbstractCommandEmitter<IFrameCommand<GameCommandType, PlayerInfo, PacketizerContext>, GameCommandType, PlayerInfo, PacketizerContext>
    {
        private IClientSession<PlayerInfo, PacketizerContext> Session;

        public InputCommandEmitter(Game game, IClientSession<PlayerInfo, PacketizerContext> session)
        {
            this.Session = session;

            var keyboard = (IKeyboardInputManager)game.Services.GetService(typeof(IKeyboardInputManager));
            keyboard.Pressed += HandleKeyPressed;
            keyboard.Released += HandleKeyReleased;
        }

        //*
        /// <summary>
        /// Player pressed a key.
        /// </summary>
        private void HandleKeyPressed(object sender, EventArgs e)
        {
            if (Session.ConnectionState != ClientState.Connected)
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

                case Keys.Q:
                    // Rotate to the left.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.TurnLeft);
                    break;
                case Keys.E:
                    // Rotate to the right.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.TurnRight);
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
            if (Session.ConnectionState != ClientState.Connected)
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

                case Keys.Q:
                    // Stop rotating left.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopTurnLeft);
                    break;
                case Keys.E:
                    // Stop rotating right.
                    command = new PlayerInputCommand(PlayerInputCommand.PlayerInput.StopTurnRight);
                    break;

                default:
                    return;
            }

            OnCommand(command);
        }

        /*
        protected override void HandleMouseMoved(object sender, EventArgs e)
        {
            if (Session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            return;

            var args = (MouseInputEventArgs)e;

            Ship ship = (Ship)simulation.Get(Session.LocalPlayer.Data.ShipUID);
            if (ship != null)
            {
                // Get ships current orientation.
                double shipAngle = ship.Rotation.DoubleValue;
                // Get angle to middle of screen (position of our ship).
                int rx = args.X - GraphicsDevice.Viewport.Width / 2;
                int ry = args.Y - GraphicsDevice.Viewport.Height / 2;
                double mouseAngle = System.Math.Atan2(ry, rx);

                Console.WriteLine(rx + ", " + ry + ", " + mouseAngle + ", " + shipAngle);

                double deltaAngle = mouseAngle - shipAngle;
                const double pi2 = System.Math.PI * 2;
                deltaAngle += (deltaAngle > System.Math.PI) ? -pi2 : (deltaAngle < -System.Math.PI) ? pi2 : 0;

                if (deltaAngle > 10e-3 || deltaAngle < -10e-3)
                {
                    targetAngle = Fixed.Create(shipAngle + deltaAngle);
                    lastAngle = ship.Rotation;
                    if (deltaAngle > 0)
                    {
                        // Rotate right.
                        var command = new PlayerInputCommand(Session.LocalPlayer,
                            simulation.CurrentFrame + 1,
                            PlayerInputCommand.PlayerInput.StopTurnLeft);
                        Apply(command, 30);
                        command = new PlayerInputCommand(Session.LocalPlayer,
                            simulation.CurrentFrame + 1,
                            PlayerInputCommand.PlayerInput.TurnRight);
                        Apply(command, 30);
                        mouseRotating = true;
                    }
                    else
                    {
                        // Rotate left.
                        var command = new PlayerInputCommand(Session.LocalPlayer,
                            simulation.CurrentFrame + 1,
                            PlayerInputCommand.PlayerInput.TurnLeft);
                        Apply(command, 30);
                        command = new PlayerInputCommand(Session.LocalPlayer,
                            simulation.CurrentFrame + 1,
                            PlayerInputCommand.PlayerInput.StopTurnRight);
                        Apply(command, 30);
                        mouseRotating = true;
                    }
                }
                else
                {
                    // Stop rotating.
                    var command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopTurnLeft);
                    Apply(command, 30);
                    command = new PlayerInputCommand(Session.LocalPlayer,
                        simulation.CurrentFrame + 1,
                        PlayerInputCommand.PlayerInput.StopTurnRight);
                    Apply(command, 30);
                    mouseRotating = false;
                }
            }
        }
        //*/
    }
}
