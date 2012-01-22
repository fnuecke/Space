using System;

namespace Engine.Input
{
    /// <summary>
    /// Interface to the keyboard input manager class. The events are of
    /// the type <see cref="MouseInputEventArgs"/>.
    /// </summary>
    public interface IMouseInputManager
    {
        /// <summary>
        /// Fired when a mouse button is pressed.
        /// </summary>
        event EventHandler<MouseInputEventArgs> Pressed;

        /// <summary>
        /// Fired when a mouse button is released.
        /// </summary>
        event EventHandler<MouseInputEventArgs> Released;

        /// <summary>
        /// Fired when the scroll wheel is scrolled.
        /// </summary>
        event EventHandler<MouseInputEventArgs> Scrolled;

        /// <summary>
        /// Fired when the mouse moves.
        /// </summary>
        event EventHandler<MouseInputEventArgs> Moved;
    }
}
