#region File Description
//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements

using Engine.Input;
using Microsoft.Xna.Framework;
using Space;
using System.Collections.Generic;
#endregion

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

        MenuEntry languageMenuEntry;
        private EditableMenueEntry playerName;
        

      

        static Dictionary<string,string> languages= new Dictionary<string,string>();

    
        static int currentLanguage = 0;

            

        static bool frobnicate = true;

        static int elf = 23;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen()
            : base("Options")
        {
            // Create our menu entries.
            languages["en"] = Strings.en;
            languages["de"] = Strings.de;
            
            languageMenuEntry = new MenuEntry(string.Empty);
            playerName = new EditableMenueEntry(Strings.playerName);
            
            SetMenuEntryText();

            MenuEntry back = new MenuEntry("Back");

            // Hook up menu event handlers.
            languageMenuEntry.Selected += LanguageMenuEntrySelected;
            languageMenuEntry.next += LanguageMenuEntryNext;
            back.Selected += OnCancel;

            playerName.Selected += PlayerNameSelected;
            
            // Add entries to the menu.
            MenuEntries.Add(languageMenuEntry);
            MenuEntries.Add(playerName);
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
           languageMenuEntry.Text = "Language: " + languages[Settings.Instance.Language];
            playerName.SetInputText(Settings.Instance.PlayerName);
        }


        #endregion

        #region Handle Input


       


        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void LanguageMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            
            
            currentLanguage = (currentLanguage + 1) % languages.Count;

            SetMenuEntryText();
        }

        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void PlayerNameSelected(object sender, PlayerIndexEventArgs e)
        {


            Settings.Instance.PlayerName = playerName.GetInputText();

            SetMenuEntryText();
        }
        void LanguageMenuEntryNext(object sender, PlayerIndexEventArgs e)
        {


            currentLanguage = (currentLanguage + 1) % languages.Count;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Frobnicate menu entry is selected.
        /// </summary>
        void FrobnicateMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            frobnicate = !frobnicate;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Elf menu entry is selected.
        /// </summary>
        void ElfMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            elf++;

            SetMenuEntryText();
        }


        #endregion
    }
}
