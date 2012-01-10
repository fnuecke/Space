//-----------------------------------------------
// XUI - GameInput.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

namespace XUI
{
    // class GameInput
    public class GameInput
    {
        // GameInput
        public GameInput(int numButtonMappings, int numAxisMappings)
        {
#if WINDOWS
            Keyboard = new KeyboardD();
            Mouse = new MouseD();
#endif

            Input = new Input[NumPads];

            for (int i = 0; i < Input.Length; ++i)
                Input[i] = new Input(numButtonMappings, numAxisMappings, i
#if WINDOWS
, Keyboard, Mouse
#endif
);
        }

        // Update
        public void Update(float frameTime)
        {
#if WINDOWS
            Keyboard.Update(frameTime);
            Mouse.Update(frameTime);
#endif

            for (int i = 0; i < Input.Length; ++i)
                Input[i].Update(frameTime);
        }

        // GetController
        public ControllerD GetController(int padIndex) { return Input[padIndex].Controller; }

        // GetInput
        public Input GetInput(int padIndex) { return Input[padIndex]; }
        //

        //
#if XBOX
	public static int		NumPads = 4;
#else
        public static int NumPads = 1;
#endif

#if WINDOWS
        public KeyboardD Keyboard { get; private set; }
        public MouseD Mouse { get; private set; }
#endif

        private Input[] Input;
        //
    };
}