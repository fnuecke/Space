#region File Description
//-----------------------------------------------------------------------------
// InputState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
//using Microsoft.Xna.Framework.Input.Touch;
using System.Collections.Generic;
#endregion

namespace GameStateManagement
{
    /// <summary>
    /// Helper for reading input from keyboard, gamepad, and touch input. This class 
    /// tracks both the current and previous state of the input devices, and implements 
    /// query methods for high level input actions such as "move up through the menu"
    /// or "pause the game".
    /// </summary>
    public class InputState
    {
        #region Fields

        public const int MaxInputs = 4;

        public  KeyboardState CurrentKeyboardState;
        public  GamePadState CurrentGamePadState;

        public  KeyboardState LastKeyboardState;
        public  GamePadState LastGamePadState;

        public  bool GamePadWasConnected;

        //public TouchCollection TouchState;

        //public readonly List<GestureSample> Gestures = new List<GestureSample>();

        #endregion

        #region Initialization


        /// <summary>
        /// Constructs a new input state.
        /// </summary>
        public InputState()
        {
        }


        #endregion

        #region Public Methods


        /// <summary>
        /// Reads the latest state of the keyboard and gamepad.
        /// </summary>
        public void Update()
        {
            
                LastKeyboardState = CurrentKeyboardState;
                LastGamePadState = CurrentGamePadState;

                CurrentKeyboardState = Keyboard.GetState();
                CurrentGamePadState = GamePad.GetState(0);

                // Keep track of whether a gamepad has ever been
                // connected, so we can detect if it is unplugged.
                if (CurrentGamePadState.IsConnected)
                {
                    GamePadWasConnected = true;
                }
            

            //TouchState = TouchPanel.GetState();

            //Gestures.Clear();
            //while (TouchPanel.IsGestureAvailable)
            //{
            //    Gestures.Add(TouchPanel.ReadGesture());
            //}
        }

        public Keys[] getPressedKey()
        {
            return CurrentKeyboardState.GetPressedKeys();
        }
        /// <summary>
        /// Helper for checking if a key was newly pressed during this update. The
        /// controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When a keypress
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsNewKeyPress(Keys key)
        {          

                return (CurrentKeyboardState.IsKeyDown(key) &&
                        LastKeyboardState.IsKeyUp(key));
           
        }


        /// <summary>
        /// Helper for checking if a button was newly pressed during this update.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When a button press
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsNewButtonPress(Buttons button)
        {
            

                return (CurrentGamePadState.IsButtonDown(button) &&
                        LastGamePadState.IsButtonUp(button));
            
        }


        /// <summary>
        /// Checks for a "menu select" input action.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When the action
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsMenuSelect()
        {
            return IsNewKeyPress(Keys.Space) ||
                   IsNewKeyPress(Keys.Enter) ||
                   IsNewButtonPress(Buttons.A) ||
                   IsNewButtonPress(Buttons.Start);
        }

        public bool IsMenuNext()
        {
            return IsNewKeyPress(Keys.D) ||
                  IsNewKeyPress(Keys.Right) ||
                  IsNewButtonPress(Buttons.LeftThumbstickRight);
        }

        public bool IsMenuPrev()
        {
            return IsNewKeyPress(Keys.A) ||
                  IsNewKeyPress(Keys.Left) ||
                  IsNewButtonPress(Buttons.LeftThumbstickLeft);
        }
        /// <summary>
        /// Checks for a "menu cancel" input action.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When the action
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsMenuCancel()
        {
            return IsNewKeyPress(Keys.Escape) ||
                   IsNewButtonPress(Buttons.B) ||
                   IsNewButtonPress(Buttons.Back);
        }


        /// <summary>
        /// Checks for a "menu up" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsMenuUp()
        {
            
            return IsNewKeyPress(Keys.Up) ||
                   IsNewButtonPress(Buttons.DPadUp) ||
                   IsNewButtonPress(Buttons.LeftThumbstickUp);
        }


        /// <summary>
        /// Checks for a "menu down" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsMenuDown()
        {
            
            return IsNewKeyPress(Keys.Down) ||
                   IsNewButtonPress(Buttons.DPadDown) ||
                   IsNewButtonPress(Buttons.LeftThumbstickDown);
        }


        /// <summary>
        /// Checks for a "pause the game" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsPauseGame()
        {
            return IsNewKeyPress(Keys.Escape ) ||
                   IsNewButtonPress(Buttons.Back) ||
                   IsNewButtonPress(Buttons.Start);
        }


        #endregion
    }
}
