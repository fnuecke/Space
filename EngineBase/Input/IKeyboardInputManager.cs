using System;
using Microsoft.Xna.Framework.Input;

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

        /// <summary>
        /// Get a representation for a specific key-combination, which can be used
        /// to register for events on specific combinations of keys only.
        /// </summary>
        /// <param name="key">the key that has to be pressed.</param>
        /// <param name="modifier">the modifier that has to be active.</param>
        /// <returns>an object that represents this keyboard combination.</returns>
        KeyCombo Combo(Keys key, KeyModifier modifier);

        /// <summary>
        /// Get a representation for a specific key-combination, which can be used
        /// to register for events on specific combinations of keys only.
        /// </summary>
        /// <param name="keys">the list of keys that have to be pressed.</param>
        /// <param name="modifier">the modifier that has to be active.</param>
        /// <returns>an object that represents this keyboard combination.</returns>
        KeyCombo Combo(Keys[] keys, KeyModifier modifier = KeyModifier.None);
    }
}
