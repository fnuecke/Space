//-----------------------------------------------
// XUI - Mouse.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace XUI
{
    // E_MouseButton
    public enum E_MouseButton
    {
        Left = 0,
        Middle,
        Right,
        XButton1,
        XButton2,

        Count,
    };

    // class MouseD
    public class MouseD : Device
    {
        // MouseD
        public MouseD()
        {
            ButtonHeldTimes = new float[(int)E_MouseButton.Count];
            ButtonHeldTimesPrev = new float[(int)E_MouseButton.Count];
        }

        // Update
        public override void Update(float frameTime)
        {
            FrameTime = frameTime;

            MouseStatePrevious = MouseState;
            MouseState = Mouse.GetState();

            // update time held
            for (int i = 0; i < (int)E_MouseButton.Count; ++i)
            {
                bool isDown = false;
                bool isDownPrevious = false;

                switch ((E_MouseButton)i)
                {
                    case E_MouseButton.Left:
                        isDown = (MouseState.LeftButton == ButtonState.Pressed);
                        isDownPrevious = (MouseStatePrevious.LeftButton == ButtonState.Pressed);
                        break;

                    case E_MouseButton.Middle:
                        isDown = (MouseState.MiddleButton == ButtonState.Pressed);
                        isDownPrevious = (MouseStatePrevious.MiddleButton == ButtonState.Pressed);
                        break;

                    case E_MouseButton.Right:
                        isDown = (MouseState.RightButton == ButtonState.Pressed);
                        isDownPrevious = (MouseStatePrevious.RightButton == ButtonState.Pressed);
                        break;

                    case E_MouseButton.XButton1:
                        isDown = (MouseState.XButton1 == ButtonState.Pressed);
                        isDownPrevious = (MouseStatePrevious.XButton1 == ButtonState.Pressed);
                        break;

                    case E_MouseButton.XButton2:
                        isDown = (MouseState.XButton2 == ButtonState.Pressed);
                        isDownPrevious = (MouseStatePrevious.XButton2 == ButtonState.Pressed);
                        break;

                    default:
                        break;
                }

                ButtonHeldTimesPrev[i] = ButtonHeldTimes[i];

                if (isDown)
                    ButtonHeldTimes[i] += frameTime;
                else
                    ButtonHeldTimes[i] = 0.0f;
            }
        }

        // ButtonDown
        public override bool ButtonDown(int button)
        {
            switch ((E_MouseButton)button)
            {
                case E_MouseButton.Left: return (MouseState.LeftButton == ButtonState.Pressed);
                case E_MouseButton.Middle: return (MouseState.MiddleButton == ButtonState.Pressed);
                case E_MouseButton.Right: return (MouseState.RightButton == ButtonState.Pressed);
                case E_MouseButton.XButton1: return (MouseState.XButton1 == ButtonState.Pressed);
                case E_MouseButton.XButton2: return (MouseState.XButton2 == ButtonState.Pressed);
                default: return false;
            }
        }

        // ButtonJustPressed
        public override bool ButtonJustPressed(int button)
        {
            switch ((E_MouseButton)button)
            {
                case E_MouseButton.Left: return ((MouseStatePrevious.LeftButton == ButtonState.Released) && (MouseState.LeftButton == ButtonState.Pressed));
                case E_MouseButton.Middle: return ((MouseStatePrevious.MiddleButton == ButtonState.Released) && (MouseState.MiddleButton == ButtonState.Pressed));
                case E_MouseButton.Right: return ((MouseStatePrevious.RightButton == ButtonState.Released) && (MouseState.RightButton == ButtonState.Pressed));
                case E_MouseButton.XButton1: return ((MouseStatePrevious.XButton1 == ButtonState.Released) && (MouseState.XButton1 == ButtonState.Pressed));
                case E_MouseButton.XButton2: return ((MouseStatePrevious.XButton2 == ButtonState.Released) && (MouseState.XButton2 == ButtonState.Pressed));
                default: return false;
            }
        }

        // ButtonJustReleased
        public override bool ButtonJustReleased(int button)
        {
            switch ((E_MouseButton)button)
            {
                case E_MouseButton.Left: return ((MouseStatePrevious.LeftButton == ButtonState.Pressed) && (MouseState.LeftButton == ButtonState.Released));
                case E_MouseButton.Middle: return ((MouseStatePrevious.MiddleButton == ButtonState.Pressed) && (MouseState.MiddleButton == ButtonState.Released));
                case E_MouseButton.Right: return ((MouseStatePrevious.RightButton == ButtonState.Pressed) && (MouseState.RightButton == ButtonState.Released));
                case E_MouseButton.XButton1: return ((MouseStatePrevious.XButton1 == ButtonState.Pressed) && (MouseState.XButton1 == ButtonState.Released));
                case E_MouseButton.XButton2: return ((MouseStatePrevious.XButton2 == ButtonState.Pressed) && (MouseState.XButton2 == ButtonState.Released));
                default: return false;
            }
        }

        // ButtonValue
        public override float ButtonValue(int button)
        {
            return (ButtonDown(button) ? 1.0f : 0.0f);
        }

        // X/Y
        public int X() { return MouseState.X; }
        public int Y() { return MouseState.Y; }
        public Vector2 XY() { return new Vector2(MouseState.X, MouseState.Y); }

        // DX/DY
        public int DX() { return (MouseState.X - MouseStatePrevious.X); }
        public int DY() { return (MouseState.Y - MouseStatePrevious.Y); }
        public Vector2 DXY() { return new Vector2(MouseState.X - MouseStatePrevious.X, MouseState.Y - MouseStatePrevious.Y); }

        // ScrolledUp/Down
        public bool ScrolledUp() { return (MouseState.ScrollWheelValue > MouseStatePrevious.ScrollWheelValue); }
        public bool ScrolledDown() { return (MouseState.ScrollWheelValue < MouseStatePrevious.ScrollWheelValue); }

        //
        public MouseState MouseState { get; private set; }
        public MouseState MouseStatePrevious { get; private set; }
        //
    };
}