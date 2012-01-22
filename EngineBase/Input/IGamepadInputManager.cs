using System;

namespace Engine.Input
{
    /// <summary>
    /// Interface to the game pad input manager class. The events are of
    /// the type <see cref="GamepadInputEventArgs"/>.
    /// </summary>
    public interface IGamepadInputManager
    {
        /// <summary>
        /// Fired when a button is newly pressed.
        /// </summary>
        event EventHandler<GamepadInputEventArgs> Pressed;

        /// <summary>
        /// Fired when a button is released.
        /// </summary>
        event EventHandler<GamepadInputEventArgs> Released;

        /// <summary>
        /// Fired when the left game pad stick was moved.
        /// </summary>
        event EventHandler<GamepadInputEventArgs> LeftMoved;

        /// <summary>
        /// Fired when the right game pad stick was moved.
        /// </summary>
        event EventHandler<GamepadInputEventArgs> RightMoved;
    }
}
