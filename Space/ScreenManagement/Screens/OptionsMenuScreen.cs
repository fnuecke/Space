#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nuclex.Input.Devices;
using Space.ScreenManagement.Screens.Entries;
using Space.Util;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    sealed class OptionsMenuScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen(Game game)
            : base(MenuStrings.Options)
        {
            var playerName = new EditableMenuEntry(MenuStrings.PlayerName, (IKeyboard)game.Services.GetService(typeof(IKeyboard)), Settings.Instance.PlayerName);
            var language = new OptionMenuEntry<string>(MenuStrings.Language, new Dictionary<string, string>()
            {
                { "en", MenuStrings.English },
                { "de", MenuStrings.German }
            }, Settings.Instance.Language);
            var displayModes = new Dictionary<Tuple<int, int>, string>();
            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                var tuple = Tuple.Create(mode.Width, mode.Height);
                if (!displayModes.ContainsKey(tuple))
                {
                    displayModes.Add(tuple, tuple.Item1 + "x" + tuple.Item2);
                }
            }
            var resolution = new OptionMenuEntry<Tuple<int, int>>(MenuStrings.ScreenResolution,
                displayModes, Tuple.Create(Settings.Instance.ScreenWidth, Settings.Instance.ScreenHeight));
            var fullscreen = new OptionMenuEntry<bool>(MenuStrings.Fullscreen, new Dictionary<bool, string>
            {
                { false, MenuStrings.Off },
                { true, MenuStrings.On }
            }, Settings.Instance.Fullscreen);
            var postprocessing = new OptionMenuEntry<bool>(MenuStrings.PostProcessing, new Dictionary<bool, string>
            {
                { false, MenuStrings.Off },
                { true, MenuStrings.On }
            }, Settings.Instance.PostProcessing);
            MenuEntry back = new MenuEntry(MenuStrings.Back);

            fullscreen.Changed += delegate(object sender, EventArgs e)
            {
                Settings.Instance.Fullscreen = fullscreen.Value;
            };
            postprocessing.Changed += delegate(object sender, EventArgs e)
            {
                Settings.Instance.PostProcessing = postprocessing.Value;
            };
            resolution.Changed += delegate(object sender, EventArgs e)
            {
                Settings.Instance.ScreenWidth = resolution.Value.Item1;
                Settings.Instance.ScreenHeight = resolution.Value.Item2;
            };
            language.Changed += delegate(object sender, EventArgs e)
            {
                Settings.Instance.Language = language.Value;
            };
            playerName.Changed += delegate(object sender, EventArgs e)
            {
                if (playerName.InputText.Length < 3)
                {
                    ErrorText = MenuStrings.NameTooShort;
                    playerName.Text = Settings.Instance.PlayerName;
                }
                else
                {
                    Settings.Instance.PlayerName = playerName.InputText;
                }
            };
            back.Activated += delegate(object sender, EventArgs e)
            {
                ExitScreen();
            };

            // Add entries to the menu.
            MenuEntries.Add(playerName);
            MenuEntries.Add(language);
            MenuEntries.Add(resolution);
            MenuEntries.Add(fullscreen);
            MenuEntries.Add(postprocessing);
            MenuEntries.Add(back);

            SetEscapeEntry(back);
        }

        #endregion
    }
}
