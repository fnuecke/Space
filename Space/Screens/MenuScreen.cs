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

namespace GameStateManagement
{
    /// <summary>
    /// Base class for screens that contain a menu of options. The user can
    /// move up and down to select an entry, or cancel to back out of the screen.
    /// </summary>
    abstract class MenuScreen : GameScreen
    {
        #region Properties

        /// <summary>
        /// Gets the list of menu entries, so derived classes can add
        /// or change the menu contents.
        /// </summary>
        protected IList<MenuEntry> MenuEntries { get; private set; }

        public string ErrorText { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The entry currently selected in this menu.
        /// </summary>
        private int selectedEntry = 0;

        /// <summary>
        /// The title of this menu.
        /// </summary>
        private string menuTitle;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuScreen(string menuTitle)
        {
            this.menuTitle = menuTitle;
            ErrorText = "";
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            MenuEntries = new List<MenuEntry>();
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Responds to user input, changing the selected entry and accepting
        /// or canceling the menu.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            // Move to the previous menu entry?
            if (input.KeyUp && !MenuEntries[selectedEntry].Locked)
            {
                MenuEntries[selectedEntry].SetFocused(false);
                selectedEntry--;

                if (selectedEntry < 0)
                    selectedEntry = MenuEntries.Count - 1;
                MenuEntries[selectedEntry].SetFocused(true);
                OnPrev();
                OnMenuChange();
            }

            // Move to the next menu entry?
            else if (input.KeyDown && !MenuEntries[selectedEntry].Locked)
            {
                MenuEntries[selectedEntry].SetFocused(false);

                selectedEntry++;

                if (selectedEntry >= MenuEntries.Count)
                    selectedEntry = 0;
                MenuEntries[selectedEntry].SetFocused(true);
                OnNext();
                OnMenuChange();
            }
            else if (input.KeyNext)
            {
                MenuEntries[selectedEntry].OnNextEntrySelected();
            }
            else if (input.KeyPrevious)
            {
                MenuEntries[selectedEntry].OnPreviousEntrySelected();
            }
            // Accept or cancel the menu? We pass in our ControllingPlayer, which may
            // either be null (to accept input from any player) or a specific index.
            // If we pass a null controlling player, the InputState helper returns to
            // us which player actually provided the input. We pass that through to
            // OnSelectEntry and OnCancel, so they can tell which player triggered them.


            else if (input.KeySelect)
            {
                OnSelectEntry(selectedEntry);
            }
            else if (input.KeyCancel)
            {
                HandleCancel();
            }

            //Mouse stuff
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            Vector2 position = new Vector2(0f, 175f);
            // ReSharper disable PossibleLossOfFraction
            position.Y -= ScreenManager.Font.LineSpacing / 2;
            // ReSharper restore PossibleLossOfFraction
            var mouseX = input.MousePosition.X;
            var mouseY = input.MousePosition.Y;
            // update each menu entry's location in turn
            bool hover = false;

            for (int i = 0; i < MenuEntries.Count; i++)
            {
                MenuEntry menuEntry = MenuEntries[i];
                int menuWidth = menuEntry.GetWidth(this);
                int menuHeight = menuEntry.GetHeight(this);
                // Console.WriteLine(menuHeight);
                // each entry is to be centered horizontally
                position.X = ScreenManager.GraphicsDevice.Viewport.Width / 2 - menuEntry.GetWidth(this) / 2;

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

                //check if mouse is within bounds
                if (mouseX > position.X && mouseX < position.X + menuWidth
                    && mouseY > position.Y && mouseY < position.Y + menuHeight)
                {
                    //hovering
                    hover = true;
                    //only update for new entry
                    if (i != selectedEntry && !MenuEntries[selectedEntry].Locked)
                    {
                        MenuEntries[selectedEntry].SetFocused(false);
                        selectedEntry = i;
                        MenuEntries[selectedEntry].SetFocused(true);

                        OnMenuChange();
                    }
                    break;

                }
                //if hovering treat left click as selected

                // move down for the next entry the size of this entry
                position.Y += menuHeight;
            }
            if (hover && input.MouseSelect)
            {
                OnSelectEntry(selectedEntry);
            }
        }

        /// <summary>
        /// Handler for when the user has chosen a menu entry.
        /// </summary>
        protected virtual void OnSelectEntry(int entryIndex)
        {
            ErrorText = "";
            MenuEntries[entryIndex].OnSelectEntry();
        }

        /// <summary>
        /// Handler for when the user has canceled the menu.
        /// </summary>
        protected virtual void HandleCancel()
        {
            ExitScreen();
        }

        /// <summary>
        /// Helper overload makes it easy to use OnCancel as a MenuEntry event handler.
        /// </summary>
        protected void HandleCancel(object sender, EventArgs e)
        {
            HandleCancel();
        }

        protected virtual void OnNext()
        {

        }

        protected virtual void OnPrev()
        {

        }

        /// <summary>
        /// Called if a new Menu entry is selected.
        /// </summary>
        protected virtual void OnMenuChange()
        {
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

            // update each menu entry's location in turn
            for (int i = 0; i < MenuEntries.Count; i++)
            {
                MenuEntry menuEntry = MenuEntries[i];

                // each entry is to be centered horizontally
                position.X = ScreenManager.GraphicsDevice.Viewport.Width / 2 - menuEntry.GetWidth(this) / 2;

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

                // move down for the next entry the size of this entry
                position.Y += menuEntry.GetHeight(this);
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
                bool isSelected = IsActive && (i == selectedEntry);

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

            // Draw each menu entry in turn.
            for (int i = 0; i < MenuEntries.Count; i++)
            {
                MenuEntry menuEntry = MenuEntries[i];

                bool isSelected = IsActive && (i == selectedEntry);

                menuEntry.Draw(this, isSelected, gameTime);
            }

            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // Draw the menu title centered on the screen
            Vector2 titlePosition = new Vector2(graphics.Viewport.Width / 2, 80);
            Vector2 titleOrigin = font.MeasureString(menuTitle) / 2;
            Color titleColor = new Color(192, 192, 192) * TransitionAlpha;
            float titleScale = 1.25f;

            titlePosition.Y -= transitionOffset * 100;

            spriteBatch.DrawString(font, menuTitle, titlePosition, titleColor, 0,
                                   titleOrigin, titleScale, SpriteEffects.None, 0);
            spriteBatch.DrawString(font, ErrorText, new Vector2(graphics.Viewport.Width / 2 - font.MeasureString(ErrorText).X / 2, graphics.Viewport.Height - font.MeasureString(ErrorText).Y), Color.Red);

            spriteBatch.End();
        }

        #endregion
    }
}
