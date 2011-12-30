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
using Microsoft.Xna.Framework.Input;
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

        void HandleKeyPressed(object sender, EventArgs e)
        {
            var args = (KeyboardInputEventArgs)e;
            switch (args.Key)
            {
                case Keys.Enter:
                case Keys.E:
                case Keys.Space:
                case Keys.NumPad5:
                    KeySelect = true;
                    break;

                case Keys.Right:
                case Keys.D:
                case Keys.NumPad6:
                    KeyNext = true;
                    break;

                case Keys.Left:
                case Keys.A:
                case Keys.NumPad4:
                    KeyPrevious = true;
                    break;

                case Keys.Escape:
                    KeyCancel = true;
                    KeyPause = true;
                    break;

                case Keys.Back:
                    KeyCancel = true;
                    break;

                case Keys.Up:
                case Keys.PageUp:
                case Keys.W:
                case Keys.NumPad8:
                    KeyUp = true;
                    break;

                case Keys.Down:
                case Keys.PageDown:
                case Keys.S:
                case Keys.NumPad2:
                    KeyDown = true;
                    break;

                case Keys.Pause:
                case Keys.F10:
                    KeyPause = true;
                    break;

                case Keys.Tab:
                    if (args.Modifier != KeyModifier.Shift)
                    {
                        KeyPrevious = true;
                    }
                    else
                    {
                        KeyNext = true;
                    }
                    break;

                default:
                    break;
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
