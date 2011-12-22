using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    /// <summary>
    /// This class may be used to get an event driven access to user mouse interaction.
    /// 
    /// Upon creation, this class registers itself as a service with the game it
    /// is created for, so it can be accessed by any other component.
    /// </summary>
    public sealed class MouseInputManager : GameComponent, IMouseInputManager
    {
        #region Events

        /// <summary>
        /// Fired when a mouse button is pressed.
        /// </summary>
        public event EventHandler<EventArgs> Pressed;

        /// <summary>
        /// Fired when a mouse button is released.
        /// </summary>
        public event EventHandler<EventArgs> Released;

        /// <summary>
        /// Fired when the scroll wheel is scrolled.
        /// </summary>
        public event EventHandler<EventArgs> Scrolled;

        /// <summary>
        /// Fired when the mouse moves.
        /// </summary>
        public event EventHandler<EventArgs> Moved;
        
        #endregion
        
        #region Fields

        /// <summary>
        /// State from the last update, to check for changes.
        /// </summary>
        private MouseState _previousState;

        #endregion

        /// <summary>
        /// Creates a new mouse manager for the given game and adds it as a service.
        /// </summary>
        /// <param name="game">the game to create the manager for.</param>
        public MouseInputManager(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IMouseInputManager), this);
        }
        
        #region Logic

        /// <summary>
        /// Implements key press / repeat / release logic.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Do not handle input, if the game is not the active window.
            if (!Game.IsActive)
            {
                return;
            }

            // Get a shortcut to the current mouse state.
            var currentState = Mouse.GetState();

            if (_previousState != null)
            {
                // Check for pressed / released events.
                if (currentState.LeftButton != _previousState.LeftButton)
                {
                    if (currentState.LeftButton == ButtonState.Pressed)
                    {
                        OnPressed(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Left, ButtonState.Pressed));
                    }
                    else
                    {
                        OnReleased(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Left, ButtonState.Released));
                    }
                }
                if (currentState.RightButton != _previousState.RightButton)
                {
                    if (currentState.RightButton == ButtonState.Pressed)
                    {
                        OnPressed(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Right, ButtonState.Pressed));
                    }
                    else
                    {
                        OnReleased(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Right, ButtonState.Released));
                    }
                }
                if (currentState.MiddleButton != _previousState.MiddleButton)
                {
                    if (currentState.MiddleButton == ButtonState.Pressed)
                    {
                        OnPressed(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Middle, ButtonState.Pressed));
                    }
                    else
                    {
                        OnReleased(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Middle, ButtonState.Released));
                    }
                }
                if (currentState.XButton1 != _previousState.XButton1)
                {
                    if (currentState.XButton1 == ButtonState.Pressed)
                    {
                        OnPressed(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Extra1, ButtonState.Pressed));
                    }
                    else
                    {
                        OnReleased(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Extra1, ButtonState.Released));
                    }
                }
                if (currentState.XButton2 != _previousState.XButton2)
                {
                    if (currentState.XButton2 == ButtonState.Pressed)
                    {
                        OnPressed(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Extra2, ButtonState.Pressed));
                    }
                    else
                    {
                        OnReleased(new MouseInputEventArgs(currentState, MouseInputEventArgs.MouseButton.Extra2, ButtonState.Released));
                    }
                }

                // Check for scroll wheel.
                if (currentState.ScrollWheelValue != _previousState.ScrollWheelValue)
                {
                    OnScrolled(new MouseInputEventArgs(currentState, _previousState.ScrollWheelValue - currentState.ScrollWheelValue));
                }

                // Check for mouse movement.
                if (currentState.X != _previousState.X || currentState.Y != _previousState.Y)
                {
                    OnMoved(new MouseInputEventArgs(currentState, currentState.X, currentState.Y, _previousState.X - currentState.X, _previousState.Y - currentState.Y));
                }
            }

            _previousState = currentState;
        }

        private void OnPressed(MouseInputEventArgs e)
        {
            if (Pressed != null)
            {
                Pressed(this, e);
            }
        }

        private void OnReleased(MouseInputEventArgs e)
        {
            if (Released != null)
            {
                Released(this, e);
            }
        }

        private void OnScrolled(MouseInputEventArgs e)
        {
            if (Scrolled != null)
            {
                Scrolled(this, e);
            }
        }

        private void OnMoved(MouseInputEventArgs e)
        {
            if (Moved != null)
            {
                Moved(this, e);
            }
        }

        #endregion
    }
}
