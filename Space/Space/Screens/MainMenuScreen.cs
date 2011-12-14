#region File Description
//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Space;
using Space.Control;
using System.Net;
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

        private GameServer server;

        private GameClient Client;
        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen()
            : base(Strings.MainMenu)
        {
            
            // Create our menu entries.
            MenuEntry playGameMenuEntry = new MenuEntry("Play Game");
            MenuEntry startServerMenuEntry = new MenuEntry("Start Server");
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
            if (server != null)
            {
                PlayerInfo info = new PlayerInfo();
                info.ShipType = "Sparrow";
                Client.Session.Join(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 50100), Settings.Instance.PlayerName, info);
                LoadingScreen.Load(ScreenManager, true,
                                   new GameplayScreen(Client));
            }
            else ScreenManager.AddScreen(new ConnectScreen(Client));
        }
        void StartServerMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            RestartServer();
        }
        private void RestartServer()
        {
            if (server != null)
            {
                server.Dispose();
                ScreenManager.Game.Components.Remove(server);
            }
            server = new GameServer(ScreenManager.Game);
            ScreenManager.Game.Components.Add(server);
        }
        private void RestartClient()
        {
            if (Client != null)
            {
                Client.Dispose();
                ScreenManager.Game.Components.Remove(Client);
            }
            Client = new GameClient(ScreenManager.Game);
            ScreenManager.Game.Components.Add(Client);
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
    }

}
