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
using Engine.Input;
using Microsoft.Xna.Framework.Graphics;
using Space;

namespace GameStateManagement
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    class OptionsMenuScreen : MenuScreen
    {
        #region Fields

        /// <summary>
        /// Menu entry for changing the language.
        /// </summary>
        MenuEntry languageMenuEntry;
        MenuEntry resolutionMenuEntry;
        private EditableMenueEntry playerName;
        private MenuEntry fullscreenMenuEntry;

        private Option<string> language;
        private Option<string> resolution;
        private Option<bool> fullscreen;
        static Dictionary<string, string> languages = new Dictionary<string, string>();

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen()
            : base(MenuStrings.Options)
        {
            // Create our menu entries.
            languages["en"] = MenuStrings.en;
            languages["de"] = MenuStrings.de;
            language = new Option<string>(languages);
            languageMenuEntry = new MenuEntry(string.Empty);
            playerName = new EditableMenueEntry(MenuStrings.PlayerName);
            resolutionMenuEntry = new MenuEntry(Settings.Instance.ScreenWidth + " x " + Settings.Instance.ScreenHeight);



            MenuEntry back = new MenuEntry(MenuStrings.Back);

            // Hook up menu event handlers.
            languageMenuEntry.Selected += LanguageMenuEntrySelected;
            languageMenuEntry.NextOptionSelected += LanguageMenuEntryNext;
            languageMenuEntry.PreviousOptionSelected += LanguageMenuEntryPrev;
            back.Selected += HandleCancel;

            playerName.Selected += PlayerNameSelected;
            //Graphics
            resolutionMenuEntry.NextOptionSelected += ResolutionMenuEntryNext;
            resolutionMenuEntry.PreviousOptionSelected += ResolutionMenuEntryPrev;
            resolutionMenuEntry.Selected += ResolutionMenuEntrySelected;

            var dict = new Dictionary<string, string>();
            var added = false;
            var height = Settings.Instance.ScreenHeight;
            var width = Settings.Instance.ScreenWidth;
            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                //Console.WriteLine(mode.Height + " x " + mode.Width);
                if (!dict.ContainsKey(mode.Width + "x" + mode.Height))
                {
                    if (!added && mode.Width > width)
                    {
                        if (!dict.ContainsKey(width + "x" + height))
                        {
                            dict.Add(width + "x" + height, width + " x " + height);
                        }
                        added = true;
                    }
                    else if (!added && mode.Width == width && mode.Height > height)
                    {
                        if (!dict.ContainsKey(width + "x" + height))
                            dict.Add(width + "x" + height, width + " x " + height);
                        added = true;
                    }
                    dict.Add(mode.Width + "x" + mode.Height, mode.Width + " x " + mode.Height);
                }

            }
            resolution = new Option<string>(dict);

            fullscreenMenuEntry = new MenuEntry(string.Empty);
            fullscreenMenuEntry.Selected += FullscreenMenuEntrySelected;
            fullscreenMenuEntry.NextOptionSelected += FullscreenMenuEntryNext;
            fullscreenMenuEntry.PreviousOptionSelected += FullscreenMenuEntryPrev;

            var screenDict = new Dictionary<bool, string>
                                 {
                                     {false, MenuStrings.Off},
                                     {true, MenuStrings.On}
                                 };
            fullscreen = new Option<bool>(screenDict);


            SetMenuEntryText();
            // Add entries to the menu.
            MenuEntries.Add(languageMenuEntry);
            MenuEntries.Add(playerName);
            MenuEntries.Add(resolutionMenuEntry);
            MenuEntries.Add(fullscreenMenuEntry);
            MenuEntries.Add(back);
        }

        public override void LoadContent()
        {
            var keyboard = (IKeyboardInputManager)ScreenManager.Game.Services.GetService(typeof(IKeyboardInputManager));
            if (keyboard != null)
            {
                keyboard.Pressed += playerName.HandleKeyPressed;
            }
        }
        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            languageMenuEntry.Text = MenuStrings.Language + languages[Settings.Instance.Language];
            language.SetCurrent(Settings.Instance.Language);
            resolutionMenuEntry.Text = MenuStrings.ScreenResolution + Settings.Instance.ScreenWidth + " x " + Settings.Instance.ScreenHeight;
            resolution.SetCurrent(Settings.Instance.ScreenWidth + "x" + Settings.Instance.ScreenHeight);

            fullscreen.SetCurrent(Settings.Instance.Fullscreen);
            fullscreenMenuEntry.Text = MenuStrings.Fullscreen + fullscreen.GetOption();

            playerName.SetInputText(Settings.Instance.PlayerName);
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void LanguageMenuEntrySelected(object sender, EventArgs e)
        {
            Settings.Instance.Language = language.GetKey();

            SetMenuEntryText();
        }

        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void PlayerNameSelected(object sender, EventArgs e)
        {
            if (playerName.Editable)
            {
                if (playerName.GetInputText().Length < 3)
                {
                    ErrorText = MenuStrings.NameTooShort;
                }
                else
                {
                    Settings.Instance.PlayerName = playerName.GetInputText();
                    playerName.Editable = false;
                    playerName.Locked = false;
                    SetMenuEntryText();
                }
            }
            else
            {
                playerName.Locked = true;
                playerName.Editable = true;
            }
        }

        void LanguageMenuEntryNext(object sender, EventArgs e)
        {
            languageMenuEntry.Text = MenuStrings.Language + language.GetNextOption();
        }

        void LanguageMenuEntryPrev(object sender, EventArgs e)
        {
            languageMenuEntry.Text = MenuStrings.Language + language.GetPrevOption();
        }

        void ResolutionMenuEntryNext(object sender, EventArgs e)
        {
            resolutionMenuEntry.Text = MenuStrings.ScreenResolution + resolution.GetNextOption();
        }

        void ResolutionMenuEntryPrev(object sender, EventArgs e)
        {
            resolutionMenuEntry.Text = MenuStrings.ScreenResolution + resolution.GetPrevOption();
        }

        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void ResolutionMenuEntrySelected(object sender, EventArgs e)
        {
            string[] words = resolution.GetKey().Split('x');
            Settings.Instance.ScreenWidth = int.Parse(words[0]);
            Settings.Instance.ScreenHeight = int.Parse(words[1]);

            SetMenuEntryText();
        }

        void FullscreenMenuEntryNext(object sender, EventArgs e)
        {
            fullscreenMenuEntry.Text = MenuStrings.Fullscreen + fullscreen.GetNextOption();
        }

        void FullscreenMenuEntryPrev(object sender, EventArgs e)
        {
            fullscreenMenuEntry.Text = MenuStrings.Fullscreen + fullscreen.GetPrevOption();
        }

        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void FullscreenMenuEntrySelected(object sender, EventArgs e)
        {
            Settings.Instance.Fullscreen = fullscreen.GetKey();

            SetMenuEntryText();
        }

        protected override void OnNext()
        {
            SetMenuEntryText();
        }

        protected override void OnPrev()
        {
            SetMenuEntryText();
        }

        protected override void OnMenuChange()
        {
            SetMenuEntryText();
        }

        #endregion

        #region Option

        public class Option<T>
        {
            #region Fields

            Dictionary<T, string> options = new Dictionary<T, string>();

            List<T> keyList;

            int current = 0;

            #endregion

            #region Initialization

            public Option(Dictionary<T, string> dict)
            {
                options = dict;
                keyList = new List<T>(options.Keys);
            }

            public string GetNextOption()
            {
                current = ++current % keyList.Count;
                return GetOption();

            }
            public string GetPrevOption()
            {
                current = (--current + keyList.Count) % keyList.Count;
                return GetOption();
            }
            public string GetOption()
            {
                return options[GetKey()];
            }

            public T GetKey()
            {
                return keyList[current];
            }

            public void SetCurrent(T key)
            {
                for (int i = 0; i < keyList.Count; i++)
                {
                    current = i;
                    if (keyList[i].Equals(key))
                        return;
                }
                //not found use 1;
                current = 0;
            }

            #endregion
        }

        #endregion
    }
}
