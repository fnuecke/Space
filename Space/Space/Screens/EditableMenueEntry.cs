using System;
using Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameStateManagement
{
    class EditableMenueEntry:MenuEntry
    {
        #region Fields
        public bool locked{get;set;}
        public KeyMap KeyMap { get; set; }
        /// <summary>
        /// Last time a key was pressed (to suppress blinking for a bit while / after typing).
        /// </summary>
        private DateTime lastKeyPress = DateTime.MinValue;
        /// <summary>
        /// Input cursor offset.
        /// </summary>
        private int Cursor = 0;
        /// <summary>
        /// The color of the caret (input position marker).
        /// </summary>
        public Color CaretColor { get; set; }
        /// <summary>
        /// Overall padding of the console.
        /// </summary>
        private const int Padding = 4;

        private string inputText;
        private string staticText = "";
        #endregion
        public EditableMenueEntry(string Text)
            : base(Text)
        {
            inputText = "";
            staticText = Text;
            
            KeyMap = KeyMap.KeyMapByLocale("en-US");
            locked = false;
            CaretColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
            
        }
        public void SetInputText(string text)
        {
            inputText = text;
            Cursor = text.Length;
            Text = staticText + inputText;
        }
        public  string  GetInputText()
        {
            return inputText;
        }
        public void HandleKeyPressed(object sender, EventArgs e)
        {
            if (Active&&!locked)
            {
                var args = (KeyboardInputEventArgs)e;

                switch (args.Key)
                {
                    case Keys.Back:
                        if (inputText.Length > 0)
                        {
                            inputText = (Cursor > 0 ? inputText.Substring(0, Cursor - 1) : "") + (Cursor > 0 ? inputText.Substring(Cursor) : inputText.Substring(Cursor + 1));
                            if(Cursor>0)
                            Cursor--;
                        }
                            break;
                    case Keys.Delete:
                        break;
                    case Keys.Down:
                        break;
                    case Keys.End:
                        break;
                    case Keys.Enter:
                        break;
                    case Keys.Escape:
                        break;
                    case Keys.Home:
                        break;
                    case Keys.Left:
                        if(Cursor>0)
                        Cursor--; 
                        break;
                    case Keys.PageDown:
                        break;
                    case Keys.PageUp:
                        break;
                    case Keys.Right:
                        if (Cursor < inputText.Length)
                            Cursor++;
                        break;
                    case Keys.Tab:

                        break;
                    case Keys.Up:

                        break;
                    default:
                        if (KeyMap != null)
                        {
                            char ch = KeyMap[args.Modifier, args.Key];
                            if (ch != '\0')
                            {
                                inputText = inputText.Substring(0, Cursor) + ch + inputText.Substring(Cursor);
                               Cursor++;
                            }
                        }
                        break;
                }
                Text = staticText + inputText;
            }
        }



         /// <summary>
        /// Draws the menu entry. This can be overridden to customize the appearance.
        /// </summary>
        override
        public void Draw(MenuScreen screen, bool isSelected, GameTime gameTime)
        {
            base.Draw(screen, isSelected, gameTime);
            
            if (Active&&((int)gameTime.TotalGameTime.TotalSeconds & 1) == 0 || (lastKeyPress != null && new TimeSpan(DateTime.Now.Ticks - lastKeyPress.Ticks).TotalSeconds < 1))
            {
                ScreenManager screenManager = screen.ScreenManager;
                SpriteBatch SpriteBatch = screenManager.SpriteBatch;
                
                SpriteFont Font = screenManager.Font;
                // Pulsate the size of the selected menu entry.
                double time = gameTime.TotalGameTime.TotalSeconds;

                float pulsate = (float)Math.Sin(time * 6) + 1;

                float scale = 1 + pulsate * 0.05f * selectionFade;
                int cursorCounter = Cursor;
                if (inputText.Length > 0&&cursorCounter>0)
                    cursorCounter -= 1;
                int cursorX = (int)Position.X + (int)(Font.MeasureString(inputText.Substring(0, Cursor)).X + Font.MeasureString(staticText).X * scale);
                int cursorY = (int)(Position.Y -Font.LineSpacing/2 * scale) ;// +Padding + (ComputeNumberOfVisibleLines() - (inputWrapped.Count - cursorLine)) * Font.LineSpacing;
                int cursorWidth;
                if (inputText.Length > cursorCounter+1)
                {
                    cursorWidth = (int)(Font.MeasureString(inputText.Substring(cursorCounter, 1)).X * scale);
                }
                else
                {
                    cursorWidth = (int)Font.MeasureString(" ").X;
                }
                
                SpriteBatch.Draw(screenManager.PixelTexture, new Rectangle(cursorX, cursorY, cursorWidth, Font.LineSpacing), CaretColor);
                            
            }
        }
    }
}
