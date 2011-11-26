using System;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    /// <summary>
    /// Event args for key pressed / released events of the <see cref="IKeyboardInputManager"/>.
    /// </summary>
    public class KeyboardInputEventArgs : EventArgs
    {
        /// <summary>
        /// The key that was pressed or released.
        /// </summary>
        public Keys Key { get; private set; }

        /// <summary>
        /// The active keyboard modifier combination.
        /// </summary>
        public KeyModifier Modifier { get; private set; }

        public KeyboardInputEventArgs(Keys key, KeyModifier modifier)
        {
            this.Key = key;
            this.Modifier = modifier;
        }
    }
}
