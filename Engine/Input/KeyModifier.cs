using System;

namespace Engine.Input
{

    /// <summary>
    /// Represents possible combinations of key modifiers that may be
    /// pressed when an input event fires in the <see cref="KeyboardInputManager"/>.
    /// </summary>
    [Flags]
    public enum KeyModifier
    {
        /// <summary>
        /// No modifier.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Alt key.
        /// </summary>
        Alt = 0x1,

        /// <summary>
        /// Control key.
        /// </summary>
        Control = 0x2,

        /// <summary>
        /// Shift key.
        /// </summary>
        Shift = 0x4,

        /// <summary>
        /// Alt and Control keys.
        /// </summary>
        AltControl = Alt | Control,

        /// <summary>
        /// Alt and Shift keys.
        /// </summary>
        AltShift = Alt | Shift,

        /// <summary>
        /// Control and shift keys.
        /// </summary>
        ControlShift = Control | Shift,

        /// <summary>
        /// Alt, Control and Shift keys.
        /// </summary>
        AltControlShift = Control | Shift | Alt,
    }

}
