using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    /// <summary>
    /// This class may be used to get an event driven access to user game pad
    /// input.
    /// 
    /// Upon creation, this class registers itself as a service with the game it
    /// is created for, so it can be accessed by any other component.
    /// </summary>
    public sealed class GamepadInputManager : GameComponent, IGamepadInputManager
    {
        #region Events

        /// <summary>
        /// Fired when a button is newly pressed.
        /// </summary>
        public event EventHandler<EventArgs> Pressed;

        /// <summary>
        /// Fired when a button is released.
        /// </summary>
        public event EventHandler<EventArgs> Released;

        /// <summary>
        /// Fired when the left game pad stick was moved.
        /// </summary>
        public event EventHandler<EventArgs> LeftMoved;

        /// <summary>
        /// Fired when the right game pad stick was moved.
        /// </summary>
        public event EventHandler<EventArgs> RightMoved;

        #endregion

        #region Properties

        /// <summary>
        /// State from the last update, to check for changes.
        /// </summary>
        private GamePadState _previousState;

        #endregion

        public GamepadInputManager(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IGamepadInputManager), this);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!Game.IsActive)
            {
                return;
            }

            // The current time, used to check for repeats.
            var currentState = GamePad.GetState((PlayerIndex)0);
            var buttons = Enum.GetValues(typeof(Buttons));
            foreach (Buttons button in buttons)
            {
                if (currentState.IsButtonDown(button))
                {
                    if (!_previousState.IsButtonDown(button))
                    {
                        // Key was pressed.
                        OnPressed(new GamePadInputEventArgs(currentState, button));
                    }
                }
                else
                {
                    if (_previousState.IsButtonDown(button))
                    {
                        OnReleased(new GamePadInputEventArgs(currentState, button));
                    }
                }
            }

            if (currentState.ThumbSticks.Left != _previousState.ThumbSticks.Left)
            {
                OnLeftMove(new GamePadInputEventArgs(currentState, Buttons.LeftStick, currentState.ThumbSticks.Left));
            }

            if (currentState.ThumbSticks.Right != _previousState.ThumbSticks.Right)
            {
                OnRightMove(new GamePadInputEventArgs(currentState, Buttons.RightStick, currentState.ThumbSticks.Right));
            }

            _previousState = currentState;
        }

        private void OnPressed(GamePadInputEventArgs e)
        {
            if (Pressed != null)
            {
                Pressed(this, e);
            }
        }

        private void OnReleased(GamePadInputEventArgs e)
        {
            if (Released != null)
            {
                Released(this, e);
            }
        }

        private void OnLeftMove(GamePadInputEventArgs e)
        {
            if (LeftMoved != null)
            {
                LeftMoved(this, e);
            }
        }

        private void OnRightMove(GamePadInputEventArgs e)
        {
            if (RightMoved != null)
            {
                RightMoved(this, e);
            }
        }
    }
}
