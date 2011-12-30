#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Space;

namespace GameStateManagement
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    class MainMenuScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen()
            : base(MenuStrings.MainMenu)
        {
            // Create our menu entries.
            MenuEntry playGameMenuEntry = new MenuEntry(MenuStrings.JoinGame);
            MenuEntry startServerMenuEntry = new MenuEntry(MenuStrings.HostGame);
            MenuEntry optionsMenuEntry = new MenuEntry(MenuStrings.Options);
            MenuEntry exitMenuEntry = new MenuEntry(MenuStrings.Exit);

            // Hook up menu event handlers.
            playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
            optionsMenuEntry.Selected += OptionsMenuEntrySelected;
            startServerMenuEntry.Selected += StartServerMenuEntrySelected;
            exitMenuEntry.Selected += HandleCancel;

            // Add entries to the menu.
            MenuEntries.Add(playGameMenuEntry);
            MenuEntries.Add(startServerMenuEntry);
            MenuEntries.Add(optionsMenuEntry);
            MenuEntries.Add(exitMenuEntry);
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void PlayGameMenuEntrySelected(object sender, EventArgs e)
        {
            // Just create the client, allow looking for remote games and
            // joining them after that.
            ((Spaaace)ScreenManager.Game).RestartClient();

            // Transition to connect screen.
            ScreenManager.AddScreen(new ConnectScreen());
        }

        void StartServerMenuEntrySelected(object sender, EventArgs e)
        {
            // Create and join.
            ((Spaaace)ScreenManager.Game).RestartServer();
            ((Spaaace)ScreenManager.Game).RestartClient(true);

            // Directly transition to in-game screen.
            LoadingScreen.Load(ScreenManager, true, new GameplayScreen());
        }

        /// <summary>
        /// Event handler for when the Options menu entry is selected.
        /// </summary>
        void OptionsMenuEntrySelected(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen());
        }

        /// <summary>
        /// When the user cancels the main menu, ask if they want to exit the sample.
        /// </summary>
        protected override void HandleCancel()
        {
            ScreenManager.Game.Exit();
        }

        #endregion
    }
}
