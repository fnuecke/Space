using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.Util;

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
            var keyboard = (IKeyboard)game.Services.GetService(typeof(IKeyboard));
            if (keyboard != null)
            {
                keyboard.KeyPressed += HandleKeyPressed;
            }

            var mouse = (IMouse)game.Services.GetService(typeof(IMouse));
            if (mouse != null)
            {
                mouse.MouseButtonPressed += HandleMousePressed;
                mouse.MouseWheelRotated += HandleMouseScrolled;
                mouse.MouseMoved += HandleMouseMoved;
            }

            var gamepad = (IGamePad)game.Services.GetService(typeof(IGamePad));
            if (gamepad != null)
            {
                
            }
        }

        void HandleKeyPressed(Keys key)
        {
            if (Settings.Instance.MenuBindings.ContainsKey(key))
            {
                switch (Settings.Instance.MenuBindings[key])
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

        void HandleMousePressed(MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                MouseSelect = true;
            }
            else if (buttons == MouseButtons.Right)
            {
                KeyCancel = true;
            }
        }

        void HandleMouseScrolled(float ticks)
        {
            if (ticks < 0)
            {
                KeyNext = true;
            }
            else
            {
                KeyPrevious = true;
            }
        }

        void HandleMouseMoved(float x, float y)
        {
            MousePosition = new Vector2(x, y);
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
