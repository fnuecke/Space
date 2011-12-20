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
using System;
using Microsoft.Xna.Framework.Graphics;
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
        MenuEntry resolutionMenuEntry;
        private EditableMenueEntry playerName;

        private Option language;
        private Option resolution;

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
            : base(Strings.Options)
        {
            // Create our menu entries.
            languages["en"] = Strings.en;
            languages["de"] = Strings.de;
            language = new Option(languages);
            languageMenuEntry = new MenuEntry(string.Empty);
            playerName = new EditableMenueEntry(Strings.playerName);
            resolutionMenuEntry = new MenuEntry(Settings.Instance.ScreenWidth + " x " + Settings.Instance.ScreenHeight);
           
            

            MenuEntry back = new MenuEntry(Strings.Back);

            // Hook up menu event handlers.
            languageMenuEntry.Selected += LanguageMenuEntrySelected;
            languageMenuEntry.next += LanguageMenuEntryNext;
            languageMenuEntry.prev += LanguageMenuEntryPrev;
            back.Selected += OnCancel;

            playerName.Selected += PlayerNameSelected;
            //Graphics
             resolutionMenuEntry.next += ResolutionMenuEntryNext;
            resolutionMenuEntry.prev += ResolutionMenuEntryPrev;
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
                        dict.Add(width + "x" + height, width + " x " + height);
                        added = true;
                    }
                        dict.Add(mode.Width + "x" + mode.Height, mode.Width + " x " + mode.Height);
                }
                    
            }
            resolution = new Option(dict);

            SetMenuEntryText();
            // Add entries to the menu.
            MenuEntries.Add(languageMenuEntry);
            MenuEntries.Add(playerName);
            MenuEntries.Add(resolutionMenuEntry);
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
            languageMenuEntry.Text = Strings.language + languages[Settings.Instance.Language];
            language.SetCurrent(Settings.Instance.Language);
            resolutionMenuEntry.Text = Strings.Resolution + Settings.Instance.ScreenWidth + " x " + Settings.Instance.ScreenHeight;
            resolution.SetCurrent(Settings.Instance.ScreenWidth + "x" + Settings.Instance.ScreenHeight);
            playerName.SetInputText(Settings.Instance.PlayerName);
        }


        #endregion

        #region Handle Input


       


        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void LanguageMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {


            Settings.Instance.Language = language.GetKey();

            SetMenuEntryText();
        }

        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void PlayerNameSelected(object sender, PlayerIndexEventArgs e)
        {

            if (playerName.Editable)
            {
                if (playerName.GetInputText().Length < 3)
                {
                    ErrorText = Strings.NameToShort;
                }
                else
                {


                    Settings.Instance.PlayerName = playerName.GetInputText();
                    playerName.Editable = false;
                    SetMenuEntryText();
                }
            }
            else
            {
                playerName.Editable = true;
            }
        }
        void LanguageMenuEntryNext(object sender, PlayerIndexEventArgs e)
        {

            

            languageMenuEntry.Text = Strings.language+ language.GetNextOption();

            
        }
        void LanguageMenuEntryPrev(object sender, PlayerIndexEventArgs e)
        {



            languageMenuEntry.Text = Strings.language + language.GetPrevOption();


        }

        void ResolutionMenuEntryNext(object sender, PlayerIndexEventArgs e)
        {



            resolutionMenuEntry.Text = Strings.Resolution + resolution.GetNextOption();


        }
        void ResolutionMenuEntryPrev(object sender, PlayerIndexEventArgs e)
        {



            resolutionMenuEntry.Text = Strings.Resolution + resolution.GetPrevOption();


        }
        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void ResolutionMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {


            string[] words = resolution.GetKey().Split('x');
            Settings.Instance.ScreenWidth = int.Parse(words[0]);
            Settings.Instance.ScreenHeight = int.Parse(words[1]);

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

        #region Option
        public class Option
        {

            #region Fields
            Dictionary<string, string> options = new Dictionary<string, string>();
            List<string> keyList;
            int current = 0;
            #endregion

            #region Initialization

            public Option(Dictionary<string, string> dict)
            {
                options = dict;
                keyList = new List<string>(options.Keys);
            }

            public string GetNextOption()
            {
                current = ++current % keyList.Count;
                return GetOption();
                
            }
            public string GetPrevOption()
            {
                current = (--current+keyList.Count) % keyList.Count;
                return GetOption();

            }
            public string GetOption()
            {
                return options[GetKey()];

            }

            public string GetKey()
            {
                return keyList[current];
            }

            public void SetCurrent(string key)
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
