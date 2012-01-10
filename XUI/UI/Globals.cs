//-----------------------------------------------
// XUI - Globals.cs
// Copyright (C) Peter Reid. All rights reserved.
//-----------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

// TODO - map other UI buttons to keys on Windows

namespace XUI
{
    // E_UiButton
    public enum E_UiButton
    {
        Quit = 0,

        Up,
        Down,
        Left,
        Right,
        A,
        B,
        X,
        Y,
        Start,
        Back,
        LeftTrigger,
        RightTrigger,
        LeftShoulder,
        RightShoulder,
        LeftStick,
        RightStick,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
#if WINDOWS
        MouseLeft,
        MouseRight,
#endif

        Count,
    };

    // E_UiAxis
    public enum E_UiAxis
    {
        LeftStickX = 0,
        LeftStickY,
        RightStickX,
        RightStickY,

        Count,
    };

    // class _UI
    public static class _UI
    {
        // Startup
        public static void Startup(Game game, GameInput gameInput)
        {
            Startup(game, gameInput, new XUI.UI.Settings()); // default settings
        }

        public static void Startup(Game game, GameInput gameInput, XUI.UI.Settings settings)
        {
            _UI.Settings = settings;

            _UI.Game = game;
            _UI.Content = new ContentManager(game.Services, "Content");

            _UI.PrimaryPad = -1;

            _UI.GameInput = gameInput;
            _UI.Texture = new XUI.UI.TextureManager();
            _UI.Effect = new XUI.UI.EffectManager();
            _UI.Sprite = new XUI.UI.SpriteManager();
            _UI.Font = new XUI.UI.FontManager();
            _UI.Screen = new XUI.UI.ScreenManager();

            _UI.Store_Color = new XUI.UI.Store<XUI.UI.SpriteColors>(Color.Magenta);
            _UI.Store_Timeline = new XUI.UI.Store<XUI.UI.Timeline>();
            _UI.Store_Texture = new XUI.UI.Store<XUI.UI.SpriteTexture>(new XUI.UI.SpriteTexture("null", 0.0f, 0.0f, 1.0f, 1.0f));
            _UI.Store_FontStyle = new XUI.UI.Store<XUI.UI.FontStyle>();
            _UI.Store_FontEffect = new XUI.UI.Store<XUI.UI.FontEffect>();
            _UI.Store_FontIcon = new XUI.UI.Store_FontIcon();
            _UI.Store_Widget = new XUI.UI.Store<XUI.UI.WidgetBase>();
            _UI.Store_RenderState = new XUI.UI.Store<XUI.UI.RenderState>(new XUI.UI.RenderState((int)XUI.UI.E_Effect.MultiTexture1, XUI.UI.E_BlendState.AlphaBlend));
            _UI.Store_BlendState = new XUI.UI.Store<BlendState>();
            _UI.Store_DepthStencilState = new XUI.UI.Store<DepthStencilState>();
            _UI.Store_RasterizerState = new XUI.UI.Store<RasterizerState>();

            _UI.Camera2D = new XUI.UI.CameraSettings2D();
            _UI.Camera3D = new XUI.UI.CameraSettings3D();

            PresentationParameters pp = game.GraphicsDevice.PresentationParameters;

            _UI.SX = pp.BackBufferWidth;
            _UI.SY = pp.BackBufferHeight;

            _UI.YT = 0.0f;
            _UI.YM = _UI.YT + (_UI.SY * 0.5f);
            _UI.YB = _UI.YT + _UI.SY;

            _UI.XL = 0.0f;
            _UI.XM = _UI.XL + (_UI.SX * 0.5f);
            _UI.XR = _UI.XL + _UI.SX;

            float safeArea = settings.Screen_SafeAreaSize;
            float offsetHalf = (1.0f - safeArea) * 0.5f;

            _UI.SSY = _UI.SY * safeArea;
            _UI.SSX = _UI.SX * safeArea;

            _UI.SYT = _UI.SY * offsetHalf;
            _UI.SYM = _UI.SYT + (_UI.SSY * 0.5f);
            _UI.SYB = _UI.SYT + _UI.SSY;

            _UI.SXL = _UI.SX * offsetHalf;
            _UI.SXM = _UI.SXL + (_UI.SSX * 0.5f);
            _UI.SXR = _UI.SXL + _UI.SSX;

            _UI.AutoRepeatDelay = 0.5f;
            _UI.AutoRepeatRepeat = 0.25f;
        }

        // Shudown
        public static void Shutdown()
        {
            _UI.Texture.DestroyBundle(-1);
            Content.Unload();
        }

        // SetupControls
        public static void SetupControls(GameInput gameInput)
        {
            for (int i = 0; i < GameInput.NumPads; ++i)
                SetupControls(gameInput.GetInput(i));
        }

        public static void SetupControls(Input input)
        {
#if WINDOWS
            input.AddButtonMapping((int)E_UiButton.Quit, E_Device.Keyboard, (int)Keys.Escape);
            input.AddButtonMapping((int)E_UiButton.Up, E_Device.Keyboard, (int)Keys.Up);
            input.AddButtonMapping((int)E_UiButton.Down, E_Device.Keyboard, (int)Keys.Down);
            input.AddButtonMapping((int)E_UiButton.Left, E_Device.Keyboard, (int)Keys.Left);
            input.AddButtonMapping((int)E_UiButton.Right, E_Device.Keyboard, (int)Keys.Right);
            input.AddButtonMapping((int)E_UiButton.A, E_Device.Keyboard, (int)Keys.Enter);
            input.AddButtonMapping((int)E_UiButton.B, E_Device.Keyboard, (int)Keys.Back);

            input.AddButtonMapping((int)E_UiButton.MouseLeft, E_Device.Mouse, (int)E_MouseButton.Left);
            input.AddButtonMapping((int)E_UiButton.MouseRight, E_Device.Mouse, (int)E_MouseButton.Right);
#endif

#if !RELEASE
        input.AddButtonMapping( (int)E_UiButton.Quit, E_Device.Controller, (int)E_Button.RightStick );
#endif

            input.AddButtonMapping((int)E_UiButton.Up, E_Device.Controller, (int)E_Button.LeftStickUp);
            input.AddButtonMapping((int)E_UiButton.Up, E_Device.Controller, (int)E_Button.DPadUp);
            input.AddButtonMapping((int)E_UiButton.Down, E_Device.Controller, (int)E_Button.LeftStickDown);
            input.AddButtonMapping((int)E_UiButton.Down, E_Device.Controller, (int)E_Button.DPadDown);
            input.AddButtonMapping((int)E_UiButton.Left, E_Device.Controller, (int)E_Button.LeftStickLeft);
            input.AddButtonMapping((int)E_UiButton.Left, E_Device.Controller, (int)E_Button.DPadLeft);
            input.AddButtonMapping((int)E_UiButton.Right, E_Device.Controller, (int)E_Button.LeftStickRight);
            input.AddButtonMapping((int)E_UiButton.Right, E_Device.Controller, (int)E_Button.DPadRight);
            input.AddButtonMapping((int)E_UiButton.A, E_Device.Controller, (int)E_Button.A);
            input.AddButtonMapping((int)E_UiButton.B, E_Device.Controller, (int)E_Button.B);
            input.AddButtonMapping((int)E_UiButton.X, E_Device.Controller, (int)E_Button.X);
            input.AddButtonMapping((int)E_UiButton.Y, E_Device.Controller, (int)E_Button.Y);
            input.AddButtonMapping((int)E_UiButton.Start, E_Device.Controller, (int)E_Button.Start);
            input.AddButtonMapping((int)E_UiButton.Back, E_Device.Controller, (int)E_Button.Back);
            input.AddButtonMapping((int)E_UiButton.LeftTrigger, E_Device.Controller, (int)E_Button.LeftTrigger);
            input.AddButtonMapping((int)E_UiButton.RightTrigger, E_Device.Controller, (int)E_Button.RightTrigger);
            input.AddButtonMapping((int)E_UiButton.LeftShoulder, E_Device.Controller, (int)E_Button.LeftShoulder);
            input.AddButtonMapping((int)E_UiButton.RightShoulder, E_Device.Controller, (int)E_Button.RightShoulder);
            input.AddButtonMapping((int)E_UiButton.LeftStick, E_Device.Controller, (int)E_Button.LeftStick);
            input.AddButtonMapping((int)E_UiButton.RightStick, E_Device.Controller, (int)E_Button.RightStick);
            input.AddButtonMapping((int)E_UiButton.DPadUp, E_Device.Controller, (int)E_Button.DPadUp);
            input.AddButtonMapping((int)E_UiButton.DPadDown, E_Device.Controller, (int)E_Button.DPadDown);
            input.AddButtonMapping((int)E_UiButton.DPadLeft, E_Device.Controller, (int)E_Button.DPadLeft);
            input.AddButtonMapping((int)E_UiButton.DPadRight, E_Device.Controller, (int)E_Button.DPadRight);

            input.AddAxisMapping((int)E_UiAxis.LeftStickX, E_Device.Controller, (int)E_Button.LeftStickLeft, (int)E_Button.LeftStickRight);
            input.AddAxisMapping((int)E_UiAxis.LeftStickY, E_Device.Controller, (int)E_Button.LeftStickDown, (int)E_Button.LeftStickUp);
            input.AddAxisMapping((int)E_UiAxis.RightStickX, E_Device.Controller, (int)E_Button.RightStickLeft, (int)E_Button.RightStickRight);
            input.AddAxisMapping((int)E_UiAxis.RightStickY, E_Device.Controller, (int)E_Button.RightStickDown, (int)E_Button.RightStickUp);
        }

        // SetupDebugMenu
        public static void SetupDebugMenu(SpriteBatch spriteBatch)
        {
            if (spriteBatch == null)
                spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            Texture2D texture = Content.Load<Texture2D>("Textures\\Debug_Menu");
            SpriteFont font = Content.Load<SpriteFont>("Textures\\Debug_Font");

            DebugMenu = new DebugMenu(spriteBatch, texture, font);
        }

        //
        public static XUI.UI.Settings Settings;

        public static Game Game;
        public static ContentManager Content;

        public static DebugMenu DebugMenu;
        public static bool DebugMenuActive;

        public static int PrimaryPad;

        public static GameInput GameInput;
        public static XUI.UI.TextureManager Texture;
        public static XUI.UI.EffectManager Effect;
        public static XUI.UI.SpriteManager Sprite;
        public static XUI.UI.FontManager Font;
        public static XUI.UI.ScreenManager Screen;

        public static XUI.UI.Store<XUI.UI.SpriteColors> Store_Color;
        public static XUI.UI.Store<XUI.UI.Timeline> Store_Timeline;
        public static XUI.UI.Store<XUI.UI.SpriteTexture> Store_Texture;
        public static XUI.UI.Store<XUI.UI.FontStyle> Store_FontStyle;
        public static XUI.UI.Store<XUI.UI.FontEffect> Store_FontEffect;
        public static XUI.UI.Store_FontIcon Store_FontIcon;
        public static XUI.UI.Store<XUI.UI.WidgetBase> Store_Widget;
        public static XUI.UI.Store<XUI.UI.RenderState> Store_RenderState;
        public static XUI.UI.Store<BlendState> Store_BlendState;
        public static XUI.UI.Store<DepthStencilState> Store_DepthStencilState;
        public static XUI.UI.Store<RasterizerState> Store_RasterizerState;

        public static XUI.UI.CameraSettings2D Camera2D;
        public static XUI.UI.CameraSettings3D Camera3D;

        public static float SX;
        public static float SY;
        public static float XL;
        public static float XM;
        public static float XR;
        public static float YT;
        public static float YB;
        public static float YM;

        public static float SSX;
        public static float SSY;
        public static float SXL;
        public static float SXM;
        public static float SXR;
        public static float SYT;
        public static float SYM;
        public static float SYB;

        public static float AutoRepeatDelay;
        public static float AutoRepeatRepeat;
        //
    };
}