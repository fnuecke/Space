using System;
using System.Text;
using Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Space.ScreenManagement.Screens.Entries
{
    public class EditableMenuEntry : MenuEntry
    {
        #region Events

        /// <summary>
        /// Raised when the text in this entry changed and was confirmed.
        /// </summary>
        public event EventHandler<EventArgs> Changed;

        #endregion

        #region Properties

        /// <summary>
        /// The current user input.
        /// </summary>
        public string InputText
        {
            get
            {
                return Focused ? _input.ToString() : _confirmedInput;
            }
            set
            {
                Focused = false;
                _confirmedInput = value;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Keyboard input manager we use to capture key presses.
        /// </summary>
        private readonly IKeyboardInputManager _keyboard;

        /// <summary>
        /// Key map we use to convert key presses to characters.
        /// </summary>
        private readonly KeyMap _keyMap = KeyMap.KeyMapByLocale("en-US");

        /// <summary>
        /// The color of the caret (input position marker).
        /// </summary>
        private readonly Color _caretColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);

        /// <summary>
        /// The last confirmed input.
        /// </summary>
        private string _confirmedInput = String.Empty;

        /// <summary>
        /// The text currently contained in this text field.
        /// </summary>
        private StringBuilder _input = new StringBuilder();

        /// <summary>
        /// Input cursor offset.
        /// </summary>
        private int _cursor = 0;

        /// <summary>
        /// Last time a key was pressed (to suppress blinking for a bit while / after typing).
        /// </summary>
        private DateTime _lastKeyPress = DateTime.MinValue;

        #endregion

        #region Constructor

        public EditableMenuEntry(string label, IKeyboardInputManager keyboard, string defaultValue = "")
            : base(label)
        {
            _keyboard = keyboard;
            _confirmedInput = defaultValue;

            _keyboard.Pressed += HandleKeyPressed;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keyboard.Pressed -= HandleKeyPressed;
            }
        }

        #endregion

        #region Handle input

        public override void HandleInput(MenuScreen screen, InputState input, bool mouseOver)
        {
            if (input.KeyCancel)
            {
                Focused = false;
            }
            else if (input.KeySelect)
            {
                if (Focused)
                {
                    if (!_input.ToString().Equals(_confirmedInput))
                    {
                        _confirmedInput = _input.ToString();
                        OnChanged(EventArgs.Empty);
                    }
                    Focused = false;
                }
                else
                {
                    _input.Clear();
                    _input.Append(_confirmedInput);
                    Focused = true;
                }
            }
            else if (mouseOver && input.MouseSelect)
            {
                if (!Focused)
                {
                    _input.Clear();
                    _input.Append(_confirmedInput);
                    Focused = true;
                }
            }
        }

        public void HandleKeyPressed(object sender, EventArgs e)
        {
            if (Focused)
            {
                var args = (KeyboardInputEventArgs)e;

                switch (args.Key)
                {
                    case Keys.Back:
                        if (_cursor > 0)
                        {
                            --_cursor;
                            _input.Remove(_cursor, 1);
                        }
                        break;

                    case Keys.Delete:
                        if (_cursor < _input.Length)
                        {
                            _input.Remove(_cursor, 1);
                        }
                        break;

                    case Keys.End:
                        _cursor = _input.Length;
                        break;

                    case Keys.Home:
                        _cursor = 0;
                        break;

                    case Keys.Left:
                        _cursor = System.Math.Max(0, _cursor - 1);
                        break;

                    case Keys.Right:
                        _cursor = System.Math.Min(_input.Length, _cursor + 1);
                        break;

                    default:
                        if (_keyMap != null)
                        {
                            char ch = _keyMap[args.Modifier, args.Key];
                            if (ch != '\0')
                            {
                                _input.Insert(_cursor, ch);
                                _cursor++;
                            }
                        }
                        break;
                }

                _lastKeyPress = DateTime.Now;
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draws the menu entry. This can be overridden to customize the appearance.
        /// </summary>
        public override void Draw(MenuScreen screen, bool isSelected, GameTime gameTime)
        {
            base.Draw(screen, isSelected, gameTime);

            if (Focused && ((int)gameTime.TotalGameTime.TotalSeconds & 1) == 0 || (new TimeSpan(DateTime.Now.Ticks - _lastKeyPress.Ticks).TotalSeconds < 1))
            {
                SpriteBatch spriteBatch = screen.ScreenManager.SpriteBatch;
                SpriteFont Font = screen.ScreenManager.Font;

                // Pulsate the size of the selected menu entry.
                double time = gameTime.TotalGameTime.TotalSeconds;

                float pulsate = (float)Math.Sin(time * 6) + 1;

                float scale = 1 + pulsate * 0.05f * _selectionFade;
                int cursorCounter = _cursor;
                if (_input.Length > 0 && cursorCounter > 0)
                {
                    cursorCounter -= 1;
                }
                int cursorX = (int)Position.X + (int)(Font.MeasureString(Text + ": " + _input.ToString().Substring(0, _cursor)).X * scale);
                int cursorY = (int)(Position.Y - Font.LineSpacing / 2 * scale);
                int cursorWidth;
                if (cursorCounter >= _input.Length)
                {
                    cursorWidth = (int)Font.MeasureString(" ").X;
                }
                else
                {
                    cursorWidth = (int)(Font.MeasureString(_input[cursorCounter].ToString()).X * scale);
                }

                spriteBatch.Draw(screen.ScreenManager.PixelTexture, new Rectangle(cursorX, cursorY, cursorWidth, Font.LineSpacing), _caretColor);
            }
        }

        /// <summary>
        /// Queries the actual text string to draw.
        /// </summary>
        /// <returns>The actual text to draw.</returns>
        protected override string GetTextToDraw()
        {
            return Text + ": " + InputText;
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
