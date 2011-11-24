using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{

    public delegate void KeyPressedEventHandler(Keys key, KeyModifier modifier);
    public delegate void KeyReleasedEventHandler(Keys key);

    /// <summary>
    /// This class may be used to get an event driven access to user key presses.
    /// 
    /// Upon creation, this class registers itself as a service with the game it
    /// is created for, so it can be accessed by any other component.
    /// </summary>
    public sealed class KeyboardInputManager : GameComponent
    {

        #region Events

        /// <summary>
        /// Fired when a key is newly pressed, or is repeated as set via RepeatDelay/RepeatRate.
        /// </summary>
        public event KeyPressedEventHandler Pressed;

        /// <summary>
        /// Fired when a key is released.
        /// </summary>
        public event KeyReleasedEventHandler Released;

        #endregion

        #region Properties

        /// <summary>
        /// The delay before a key start to trigger repeatedly.
        /// </summary>
        public int RepeatDelay { get; set; }

        /// <summary>
        /// The delay between triggering when triggering repeatedly (key held down).
        /// </summary>
        public int RepeatRate { get; set; }

        #endregion

        #region Fields

        private Keys lastPressedKey;
        private DateTime lastRepeat;
        private KeyboardState previousState;

        #endregion

        /// <summary>
        /// Creates a new keyboard manager for the given game.
        /// </summary>
        /// <param name="game">the game to create the manager for.</param>
        public KeyboardInputManager(Game game)
            : base(game)
        {
            RepeatDelay = 300;
            RepeatRate = 40;

            game.Components.Add(this);
            game.Services.AddService(typeof(KeyboardInputManager), this);
        }

        #region Logic

        /// <summary>
        /// Implements key press / repeat / release logic.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            var currentState = Keyboard.GetState();

            if (previousState != null)
            {
                var currentTime = DateTime.Now;
                foreach (var key in currentState.GetPressedKeys())
                {
                    KeyModifier modifier = KeyModifier.None;
                    if (currentState.GetPressedKeys().Contains(Keys.LeftAlt) ||
                        currentState.GetPressedKeys().Contains(Keys.RightAlt))
                    {
                        modifier |= KeyModifier.Alt;
                    }
                    if (currentState.GetPressedKeys().Contains(Keys.LeftControl) ||
                        currentState.GetPressedKeys().Contains(Keys.RightControl))
                    {
                        modifier |= KeyModifier.Control;
                    }
                    if (currentState.GetPressedKeys().Contains(Keys.LeftShift) ||
                        currentState.GetPressedKeys().Contains(Keys.RightShift))
                    {
                        modifier |= KeyModifier.Shift;
                    }

                    if (previousState.GetPressedKeys().Contains(key))
                    {
                        // Key was held.
                        if (RepeatDelay > 0 && RepeatRate > 0 && lastRepeat != null)
                        {
                            if (key == lastPressedKey && new TimeSpan(currentTime.Ticks - lastRepeat.Ticks).TotalMilliseconds > RepeatRate)
                            {
                                lastRepeat = currentTime;
                                OnPressed(key, modifier);
                            }
                        }
                    }
                    else
                    {
                        // Key was pressed.
                        if (RepeatDelay > 0 && RepeatRate > 0)
                        {
                            lastPressedKey = key;
                            lastRepeat = currentTime.AddMilliseconds(RepeatDelay);
                        }
                        OnPressed(key, modifier);
                    }
                }
                foreach (var key in previousState.GetPressedKeys().Except(currentState.GetPressedKeys()))
                {
                    // Key was released.
                    if (key == lastPressedKey)
                    {
                        lastPressedKey = Keys.None;
                    }
                    OnReleased(key);
                }
            }

            previousState = currentState;

            base.Update(gameTime);
        }

        private void OnPressed(Keys key, KeyModifier modifier)
        {
            if (Pressed != null)
            {
                Pressed(key, modifier);
            }
        }

        private void OnReleased(Keys key)
        {
            if (Released != null)
            {
                Released(key);
            }
        }

        #endregion

    }
}
