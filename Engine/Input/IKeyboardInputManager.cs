using System;

namespace Engine.Input
{
    /// <summary>
    /// Interface to the keyboard input manager class. The events are of
    /// the type <see cref="KeyboardInputEventArgs"/>.
    /// </summary>
    public interface IKeyboardInputManager
    {
        /// <summary>
        /// Fired when a key is newly pressed or repeated.
        /// </summary>
        event EventHandler<EventArgs> Pressed;

        /// <summary>
        /// Fired when a key is released.
        /// </summary>
        event EventHandler<EventArgs> Released;
    }
}
