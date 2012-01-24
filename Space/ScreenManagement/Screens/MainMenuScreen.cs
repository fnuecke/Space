using System;
using Space.ScreenManagement.Screens.Entries;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    sealed class MainMenuScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen()
            : base(MenuStrings.MainMenu)
        {
            // Create our menu entries.
            var join = new MenuEntry(MenuStrings.JoinGame);
            var host = new MenuEntry(MenuStrings.HostGame);
            var options = new MenuEntry(MenuStrings.Options);
            var exit = new MenuEntry(MenuStrings.Exit);

            // Hook up menu event handlers.
            join.Activated += HandleJoinActivated;
            options.Activated += HandleOptionsActivated;
            host.Activated += HandleHostActivated;
            exit.Activated += delegate(object sender, EventArgs e)
            {
                ScreenManager.Game.Exit();
            };

            // Add entries to the menu.
            MenuEntries.Add(join);
            MenuEntries.Add(host);
            MenuEntries.Add(options);
            MenuEntries.Add(exit);

            SetEscapeEntry(exit);
        }

        #endregion

        #region Handle Input

        public override void HandleInput(InputState input)
        {
            base.HandleInput(input);

            if (input.KeyCancel)
            {
                ScreenManager.Game.Exit();
            }
        }

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void HandleJoinActivated(object sender, EventArgs e)
        {
            // Just create the client, allow looking for remote games and
            // joining them after that.
            ((Spaaace)ScreenManager.Game).RestartClient();

            // Transition to connect screen.
            ScreenManager.AddScreen(new ConnectScreen(ScreenManager.Game));
        }

        void HandleHostActivated(object sender, EventArgs e)
        {
            // Create and join.
            ((Spaaace)ScreenManager.Game).RestartServer();
            ((Spaaace)ScreenManager.Game).RestartClient(true);

            // Directly transition to in-game screen.
            LoadingScreen.Load(ScreenManager, true, new GameplayScreen(((Spaaace)ScreenManager.Game).Client));
        }

        /// <summary>
        /// Event handler for when the Options menu entry is selected.
        /// </summary>
        void HandleOptionsActivated(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen(ScreenManager.Game));
        }

        #endregion
    }
}
