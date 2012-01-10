//-----------------------------------------------
// XUI - Input.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using System.Collections.Generic;

namespace XUI
{
    // class ButtonMapping
    public class ButtonMapping
    {
        // ButtonMapping
        public ButtonMapping(Device device, int button)
        {
            Device = device;
            Button = button;
        }

        //
        public Device Device;
        public int Button;
        //
    };

    // class AxisMapping
    public class AxisMapping
    {
        // AxisMapping
        public AxisMapping(Device device, int buttonNegative, int buttonPositive)
        {
            Device = device;
            ButtonNegative = buttonNegative;
            ButtonPositive = buttonPositive;
        }

        //
        public Device Device;
        public int ButtonNegative;
        public int ButtonPositive;
        //
    };

    // class Input
    public class Input
    {
        // Input
        public Input(int numButtonMappings, int numAxisMappings, int padIndex
#if WINDOWS
, KeyboardD keyboard, MouseD mouse
#endif
)
        {
            ButtonMappings = new List<List<ButtonMapping>>(numButtonMappings);

            for (int i = 0; i < numButtonMappings; ++i)
                ButtonMappings.Add(new List<ButtonMapping>());

            AxisMappings = new List<List<AxisMapping>>(numAxisMappings);

            for (int i = 0; i < numAxisMappings; ++i)
                AxisMappings.Add(new List<AxisMapping>());

#if WINDOWS
            Keyboard = keyboard;
            Mouse = mouse;
#endif

            Controller = new ControllerD(padIndex);
        }

        // Update
        public void Update(float frameTime)
        {
            Controller.Update(frameTime);
        }

        // Clear
        public void Clear()
        {
            for (int i = 0; i < ButtonMappings.Count; ++i)
                ButtonMappings[i].Clear();

            for (int i = 0; i < AxisMappings.Count; ++i)
                AxisMappings[i].Clear();
        }

        // AddButtonMapping
        public void AddButtonMapping(int gameButton, E_Device device, int deviceButton)
        {
            ButtonMappings[gameButton].Add(new ButtonMapping(GetDevice(device), deviceButton));
        }

        // AddAxisMapping
        public void AddAxisMapping(int gameAxis, E_Device device, int deviceButtonNegative, int deviceButtonPositive)
        {
            AxisMappings[gameAxis].Add(new AxisMapping(GetDevice(device), deviceButtonNegative, deviceButtonPositive));
        }

        // GetDevice
        public Device GetDevice(E_Device device)
        {
            switch (device)
            {
#if WINDOWS
                case E_Device.Keyboard: return Keyboard;
                case E_Device.Mouse: return Mouse;
#endif
                case E_Device.Controller: return Controller;
                default: return null;
            }
        }

        // - the following mirror those in Device but return based on the set mappings

        // ButtonDown
        public bool ButtonDown(int gameButton)
        {
            for (int i = 0; i < ButtonMappings[gameButton].Count; ++i)
            {
                ButtonMapping inputMapping = ButtonMappings[gameButton][i];

                if (inputMapping.Device.ButtonDown(inputMapping.Button))
                    return true;
            }

            return false;
        }

        // ButtonJustPressed
        public bool ButtonJustPressed(int gameButton)
        {
            for (int i = 0; i < ButtonMappings[gameButton].Count; ++i)
            {
                ButtonMapping inputMapping = ButtonMappings[gameButton][i];

                if (inputMapping.Device.ButtonJustPressed(inputMapping.Button))
                    return true;
            }

            return false;
        }

        // ButtonJustReleased
        public bool ButtonJustReleased(int gameButton)
        {
            for (int i = 0; i < ButtonMappings[gameButton].Count; ++i)
            {
                ButtonMapping inputMapping = ButtonMappings[gameButton][i];

                if (inputMapping.Device.ButtonJustReleased(inputMapping.Button))
                    return true;
            }

            return false;
        }

        // ButtonValue
        public float ButtonValue(int gameButton)
        {
            float value = 0.0f;

            for (int i = 0; i < ButtonMappings[gameButton].Count; ++i)
            {
                ButtonMapping inputMapping = ButtonMappings[gameButton][i];

                float buttonValue = inputMapping.Device.ButtonValue(inputMapping.Button);

                if (buttonValue > value)
                    value = buttonValue;
            }

            return value;
        }

        // ButtonHeldTime
        public float ButtonHeldTime(int gameButton)
        {
            float heldTime = 0.0f;

            for (int i = 0; i < ButtonMappings[gameButton].Count; ++i)
            {
                ButtonMapping inputMapping = ButtonMappings[gameButton][i];

                float buttonHeldTime = inputMapping.Device.ButtonHeldTime(inputMapping.Button);

                if (buttonHeldTime > heldTime)
                    heldTime = buttonHeldTime;
            }

            return heldTime;
        }

        // ButtonHeldTimePrev
        public float ButtonHeldTimePrev(int gameButton)
        {
            float heldTimePrev = 0.0f;

            for (int i = 0; i < ButtonMappings[gameButton].Count; ++i)
            {
                ButtonMapping inputMapping = ButtonMappings[gameButton][i];

                float buttonHeldTimePrev = inputMapping.Device.ButtonHeldTimePrev(inputMapping.Button);

                if (buttonHeldTimePrev > heldTimePrev)
                    heldTimePrev = buttonHeldTimePrev;
            }

            return heldTimePrev;
        }

        // ButtonAutoRepeat
        public bool ButtonAutoRepeat(int gameButton, float initialDelay, float repeatTime)
        {
            for (int i = 0; i < ButtonMappings[gameButton].Count; ++i)
            {
                ButtonMapping inputMapping = ButtonMappings[gameButton][i];

                if (inputMapping.Device.ButtonAutoRepeat(inputMapping.Button, initialDelay, repeatTime))
                    return true;
            }

            return false;
        }

        // AxisValue
        public float AxisValue(int gameAxis)
        {
            float value = 0.0f;
            bool setFirst = false;

            for (int i = 0; i < AxisMappings[gameAxis].Count; ++i)
            {
                AxisMapping inputMapping = AxisMappings[gameAxis][i];

                float buttonValueNegative = inputMapping.Device.ButtonValue(inputMapping.ButtonNegative);
                float buttonValuePositive = inputMapping.Device.ButtonValue(inputMapping.ButtonPositive);

                if ((buttonValueNegative == 0.0f) && (buttonValuePositive == 0.0f))
                    continue;

                float axisValue = -buttonValueNegative + buttonValuePositive;

                if (!setFirst)
                {
                    value = axisValue;
                    setFirst = true;

                    continue;
                }

                if (axisValue > value)
                    value = axisValue;
            }

            return value;
        }

        //
#if WINDOWS
        private KeyboardD Keyboard;
        private MouseD Mouse;
#endif

        public ControllerD Controller { get; private set; }

        private List<List<ButtonMapping>> ButtonMappings;
        private List<List<AxisMapping>> AxisMappings;
        //
    };
}