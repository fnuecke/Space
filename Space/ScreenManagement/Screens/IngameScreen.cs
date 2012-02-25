using System;
using System.Collections.Generic;
using System.Threading;
using Awesomium.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.Control;
using Space.ScreenManagement.Screens.Gameplay;
using Space.ScreenManagement.Screens.Helper;
using Space.ScreenManagement.Screens.Ingame;
using Space.ScreenManagement.Screens.Ingame.GuiElementManager;
using Space.ScreenManagement.Screens.Ingame.Hud;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using Space.Util;
using System.Drawing.Imaging;

namespace Space.ScreenManagement.Screens
{
    /// <summary>
    /// This screen implements all the ingame elements of the GUI.
    /// </summary>
    public sealed class IngameScreen : GameScreen
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        #region Fields

        private float _pauseAlpha;

        /// <summary>
        /// The game client that's active for this game.
        /// </summary>
        private GameClient _client;

        /// <summary>
        /// Grab player input when this screen is active.
        /// </summary>
        private InputHandler _input;

        /// <summary>
        /// The item manager for ingame items.
        /// </summary>
        private ItemSelectionManager _itemManager;

        private TextureManager _textureManager;

        /// <summary>
        /// The component responsible for post-processing effects.
        /// </summary>
        Postprocessing _postprocessing;

        /// <summary>
        /// Holds all GUI elements that can be displayed. Remember this list
        /// also holds the GUI elements that are invisible.
        /// </summary>
        List<AbstractGuiElement> _elements;

        /// <summary>
        /// Holds the background.
        /// </summary>
        Background _background;

        Inventory _inventory;

        private WebView _webView;
        private Texture2D _webTexture;
        private System.Drawing.Bitmap _frameBuffer;

        #endregion

        #region Properties

        /// <summary>
        /// The basic object for dynamic scaling of graphic elements.
        /// </summary>
        public Scale Scale { get; private set; }

        /// <summary>
        /// The basic object for dynamic scaling of graphic elements.
        /// </summary>
        public Fonts Fonts { get; private set; }

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        public SpriteBatch SpriteBatch { get; private set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public IngameScreen(GameClient client)
        {
            _client = client;
            _input = new InputHandler(client, this);
            _itemManager = new ItemSelectionManager();
            _textureManager = new TextureManager(client);
            Fonts = new Fonts();

            _elements = new List<AbstractGuiElement>();
            _elements.Add(new Orbits(client));
            _elements.Add(new Radar(client));

            _inventory = new Inventory(client, _itemManager, _textureManager);
            _elements.Add(_inventory);

            _elements.Add(new MouseLayer(client, _itemManager, _textureManager));

            _background = new Background(client);

            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            var game = ScreenManager.Game;
            SpriteBatch = ScreenManager.SpriteBatch;
            Scale = new Scale(SpriteBatch);
            Fonts.LoadContent(this, game.Content);

            _background.LoadContent(SpriteBatch, game.Content);

            // loop the list of elements and load all of them
            foreach (AbstractGuiElement e in _elements)
            {
                e.LoadContent(this, game.Content);
            }

            _postprocessing = new Postprocessing(game);
            ScreenManager.Game.Components.Add(_postprocessing);

            var viewport = SpriteBatch.GraphicsDevice.Viewport;

            // do individual settings for the GUI objects here
            _inventory.SetPosition(690, 50);
            _inventory.SetWidth(540);
            _inventory.SetHeight(700);

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();

            // initialize the Web View
            _webView = WebCore.CreateWebView(800, 600);
            _webView.FlushAlpha = false;
            _webView.IsTransparent = true;

            // initialize the functionality for mouse and keyboard input
            var keyboard = ((IKeyboard)ScreenManager.Game.Services.GetService(typeof(IKeyboard)));
            keyboard.CharacterEntered +=
                delegate(char ch)
                {
                    var evt = new WebKeyboardEvent();
                    evt.IsSystemKey = false;
                    if (keyboard.GetState().IsKeyDown(Keys.LeftControl) ||
                        keyboard.GetState().IsKeyDown(Keys.RightControl))
                    {
                        evt.Modifiers |= WebKeyModifiers.ControlKey;
                    }
                    if (keyboard.GetState().IsKeyDown(Keys.LeftShift) ||
                        keyboard.GetState().IsKeyDown(Keys.RightShift))
                    {
                        evt.Modifiers |= WebKeyModifiers.ShiftKey;
                    }
                    if (keyboard.GetState().IsKeyDown(Keys.LeftAlt) ||
                        keyboard.GetState().IsKeyDown(Keys.RightAlt))
                    {
                        evt.Modifiers |= WebKeyModifiers.ShiftKey;
                    }
                    evt.Text = new ushort[] { ch, 0, 0, 0 };
                    evt.UnmodifiedText = new ushort[] { ch, 0, 0, 0 };
                    evt.VirtualKeyCode = (VirtualKey)VkKeyScan(ch);
                    evt.NativeKeyCode = ch;

                    evt.Type = WebKeyType.KeyDown;
                    _webView.InjectKeyboardEvent(evt);
                    WebCore.Update();
                    Thread.Yield();
                    evt.Type = WebKeyType.Char;
                    _webView.InjectKeyboardEvent(evt);
                    WebCore.Update();
                    Thread.Yield();
                    evt.Type = WebKeyType.KeyUp;
                    _webView.InjectKeyboardEvent(evt);
                    WebCore.Update();
                    Thread.Yield();
                };
            var mouse = ((IMouse)ScreenManager.Game.Services.GetService(typeof(IMouse)));
            mouse.MouseMoved +=
                delegate(float x, float y)
                {
                    _webView.InjectMouseMove((int)x, (int)y);
                };
            mouse.MouseButtonPressed +=
                delegate(MouseButtons buttons)
                {
                    switch (buttons)
                    {
                        case MouseButtons.Left:
                            _webView.InjectMouseDown(MouseButton.Left);
                            break;
                        case MouseButtons.Right:
                            _webView.InjectMouseDown(MouseButton.Right);
                            break;
                        case MouseButtons.Middle:
                            _webView.InjectMouseDown(MouseButton.Middle);
                            break;
                    }
                };
            mouse.MouseButtonReleased +=
                delegate(MouseButtons buttons)
                {
                    switch (buttons)
                    {
                        case MouseButtons.Left:
                            _webView.InjectMouseUp(MouseButton.Left);
                            break;
                        case MouseButtons.Right:
                            _webView.InjectMouseUp(MouseButton.Right);
                            break;
                        case MouseButtons.Middle:
                            _webView.InjectMouseUp(MouseButton.Middle);
                            break;
                    }
                };
            mouse.MouseWheelRotated +=
                delegate(float ticks)
                {
                    _webView.InjectMouseWheel((int)ticks);
                };

            // load FIFA 4 Fans as sample homepage for the web texture
            _webView.LoadURL("http://www.fifa4fans.de/");
        }

        public override void UnloadContent()
        {
            ScreenManager.Game.Components.Remove(_postprocessing);

            if (_postprocessing != null)
            {
                _postprocessing.Dispose();
            }

            _webView.Close();
            if (_webTexture != null)
            {
                _webTexture.Dispose();
                _webTexture = null;
            }
        }

        #endregion

        #region Getter

        /// <summary>
        /// Returns the list holding all GUI elements.
        /// </summary>
        /// <returns>The list holding all GUI elements.</returns>
        public List<AbstractGuiElement> GetGuiElements()
        {
            return _elements;
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            // Enable or disable our game input depending on whether we're the
            // active screen or not, and whether the game is running or not.
            if (_client.IsRunning())
            {
                _input.SetEnabled(!otherScreenHasFocus);

                // Only update our input if we have focus.
                if (!otherScreenHasFocus)
                {
                    _input.Update();
                }

            }
            else
            {
                _input.SetEnabled(false);
            }

            // Gradually fade in or out depending on whether we are covered by the pause screen.
            if (coveredByOtherScreen)
            {
                _pauseAlpha = Math.Min(_pauseAlpha + 1f / 32, 1);
            }
            else
            {
                _pauseAlpha = Math.Max(_pauseAlpha - 1f / 32, 0);
            }

            WebCore.Update();
        }

        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input.KeyPause)
            {
                ScreenManager.AddScreen(new PauseMenuScreen());
            }
        }

        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // draw the background first
            _background.Draw();

            // Draw world elements.
            _client.Controller.Draw(gameTime);

            // Finish up post processing, GUI should not be affected by it.
            if (Settings.Instance.PostProcessing)
            {
                _postprocessing.Draw();
            }

            // loop the list of elements and load all of them
            foreach (AbstractGuiElement e in _elements)
            {
                e.Draw();
            }

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || _pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, _pauseAlpha / 2);
                ScreenManager.FadeBackBufferToBlack(alpha);
            }

            var webBuffer = _webView.Render();
            if (_webTexture != null && (_webTexture.Width != webBuffer.Width || _webTexture.Height != webBuffer.Height))
            {
                _webTexture.Dispose();
                _webTexture = null;
            }

            if (_webTexture == null)
            {
                _webTexture = new Texture2D(SpriteBatch.GraphicsDevice, webBuffer.Width, webBuffer.Height);
            }


            // render and draw the web view
            Render();
            _webView.Focus();
            SpriteBatch.Begin();
            SpriteBatch.Draw(_webTexture, new Rectangle(0, 0, 800, 600), Color.White);
            SpriteBatch.End();
        }

        #endregion

        #region Visibility Getter / Setter

        /// <summary>
        /// Opens the inventory.
        /// </summary>
        public void OpenInventory()
        {
            _inventory.Visible = true;
        }

        /// <summary>
        /// Closes the inventory.
        /// </summary>
        public void CloseInventory()
        {
            _inventory.Visible = false;
        }

        /// <summary>
        /// Returns if the inventory is visible or not.
        /// </summary>
        /// <returns><code>True</code> if the inventory is visible, <code>false</code> else.</returns>
        public bool IsInventoryVisible()
        {
            return _inventory.Visible;
        }

        #endregion

        /// <summary>
        /// Method that renders the web view texture.
        /// </summary>
        void Render()
        {
            if (!_webView.IsEnabled)
                return;

            try
            {
                RenderBuffer rBuffer = _webView.Render();

                // Only recreate the bitmap if the size of the buffer has changed.
                if (_frameBuffer == null || _frameBuffer.Width != rBuffer.Width || _frameBuffer.Height != rBuffer.Height)
                {
                    if (_frameBuffer != null)
                        _frameBuffer.Dispose();

                    _frameBuffer = new System.Drawing.Bitmap(rBuffer.Width, rBuffer.Height, PixelFormat.Format32bppPArgb);
                }

                BitmapData bits = _frameBuffer.LockBits(
                    new System.Drawing.Rectangle(0, 0, rBuffer.Width, rBuffer.Height),
                    ImageLockMode.ReadWrite, _frameBuffer.PixelFormat);

                // Copy to Bitmap specifying ConvertToRGBA = true.
                rBuffer.CopyTo(bits.Scan0, bits.Stride, 4, true);

                // Get the size of the buffer.
                int bufferSize = bits.Height * bits.Stride;

                // Create a data buffer.
                byte[] bytes = new byte[bufferSize];

                // Copy bitmap data into buffer.
                System.Runtime.InteropServices.Marshal.Copy(bits.Scan0, bytes, 0, bytes.Length);

                _frameBuffer.UnlockBits(bits);

                if (_webTexture != null)
                    _webTexture.Dispose();

                // Copy our buffer to the texture.
                _webTexture = new Texture2D(SpriteBatch.GraphicsDevice, rBuffer.Width, rBuffer.Height);
                _webTexture.SetData(bytes);
            }
            catch { /* */ }
            finally
            {
                GC.Collect();
            }
        }
    }
}
