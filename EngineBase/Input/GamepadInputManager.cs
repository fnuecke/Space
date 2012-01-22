using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    public class GamepadInputManager :GameComponent, IGamepadInputManager
    {

          #region Properties

        /// <summary>
        /// The delay before a key start to trigger repeatedly.
        /// </summary>
        public int RepeatDelay { get; set; }

        /// <summary>
        /// The delay between triggering when triggering repeatedly (key held down).
        /// </summary>
        public int RepeatRate { get; set; }


        private Vector2 _lastLeftStick;

        /// <summary>
        /// Last time we sent a repeated key press.
        /// </summary>
        private DateTime _lastRepeat;

        /// <summary>
        /// State from the last update, to check for changes.
        /// </summary>
        private GamePadState _previousState;
        #endregion
        public GamepadInputManager(Game game) : base(game)
        {
           
            game.Services.AddService(typeof(IGamepadInputManager),this);

        }
        
       
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!Game.IsActive)
            {
                return;
            }


            // The current time, used to check for repeats.
            var currentTime = DateTime.Now;

            var currentState = GamePad.GetState((PlayerIndex) 0);
            var buttons = Enum.GetValues(typeof (Buttons));
            foreach (Buttons button in buttons)
            {
                if (currentState.IsButtonDown(button))
                {
                    if (_previousState.IsButtonDown(button))
                    {
                        // Key was held.
                        if (RepeatDelay > 0 && RepeatRate > 0 && _lastRepeat != null)
                        {
                            if (new TimeSpan(currentTime.Ticks - _lastRepeat.Ticks).TotalMilliseconds > RepeatRate)
                            {
                                _lastRepeat = currentTime;
                                OnPressed(new GamePadInputEventArgs(currentState, button, true));
                            }
                        }
                    }
                    else
                    {
                        // Key was pressed.
                        if (RepeatDelay > 0 && RepeatRate > 0)
                        {
                            
                            _lastRepeat = currentTime.AddMilliseconds(RepeatDelay);
                        }
                        OnPressed(new GamePadInputEventArgs(currentState, button, false));
                    }
                }
                else
                {
                    if (_previousState.IsButtonDown(button))
                    {

                        OnReleased(new GamePadInputEventArgs(currentState, button, false));
                    }
                }

                
            }

            if (currentState.ThumbSticks.Left != _previousState.ThumbSticks.Left)
            {
                OnLeftMove(new GamePadInputEventArgs(currentState));
            }
            if (currentState.ThumbSticks.Right != _previousState.ThumbSticks.Right)
            {
                OnRightMove(new GamePadInputEventArgs(currentState));
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
        public event EventHandler<EventArgs> Pressed;
        public event EventHandler<EventArgs> Released;
        public event EventHandler<EventArgs> LeftMoved;
        public event EventHandler<EventArgs> RightMoved;
    }
}
