using System;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    /// <summary>
    /// Event args for key pressed / released events of the <see cref="IKeyboardInputManager"/>.
    /// </summary>
    public sealed class KeyboardInputEventArgs : EventArgs
    {
        /// <summary>
        /// The key that was pressed or released.
        /// </summary>
        public Keys Key { get; private set; }

        /// <summary>
        /// The active keyboard modifier combination.
        /// </summary>
        public KeyModifier Modifier { get; private set; }

        /// <summary>
        /// The overall keyboard state that's now active.
        /// </summary>
        public KeyboardState State { get; private set; }

        internal KeyboardInputEventArgs(KeyboardState state, Keys key, KeyModifier modifier)
        {
            this.State = state;
            this.Key = key;
            this.Modifier = modifier;
        }
    }
}
