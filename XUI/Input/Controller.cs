//-----------------------------------------------
// XUI - Controller.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace XUI
{
    // E_Button
    public enum E_Button
    {
        DPadUp = 0,
        DPadDown,
        DPadLeft,
        DPadRight,
        LeftStick,
        LeftStickUp,
        LeftStickDown,
        LeftStickLeft,
        LeftStickRight,
        RightStick,
        RightStickUp,
        RightStickDown,
        RightStickLeft,
        RightStickRight,
        A,
        B,
        X,
        Y,
        Start,
        Back,
        LeftShoulder,
        RightShoulder,
        LeftTrigger,
        RightTrigger,
        BigButton,

        Count,
    };

    // class ControllerD
    public class ControllerD : Device
    {
        // ControllerD
        public ControllerD(int padIndex)
        {
            PadIndex = padIndex;
            ButtonHeldTimes = new float[(int)E_Button.Count];
            ButtonHeldTimesPrev = new float[(int)E_Button.Count];
        }

        // Update
        public override void Update(float frameTime)
        {
            FrameTime = frameTime;

            GamePadStatePrevious = GamePadState;
            GamePadState = GamePad.GetState((PlayerIndex)PadIndex);

            if (!GamePadState.IsConnected)
                return;

            // update time held
            for (int i = 0; i < (int)E_Button.Count; ++i)
            {
                ButtonHeldTimesPrev[i] = ButtonHeldTimes[i];

                if (ButtonValue(i) > 0.5f) //if ( GamePadState.IsButtonDown( (Buttons)ButtonMappings[ i ] ) )
                    ButtonHeldTimes[i] += frameTime;
                else
                    ButtonHeldTimes[i] = 0.0f;
            }

            // update vibration
            if (VibrateTimer > 0.0f)
            {
                GamePad.SetVibration((PlayerIndex)PadIndex, VibrateLeftMotor, VibrateRightMotor);

                VibrateTimer -= frameTime;

                if (VibrateTimer <= 0.0f)
                    GamePad.SetVibration((PlayerIndex)PadIndex, 0.0f, 0.0f);
            }
        }

        // ButtonDown
        public override bool ButtonDown(int button)
        {
            return GamePadState.IsButtonDown(ButtonMappings[button]);
        }

        // ButtonJustPressed
        public override bool ButtonJustPressed(int button)
        {
            return (GamePadStatePrevious.IsButtonUp(ButtonMappings[button]) && GamePadState.IsButtonDown(ButtonMappings[button]));
        }

        // ButtonJustReleased
        public override bool ButtonJustReleased(int button)
        {
            return (GamePadStatePrevious.IsButtonDown(ButtonMappings[button]) && GamePadState.IsButtonUp(ButtonMappings[button]));
        }

        // ButtonValue
        public override float ButtonValue(int button)
        {
            switch ((E_Button)button)
            {
                case E_Button.LeftStickUp: return MathHelper.Clamp(GamePadState.ThumbSticks.Left.Y, 0.0f, 1.0f);
                case E_Button.LeftStickDown: return (MathHelper.Clamp(GamePadState.ThumbSticks.Left.Y, -1.0f, 0.0f) * -1.0f);
                case E_Button.LeftStickLeft: return (MathHelper.Clamp(GamePadState.ThumbSticks.Left.X, -1.0f, 0.0f) * -1.0f);
                case E_Button.LeftStickRight: return MathHelper.Clamp(GamePadState.ThumbSticks.Left.X, 0.0f, 1.0f);

                case E_Button.RightStickUp: return MathHelper.Clamp(GamePadState.ThumbSticks.Right.Y, 0.0f, 1.0f);
                case E_Button.RightStickDown: return (MathHelper.Clamp(GamePadState.ThumbSticks.Right.Y, -1.0f, 0.0f) * -1.0f);
                case E_Button.RightStickLeft: return (MathHelper.Clamp(GamePadState.ThumbSticks.Right.X, -1.0f, 0.0f) * -1.0f);
                case E_Button.RightStickRight: return MathHelper.Clamp(GamePadState.ThumbSticks.Right.X, 0.0f, 1.0f);

                case E_Button.LeftTrigger: return GamePadState.Triggers.Left;
                case E_Button.RightTrigger: return GamePadState.Triggers.Right;

                default: return (ButtonDown(button) ? 1.0f : 0.0f);
            }
        }

        // IsConnected
        public bool IsConnected()
        {
            return (GamePadState.IsConnected);
        }

        // SetVibration
        public void SetVibration(float time, float leftMotor, float rightMotor)
        {
            VibrateTimer = time;
            VibrateLeftMotor = MathHelper.Clamp(leftMotor, 0.0f, 1.0f);
            VibrateRightMotor = MathHelper.Clamp(rightMotor, 0.0f, 1.0f);
        }

        // ResetVibration
        public void ResetVibration()
        {
            VibrateTimer = 0.0f;
            GamePad.SetVibration((PlayerIndex)PadIndex, 0.0f, 0.0f);
        }

        //
        public int PadIndex { get; private set; }

        public GamePadState GamePadState { get; private set; }
        public GamePadState GamePadStatePrevious { get; private set; }

        private float VibrateTimer;
        private float VibrateLeftMotor;
        private float VibrateRightMotor;

        // device button mappings
        private Buttons[] ButtonMappings = new Buttons[(int)E_Button.Count]
	    {
		    Buttons.DPadUp,						// E_Button.DPadUp
		    Buttons.DPadDown,					// E_Button.DPadDown
		    Buttons.DPadLeft,					// E_Button.DPadLeft
		    Buttons.DPadRight,					// E_Button.DPadRight
		    Buttons.LeftStick,					// E_Button.LeftStick
		    Buttons.LeftThumbstickUp,			// E_Button.LeftStickUp
		    Buttons.LeftThumbstickDown,			// E_Button.LeftStickDown
		    Buttons.LeftThumbstickLeft,			// E_Button.LeftStickLeft
		    Buttons.LeftThumbstickRight,		// E_Button.LeftStickRight
		    Buttons.RightStick,					// E_Button.RightStick
		    Buttons.RightThumbstickUp,			// E_Button.RightStickUp
		    Buttons.RightThumbstickDown,		// E_Button.RightStickDown
		    Buttons.RightThumbstickLeft,		// E_Button.RightStickLeft
		    Buttons.RightThumbstickRight,		// E_Button.RightStickRight
		    Buttons.A,							// E_Button.A
		    Buttons.B,							// E_Button.B
		    Buttons.X,							// E_Button.X
		    Buttons.Y,							// E_Button.Y
		    Buttons.Start,						// E_Button.Start
		    Buttons.Back,						// E_Button.Back
		    Buttons.LeftShoulder,				// E_Button.LeftShoulder
		    Buttons.RightShoulder,				// E_Button.RightShoulder
		    Buttons.LeftTrigger,				// E_Button.LeftTrigger
		    Buttons.RightTrigger,				// E_Button.RightTrigger
		    Buttons.BigButton,					// E_Button.BigButton
	    };
        //
    };
}