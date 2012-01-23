using System;
using System.Collections.Generic;

namespace Space.ScreenManagement.Screens.Entries
{
    sealed class OptionMenuEntry<T> : MenuEntry
    {
        #region Events

        /// <summary>
        /// Raised when the selected option in this entry changed.
        /// </summary>
        public event EventHandler<EventArgs> Changed;

        #endregion

        #region Properties

        /// <summary>
        /// The currently selected value in this option entry.
        /// </summary>
        public T Value { get { return _values[_currentValue]; } }

        #endregion
        
        #region Fields

        /// <summary>
        /// Possible values for this option, with their string representations.
        /// </summary>
        private Dictionary<T, string> _labels = new Dictionary<T, string>();

        /// <summary>
        /// The actual list of possible option values.
        /// </summary>
        private List<T> _values;

        /// <summary>
        /// The index of the option value currently selected.
        /// </summary>
        private int _currentValue = 0;

        #endregion

        #region Constructor

        public OptionMenuEntry(string label, Dictionary<T, string> options, T defaultValue = default(T))
            : base(label)
        {
            _labels = options;
            _values = new List<T>(_labels.Keys);
            _currentValue = System.Math.Max(0, _values.IndexOf(defaultValue));
        }

        #endregion

        #region Handle Input

        public override void HandleInput(MenuScreen screen, InputState input, bool mouseOver)
        {
            if (input.KeyNext || mouseOver && input.MouseSelect)
            {
                // Select next option of a menu entry.
                _currentValue = (_currentValue + 1) % _values.Count;
                if (_values.Count > 1)
                {
                    OnChanged(EventArgs.Empty);
                }
            }
            else if (input.KeyPrevious)
            {
                // Select previous option of a menu entry.
                _currentValue = (_currentValue - 1 + _values.Count) % _values.Count;
                if (_values.Count > 1)
                {
                    OnChanged(EventArgs.Empty);
                }
            }
        }

        #endregion

        #region Draw

        protected override string GetTextToDraw()
        {
            return Text + ": " + _labels[Value];
        }

        #endregion

        #region Event dispatching

        private void OnChanged(EventArgs e)
        {
            if (Changed != null)
            {
                Changed(this, e);
            }
        }

        #endregion
    }
}
