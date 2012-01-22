using System;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    /// <summary>
    /// Event args for mouse pressed / released / etc events of the <see cref="IMouseInputManager"/>.
    /// </summary>
    public sealed class MouseInputEventArgs : EventArgs
    {
        /// <summary>
        /// Possible mouse buttons.
        /// </summary>
        public enum MouseButton
        {
            /// <summary>
            /// No mouse button.
            /// </summary>
            None,

            /// <summary>
            /// The left mouse button.
            /// </summary>
            Left,
            
            /// <summary>
            /// The middle mouse button.
            /// </summary>
            Middle,

            /// <summary>
            /// The right mouse button.
            /// </summary>
            Right,
            
            /// <summary>
            /// Additional mouse button one (normally: forward).
            /// </summary>
            Extra1,

            /// <summary>
            /// Additional mouse button two (normally: backward).
            /// </summary>
            Extra2
        }

        /// <summary>
        /// The overall mouse state that's now active.
        /// </summary>
        public MouseState State { get; private set; }

        /// <summary>
        /// The button relevant to this event.
        /// </summary>
        public MouseButton Button { get; private set; }

        /// <summary>
        /// The new state of the relevant button.
        /// </summary>
        public ButtonState ButtonState { get; private set; }

        /// <summary>
        /// The scrolling delta of the scroll wheel.
        /// </summary>
        public int ScrollDelta { get; private set; }

        /// <summary>
        /// The delta in X position of the mouse due to a mouse move.
        /// </summary>
        public int DeltaX { get; private set; }

        /// <summary>
        /// The delta in Y position of the mouse due to a mouse move.
        /// </summary>
        public int DeltaY { get; private set; }

        /// <summary>
        /// The absolute X position of the mouse.
        /// </summary>
        public int X { get; private set; }

        /// <summary>
        /// The absolute Y position of the mouse.
        /// </summary>
        public int Y { get; private set; }

        /// <summary>
        /// For pressed / released events.
        /// </summary>
        internal MouseInputEventArgs(MouseState mouseState, MouseButton button, ButtonState buttonState)
        {
            this.State = mouseState;
            this.Button = button;
            this.ButtonState = buttonState;
        }

        /// <summary>
        /// For scroll wheel events.
        /// </summary>
        internal MouseInputEventArgs(MouseState mouseState, int delta)
        {
            this.State = mouseState;
            this.ScrollDelta = delta;
        }

        /// <summary>
        /// For mouse moved events.
        /// </summary>
        internal MouseInputEventArgs(MouseState mouseState, int x, int y, int dx, int dy)
        {
            this.State = mouseState;
            this.X = x;
            this.Y = y;
            this.DeltaX = dx;
            this.DeltaY = dy;
        }
    }
}
