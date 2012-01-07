#region File Description
//-----------------------------------------------------------------------------
// PauseMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Space.ScreenManagement.Screens.Entries;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// The pause menu comes up over the top of the game,
    /// giving the player options to resume or quit.
    /// </summary>
    class PauseMenuScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public PauseMenuScreen()
            : base("Paused")
        {
            // Create our menu entries.
            var resume = new MenuEntry("Resume Game");
            var quit = new MenuEntry("Quit Game");

            // Hook up menu event handlers.
            resume.Activated += delegate(object sender, EventArgs e)
            {
                ExitScreen();
            };
            quit.Activated += QuitGameMenuEntrySelected;

            // Add entries to the menu.
            MenuEntries.Add(resume);
            MenuEntries.Add(quit);

            SetEscapeEntry(resume);
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Event handler for when the Quit Game menu entry is selected.
        /// </summary>
        void QuitGameMenuEntrySelected(object sender, EventArgs e)
        {
            string message = MenuStrings.QuitGameConfirmation;

            MessageBoxScreen confirmQuitMessageBox = new MessageBoxScreen(message);

            confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmQuitMessageBox);
        }

        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to quit" message box. This uses the loading screen to
        /// transition from the game back to the main menu screen.
        /// </summary>
        void ConfirmQuitMessageBoxAccepted(object sender, EventArgs e)
        {
            ((Spaaace)ScreenManager.Game).DisposeControllers();

            LoadingScreen.Load(ScreenManager, false, null, new BackgroundScreen(),
                                                           new MainMenuScreen());
        }

        #endregion
    }
}
