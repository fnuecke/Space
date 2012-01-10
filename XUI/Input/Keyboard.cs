//-----------------------------------------------
// XUI - Keyboard.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework.Input;

namespace XUI
{
    // class KeyboardD
    public class KeyboardD : Device
    {
        // KeyboardD
        public KeyboardD()
        {
            ButtonHeldTimes = new float[256];
            ButtonHeldTimesPrev = new float[256];
        }

        // Update
        public override void Update(float frameTime)
        {
            FrameTime = frameTime;

            KeyboardStatePrevious = KeyboardState;
            KeyboardState = Keyboard.GetState();

            // update time held
            for (int i = 0; i < 256; ++i)
            {
                ButtonHeldTimesPrev[i] = ButtonHeldTimes[i];

                if (KeyboardState.IsKeyDown((Keys)i))
                    ButtonHeldTimes[i] += frameTime;
                else
                    ButtonHeldTimes[i] = 0.0f;
            }
        }

        // ButtonDown
        public override bool ButtonDown(int button)
        {
            return (KeyboardState.IsKeyDown((Keys)button));
        }

        // ButtonJustPressed
        public override bool ButtonJustPressed(int button)
        {
            return (KeyboardStatePrevious.IsKeyUp((Keys)button) && KeyboardState.IsKeyDown((Keys)button));
        }

        // ButtonJustReleased
        public override bool ButtonJustReleased(int button)
        {
            return (KeyboardStatePrevious.IsKeyDown((Keys)button) && KeyboardState.IsKeyUp((Keys)button));
        }

        // ButtonValue
        public override float ButtonValue(int button)
        {
            return (ButtonDown(button) ? 1.0f : 0.0f);
        }

        //
        public KeyboardState KeyboardState { get; private set; }
        public KeyboardState KeyboardStatePrevious { get; private set; }
        //
    };
}