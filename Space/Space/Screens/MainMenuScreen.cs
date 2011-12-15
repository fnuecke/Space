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
using Space.Model;

#endregion

namespace GameStateManagement
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    class MainMenuScreen : MenuScreen
    {

       

        #region Initialization

        private GameServer _server;
        private GameClient _client;

        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen()
            : base(Strings.MainMenu)
        {
            
            // Create our menu entries.
            MenuEntry playGameMenuEntry = new MenuEntry(Strings.join);
            MenuEntry startServerMenuEntry = new MenuEntry(Strings.host);
            MenuEntry optionsMenuEntry = new MenuEntry("Options");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

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
            RestartClient();

            ScreenManager.AddScreen(new ConnectScreen(_client));
        }

        void StartServerMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            RestartServer();
            RestartClient();

            // Autojoin self.
            PlayerInfo info = new PlayerInfo();
            info.ShipType = "Sparrow";
            _client.Session.Join(_server.Session, "player", info);

            LoadingScreen.Load(ScreenManager, true, new GameplayScreen(_client));
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
        
        private void RestartServer()
        {
            if (_server != null)
            {
                _server.Dispose();
                ScreenManager.Game.Components.Remove(_server);
            }
            _server = new GameServer(ScreenManager.Game);
            ScreenManager.Game.Components.Add(_server);
        }

        private void RestartClient()
        {
            if (_client != null)
            {
                _client.Dispose();
                ScreenManager.Game.Components.Remove(_client);
            }
            _client = new GameClient(ScreenManager.Game);
            ScreenManager.Game.Components.Add(_client);
        }

        #endregion
    }

}
