#region File Description
//-----------------------------------------------------------------------------
// InputState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
//using Microsoft.Xna.Framework.Input.Touch;
using Engine.Input;
using Microsoft.Xna.Framework;

#endregion

namespace Space.ScreenManagement
{
    /// <summary>
    /// Helper for reading input from keyboard, gamepad, and touch input. This class 
    /// tracks both the current and previous state of the input devices, and implements 
    /// query methods for high level input actions such as "move up through the menu"
    /// or "pause the game".
    /// </summary>
    public class InputState
    {
        public bool KeySelect { get; private set; }
        public bool KeyNext { get; private set; }
        public bool KeyPrevious { get; private set; }
        public bool KeyCancel { get; private set; }
        public bool KeyUp { get; private set; }
        public bool KeyDown { get; private set; }
        public bool KeyPause { get; private set; }

        public bool MouseSelect { get; private set; }
        public Vector2 MousePosition { get; private set; }

        #region Initialization

        /// <summary>
        /// Constructs a new input state.
        /// </summary>
        public InputState(Game game)
        {
            var keyboard = (IKeyboardInputManager)game.Services.GetService(typeof(IKeyboardInputManager));
            var mouse = (IMouseInputManager)game.Services.GetService(typeof(IMouseInputManager));

            keyboard.Pressed += HandleKeyPressed;
            mouse.Pressed += HandleMousePressed;
            mouse.Scrolled += HandleMouseScrolled;
            mouse.Moved += HandleMouseMoved;
        }

        void HandleKeyPressed(object sender, KeyboardInputEventArgs e)
        {
            if (Settings.Instance.MenuBindings.ContainsKey(e.Key))
            {
                switch (Settings.Instance.MenuBindings[e.Key])
                {
                    case Settings.MenuCommand.Up:
                        KeyUp = true;
                        break;
                    case Settings.MenuCommand.Down:
                        KeyDown = true;
                        break;
                    case Settings.MenuCommand.Next:
                        KeyNext = true;
                        break;
                    case Settings.MenuCommand.Previous:
                        KeyPrevious = true;
                        break;
                    case Settings.MenuCommand.Select:
                        KeySelect = true;
                        break;
                    case Settings.MenuCommand.Back:
                        KeyCancel = true;
                        KeyPause = true;
                        break;
                    case Settings.MenuCommand.Pause:
                        KeyPause = true;
                        break;

                    default:
                        break;
                }
            }
        }

        void HandleMousePressed(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;
            if (args.Button == MouseInputEventArgs.MouseButton.Left)
            {
                MouseSelect = true;
            }
            else if (args.Button == MouseInputEventArgs.MouseButton.Right)
            {
                KeyCancel = true;
            }
        }

        void HandleMouseScrolled(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;
            if (args.ScrollDelta < 0)
            {
                KeyNext = true;
            }
            else
            {
                KeyPrevious = true;
            }
        }

        void HandleMouseMoved(object sender, EventArgs e)
        {
            var args = (MouseInputEventArgs)e;
            MousePosition = new Vector2(args.X, args.Y);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets states.
        /// </summary>
        public void Update()
        {
            KeySelect = false;
            KeyNext = false;
            KeyPrevious = false;
            KeyCancel = false;
            KeyUp = false;
            KeyDown = false;
            KeyPause = false;
            MouseSelect = false;
        }

        #endregion
    }
}
