#region File Description
//-----------------------------------------------------------------------------
// MenuScreen.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ScreenManagement.Screens.Entries;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// Base class for screens that contain a menu of options. The user can
    /// move up and down to select an entry, or cancel to back out of the screen.
    /// </summary>
    public abstract class MenuScreen : GameScreen
    {
        #region Properties

        /// <summary>
        /// Gets the list of menu entries, so derived classes can add
        /// or change the menu contents.
        /// </summary>
        protected IList<MenuEntry> MenuEntries { get; private set; }

        /// <summary>
        /// Error message to display at the bottom of the screen.
        /// </summary>
        public string ErrorText { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The title of this menu.
        /// </summary>
        private readonly string _title;

        /// <summary>
        /// Entry who's activate method to call when the user presses escape.
        /// </summary>
        private MenuEntry _escapeEntry;

        /// <summary>
        /// The entry currently selected in this menu.
        /// </summary>
        private int _selectedEntry = 0;

        /// <summary>
        /// Overall entry size.
        /// </summary>
        private Rectangle _entriesBounds = Rectangle.Empty;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuScreen(string title)
        {
            this._title = title;
            MenuEntries = new List<MenuEntry>();
            ErrorText = String.Empty;
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void UnloadContent()
        {
            foreach (var entry in MenuEntries)
            {
                entry.Dispose();
            }
        }

        protected void SetEscapeEntry(MenuEntry entry)
        {
            _escapeEntry = entry;
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Responds to user input, changing the selected entry and accepting
        /// or canceling the menu.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (!MenuEntries[_selectedEntry].Focused)
            {
                if (input.KeyUp)
                {
                    // Move to the previous menu entry.
                    _selectedEntry = (_selectedEntry - 1 + MenuEntries.Count) % MenuEntries.Count;
                    return;
                }
                else if (input.KeyDown)
                {
                    // Move to the next menu entry.
                    _selectedEntry = (_selectedEntry + 1) % MenuEntries.Count;
                    return;
                }
                else if (input.KeyCancel)
                {
                    // Exit out of this menu.
                    _escapeEntry.Activate();
                    return;
                }
            }

            // Handle selection via mouse position.
            var mouseX = input.MousePosition.X;
            var mouseY = input.MousePosition.Y;

            // Check if the mouse is in fact hovering an entry.
            bool mouseOver = false;

            // Find the first entry we're over (if any).
            for (int i = 0; i < MenuEntries.Count; i++)
            {
                var entry = MenuEntries[i];
                // Check if mouse is within bounds.
                if (mouseX > entry.Position.X && mouseX < entry.Position.X + entry.GetWidth(this)
                    && mouseY > entry.Position.Y && mouseY < entry.Position.Y + entry.GetHeight(this))
                {
                    // Hovering.
                    mouseOver = true;

                    // Update selection to that entry.
                    _selectedEntry = i;

                    // Done. Just take the first we find.
                    break;
                }
            }

            // Then delegate actual handling to the menu entry.
            MenuEntries[_selectedEntry].HandleInput(this, input, mouseOver);
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Allows the screen the chance to position the menu entries. By default
        /// all menu entries are lined up in a vertical list, centered on the screen.
        /// </summary>
        protected virtual void UpdateMenuEntryLocations()
        {
            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // start at Y = 175; each X value is generated per entry
            Vector2 position = new Vector2(0f, 175f);

            // Reset.
            _entriesBounds = Rectangle.Empty;
            _entriesBounds.X = int.MaxValue;
            _entriesBounds.Y = (int)position.Y - ((MenuEntries.Count > 0) ? (MenuEntries[0].GetHeight(this) / 2) : 0);

            // update each menu entry's location in turn
            for (int i = 0; i < MenuEntries.Count; i++)
            {
                MenuEntry menuEntry = MenuEntries[i];

                var entryWidth = menuEntry.GetWidth(this);
                var entryHeight = menuEntry.GetHeight(this);

                // each entry is to be centered horizontally
                position.X = ScreenManager.GraphicsDevice.Viewport.Width / 2 - entryWidth / 2;

                if (ScreenState == ScreenState.TransitionOn)
                {
                    position.X -= transitionOffset * 256;
                }
                else
                {
                    position.X += transitionOffset * 512;
                }

                // set the entry's position
                menuEntry.Position = position;

                _entriesBounds.X = System.Math.Min(_entriesBounds.X, (int)position.X);
                _entriesBounds.Width = System.Math.Max(_entriesBounds.Width, entryWidth);
                _entriesBounds.Height += entryHeight;

                // move down for the next entry the size of this entry
                position.Y += entryHeight;
            }
        }

        /// <summary>
        /// Updates the menu.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Update each nested MenuEntry object.
            for (int i = 0; i < MenuEntries.Count; i++)
            {
                bool isSelected = IsActive && (i == _selectedEntry);

                MenuEntries[i].Update(this, isSelected, gameTime);
            }
        }

        /// <summary>
        /// Draws the menu.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // make sure our entries are in the right place before we draw them
            UpdateMenuEntryLocations();

            GraphicsDevice graphics = ScreenManager.GraphicsDevice;
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;

            spriteBatch.Begin();

            _entriesBounds.Inflate(20, 20);
            spriteBatch.Draw(ScreenManager.PixelTexture, _entriesBounds, new Color(0, 0, 0, 0.5f));

            // Draw each menu entry in turn.
            for (int i = 0; i < MenuEntries.Count; i++)
            {
                MenuEntry menuEntry = MenuEntries[i];

                bool isSelected = IsActive && (i == _selectedEntry);

                menuEntry.Draw(this, isSelected, gameTime);
            }

            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // Draw the menu title centered on the screen
            Vector2 titlePosition = new Vector2(graphics.Viewport.Width / 2, 80);
            Vector2 titleOrigin = font.MeasureString(_title) / 2;
            Color titleColor = new Color(192, 192, 192) * TransitionAlpha;
            float titleScale = 1.25f;

            titlePosition.Y -= transitionOffset * 100;

            spriteBatch.DrawString(font, _title, titlePosition, titleColor, 0, titleOrigin, titleScale, SpriteEffects.None, 0);
            spriteBatch.DrawString(font, ErrorText, new Vector2(graphics.Viewport.Width / 2 - font.MeasureString(ErrorText).X / 2, graphics.Viewport.Height - font.MeasureString(ErrorText).Y * 2), Color.Red);

            spriteBatch.End();
        }

        #endregion
    }
}
