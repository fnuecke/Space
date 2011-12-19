#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Space;
using Space.Control;
using Space.Simulation;
using SpaceData;

#endregion

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
            : base(Strings.MainMenu)
        {
            
            // Create our menu entries.
            MenuEntry playGameMenuEntry = new MenuEntry(Strings.join);
            MenuEntry startServerMenuEntry = new MenuEntry(Strings.host);
            MenuEntry optionsMenuEntry = new MenuEntry(Strings.Options);
            MenuEntry exitMenuEntry = new MenuEntry(Strings.Exit);

            // Hook up menu event handlers.
            playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
            optionsMenuEntry.Selected += OptionsMenuEntrySelected;
            startServerMenuEntry.Selected += StartServerMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

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
        void PlayGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.RestartClient();

            ScreenManager.AddScreen(new ConnectScreen(ScreenManager.Client));
        }

        void StartServerMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.RestartServer();
            ScreenManager.RestartClient();

            // Autojoin self.
            PlayerInfo info = new PlayerInfo();
            info.Ship = this.ScreenManager.Game.Content.Load<ShipData[]>("Data/ships")[0];
            ScreenManager.Client.Session.Join(ScreenManager.Server.Session, Settings.Instance.PlayerName, info);

            LoadingScreen.Load(ScreenManager, true, new GameplayScreen());
        }

        /// <summary>
        /// Event handler for when the Options menu entry is selected.
        /// </summary>
        void OptionsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen());
        }


        /// <summary>
        /// When the user cancels the main menu, ask if they want to exit the sample.
        /// </summary>
        protected override void OnCancel()
        {
            const string message = "Are you sure you want to exit this sample?";

            MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen(message);

            confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmExitMessageBox);
        }


        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to exit" message box.
        /// </summary>
        void ConfirmExitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.Game.Exit();
        }

        #endregion

        #region Utility Methods
        
        

        #endregion
    }

}
