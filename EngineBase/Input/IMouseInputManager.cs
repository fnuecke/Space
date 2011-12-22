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
        event EventHandler<EventArgs> Pressed;

        /// <summary>
        /// Fired when a mouse button is released.
        /// </summary>
        event EventHandler<EventArgs> Released;

        /// <summary>
        /// Fired when the scroll wheel is scrolled.
        /// </summary>
        event EventHandler<EventArgs> Scrolled;

        /// <summary>
        /// Fired when the mouse moves.
        /// </summary>
        event EventHandler<EventArgs> Moved;
    }
}
