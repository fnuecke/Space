using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    /// <summary>
    /// Event args for key pressed / released events of the <see cref="IGamepadInputManager"/>.
    /// </summary>
    public sealed class GamePadInputEventArgs : EventArgs
    {
        /// <summary>
        /// The button that was pressed or released.
        /// </summary>
        public Buttons Buttons { get; private set; }

        /// <summary>
        /// The new stick position, if the event was triggered by a stick being
        /// moved.
        /// </summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// The overall keyboard state that's now active.
        /// </summary>
        public GamePadState State { get; private set; }

        internal GamePadInputEventArgs(GamePadState state, Buttons buttons, Vector2 position)
        {
            this.State = state;
            this.Buttons = buttons;
        }

        internal GamePadInputEventArgs(GamePadState state, Buttons buttons)
            : this(state, buttons, Vector2.Zero)
        {
        }
    }
}
