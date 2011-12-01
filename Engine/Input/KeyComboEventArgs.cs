using System;

namespace Engine.Input
{
    /// <summary>
    /// Event arg for a key-combination specific event.
    /// </summary>
    public sealed class KeyComboEventArgs : EventArgs
    {
        /// <summary>
        /// The key that was pressed or released.
        /// </summary>
        public KeyCombo Combo { get; private set; }

        internal KeyComboEventArgs(KeyCombo combo)
        {
            this.Combo = combo;
        }
    }
}
