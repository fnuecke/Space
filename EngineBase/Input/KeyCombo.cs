using System;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    /// <summary>
    /// Represents a combination of keys that:
    /// <list type="ul">
    /// <item>when pressed together will fire a pressed event.</item>
    /// <item>when all pressed and one key is released will fire a released event.</item>
    /// </list>
    /// </summary>
    public sealed class KeyCombo
    {
        #region Events

        /// <summary>
        /// Fired when the combo is pressed.
        /// </summary>
        public event EventHandler<EventArgs> Pressed;

        /// <summary>
        /// Fired when the combo is released.
        /// </summary>
        public event EventHandler<EventArgs> Released;

        #endregion

        #region Properties

        /// <summary>
        /// The list of keys that make up this combination.
        /// </summary>
        public ReadOnlyCollection<Keys> Combo { get { return Array.AsReadOnly<Keys>(_combo); } }

        /// <summary>
        /// The active keyboard modifier combination.
        /// </summary>
        public KeyModifier Modifier { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The actual value for the keys.
        /// </summary>
        private readonly Keys[] _combo;

        /// <summary>
        /// Key combo currently active?
        /// </summary>
        private bool _isActive;

        #endregion

        internal KeyCombo(IKeyboardInputManager manager, Keys[] combo, KeyModifier modifier)
        {
            this._combo = combo;
            this.Modifier = modifier;
            manager.Pressed += HandleKeyPressedOrReleased;
            manager.Released += HandleKeyPressedOrReleased;
        }

        #region Event handling

        void HandleKeyPressedOrReleased(object sender, EventArgs e)
        {
            var args = (KeyboardInputEventArgs)e;
            if (args.Modifier == Modifier)
            {
                foreach (var key in _combo)
                {
                    if (!args.State.IsKeyDown(key))
                    {
                        Inactive();
                        return;
                    }
                }
                Active();
            }
            else
            {
                Inactive();
            }
        }

        #endregion

        #region Utility methods

        private void Active()
        {
            if (!_isActive)
            {
                _isActive = true;
                OnPressed(new KeyComboEventArgs(this));
            }
        }

        private void Inactive()
        {
            if (_isActive)
            {
                _isActive = false;
                OnReleased(new KeyComboEventArgs(this));
            }
        }

        private void OnPressed(KeyComboEventArgs e)
        {
            if (Pressed != null)
            {
                Pressed(this, e);
            }
        }

        private void OnReleased(KeyComboEventArgs e)
        {
            if (Released != null)
            {
                Released(this, e);
            }
        }

        #endregion
    }
}
