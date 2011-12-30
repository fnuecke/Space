#region File Description
//-----------------------------------------------------------------------------
// MenuEntry.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ScreenManagement.Screens.Entries
{
    /// <summary>
    /// Helper class represents a single entry in a MenuScreen. By default this
    /// just draws the entry text string, but it can be customized to display menu
    /// entries in different ways. This also provides an event that will be raised
    /// when the menu entry is selected.
    /// </summary>
    public class MenuEntry : IDisposable
    {
        #region Events

        /// <summary>
        /// Event raised when the menu entry is selected.
        /// </summary>
        public event EventHandler<EventArgs> Activated;

        #endregion

        #region Fields

        /// <summary>
        /// Tracks a fading selection effect on the entry.
        /// </summary>
        /// <remarks>
        /// The entries transition out of the selection effect when they are deselected.
        /// </remarks>
        protected float _selectionFade;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the text of this menu entry.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the position at which to draw this menu entry.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Returns whether the entry is focused (if any entry is focused, the
        /// entry selection cannot change).
        /// </summary>
        public bool Focused { get; set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructs a new menu entry with the specified text.
        /// </summary>
        public MenuEntry(string text)
        {
            this.Text = text;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion

        #region Handle Input

        public virtual void HandleInput(MenuScreen screen, InputState input, bool mouseOver)
        {
            if (input.KeySelect || (mouseOver && input.MouseSelect))
            {
                // Activate a menu entry.
                screen.ErrorText = String.Empty;
                Activate();
            }
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the menu entry.
        /// </summary>
        public virtual void Update(MenuScreen screen, bool isSelected, GameTime gameTime)
        {
            // When the menu selection changes, entries gradually fade between
            // their selected and deselected appearance, rather than instantly
            // popping to the new state.
            float fadeSpeed = (float)gameTime.ElapsedGameTime.TotalSeconds * 4;

            if (isSelected)
            {
                _selectionFade = Math.Min(_selectionFade + fadeSpeed, 1);
            }
            else
            {
                _selectionFade = Math.Max(_selectionFade - fadeSpeed, 0);
            }
        }

        /// <summary>
        /// Draws the menu entry. This can be overridden to customize the appearance.
        /// </summary>
        public virtual void Draw(MenuScreen screen, bool isSelected, GameTime gameTime)
        {
            // Draw the selected entry in yellow, otherwise white.
            Color color = isSelected ? Color.Yellow : Color.White;

            // Pulsate the size of the selected menu entry.
            double time = gameTime.TotalGameTime.TotalSeconds;
            
            float pulsate = (float)Math.Sin(time * 6) + 1;

            float scale = 1 + pulsate * 0.05f * _selectionFade;

            // Modify the alpha to fade text out during transitions.
            color *= screen.TransitionAlpha;

            // Draw text, centered on the middle of each line.
            SpriteBatch spriteBatch = screen.ScreenManager.SpriteBatch;
            SpriteFont font = screen.ScreenManager.Font;

            Vector2 origin = new Vector2(0, font.LineSpacing / 2);

            var normalWidth = font.MeasureString(GetTextToDraw()).X;
            var scaledWidth = normalWidth * scale;
            var widthDelta = scaledWidth - normalWidth;
            var correctedPosition = Position;
            correctedPosition.X -= widthDelta / 2;
            spriteBatch.DrawString(font, GetTextToDraw(), correctedPosition, color, 0,
                                   origin, scale, SpriteEffects.None, 0);
        }

        /// <summary>
        /// Queries the actual text string to draw.
        /// </summary>
        /// <returns>The actual text to draw.</returns>
        protected virtual string GetTextToDraw()
        {
            return Text;
        }

        /// <summary>
        /// Queries how much space this menu entry requires.
        /// </summary>
        public virtual int GetHeight(MenuScreen screen)
        {
            return screen.ScreenManager.Font.LineSpacing;
        }

        /// <summary>
        /// Queries how wide the entry is, used for centering on the screen.
        /// </summary>
        public virtual int GetWidth(MenuScreen screen)
        {
            return (int)screen.ScreenManager.Font.MeasureString(GetTextToDraw()).X;
        }

        #endregion

        #region Event dispatching

        /// <summary>
        /// Method for raising the activated event.
        /// </summary>
        public void Activate()
        {
            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
