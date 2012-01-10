//-----------------------------------------------
// XUI - Device.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System;

namespace XUI
{
    // E_Device
    public enum E_Device
    {
        Keyboard = 0,
        Mouse,
        Controller,

        Count,
    };

    // class Device
    public abstract class Device
    {
        // Update
        public abstract void Update(float frameTime);

        // ButtonDown
        public abstract bool ButtonDown(int button);

        // ButtonJustPressed
        public abstract bool ButtonJustPressed(int button);

        // ButtonJustReleased
        public abstract bool ButtonJustReleased(int button);

        // ButtonValue
        public abstract float ButtonValue(int button);

        // ButtonHeldTime
        public float ButtonHeldTime(int button)
        {
            return (ButtonHeldTimes[button]);
        }

        // ButtonHeldTimePrev
        public float ButtonHeldTimePrev(int button)
        {
            return (ButtonHeldTimesPrev[button]);
        }

        // ButtonAutoRepeat
        public bool ButtonAutoRepeat(int button, float initialDelay, float repeatTime)
        {
            float timeHeld = ButtonHeldTimes[button];

            if (timeHeld == 0.0f)
                return false;

            if (timeHeld <= FrameTime)
                return true;

            int count = Math.Max((int)Math.Floor((timeHeld - initialDelay) / repeatTime), -1);
            int countPrev = Math.Max((int)Math.Floor((timeHeld - FrameTime - initialDelay) / repeatTime), -1);

            return (count != countPrev);
        }

        //
        protected float FrameTime;
        protected float[] ButtonHeldTimes;
        protected float[] ButtonHeldTimesPrev;
        //
    };
}