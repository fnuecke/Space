using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Awesomium.Core;
using Awesomium.Xna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;

namespace Awesomium.ScreenManagement
{
    /// <summary>
    /// Screen manager for keeping track of multiple web views.
    /// </summary>
    public sealed class ScreenManager : DrawableGameComponent
    {
        #region Properties

        /// <summary>
        /// How many pixels to scroll per tick.
        /// </summary>
        public int ScrollAmount { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The sprite batch to render into.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The input manager used for updating.
        /// </summary>
        private readonly InputManager _inputManager;

        /// <summary>
        /// Buffered result we keep until the current view is flagged dirty
        /// again.
        /// </summary>
        private RenderBuffer _renderBuffer;

        /// <summary>
        /// Used to render screens into before painting them to the screen.
        /// </summary>
        private readonly Texture2D _backBuffer;

        /// <summary>
        /// List of callbacks to register to allow interaction.
        /// </summary>
        private readonly Dictionary<string, JSCallback> _callbacks = new Dictionary<string, JSCallback>();

        /// <summary>
        /// List of values to expose to our screens.
        /// </summary>
        private readonly Dictionary<string, JSValue> _values = new Dictionary<string, JSValue>();

        /// <summary>
        /// All currently tracked screens.
        /// </summary>
        private readonly Stack<WebView> _screens = new Stack<WebView>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenManager"/> class.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="spriteBatch">The sprite batch to use for rendering.</param>
        /// <param name="manager">The input manager to use.</param>
        public ScreenManager(Game game, SpriteBatch spriteBatch, InputManager manager)
            : base(game)
        {
            _spriteBatch = spriteBatch;
            _inputManager = manager;
            ScrollAmount = 100;

            _backBuffer = new Texture2D(Game.GraphicsDevice,
                                        Game.GraphicsDevice.Viewport.Width,
                                        Game.GraphicsDevice.Viewport.Height);

            foreach (var keyboard in _inputManager.Keyboards)
            {
                keyboard.KeyPressed += HandleKeyPressed;
                keyboard.CharacterEntered += HandleCharacterEntered;
                keyboard.KeyReleased += HandleKeyReleased;
            }

            foreach (var mouse in _inputManager.Mice)
            {
                mouse.MouseButtonPressed += HandleMouseButtonPressed;
                mouse.MouseButtonReleased += HandleMouseButtonReleased;
                mouse.MouseMoved += HandleMouseMoved;
                mouse.MouseWheelRotated += HandleMouseWheelRotated;
            }

            if (!WebCore.IsRunning)
            {
                var config = new WebCoreConfig
                             {
                                 ForceSingleProcess = true,
                                 ProxyServer = "none"
                             };
                WebCore.Initialize(config);
            }

            AddCallback("Screens", "push", JSPushScreen);
            AddCallback("Screens", "pop", JSPopScreen);
        }

        /// <summary>
        /// Disposes all screens and removes event listeners for input events.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var screen in _screens)
                {
                    screen.Close();
                }

                foreach (var keyboard in _inputManager.Keyboards)
                {
                    keyboard.KeyPressed -= HandleKeyPressed;
                    keyboard.CharacterEntered -= HandleCharacterEntered;
                    keyboard.KeyReleased -= HandleKeyReleased;
                }

                foreach (var mouse in _inputManager.Mice)
                {
                    mouse.MouseButtonPressed -= HandleMouseButtonPressed;
                    mouse.MouseButtonReleased -= HandleMouseButtonReleased;
                    mouse.MouseMoved -= HandleMouseMoved;
                    mouse.MouseWheelRotated -= HandleMouseWheelRotated;
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            WebCore.Update();
        }

        /// <summary>
        /// Draws all active screens on top of each other.
        /// </summary>
        /// <param name="gameTime">Time that passed since the last call to draw.</param>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (_screens.Count < 1)
            {
                return;
            }

            var screen = _screens.Peek();
            if (_renderBuffer == null || screen.IsDirty)
            {
                _renderBuffer = screen.Render();
            }

            _renderBuffer.RenderTexture2D(_backBuffer);
            _spriteBatch.Begin();
            _spriteBatch.Draw(_backBuffer, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Pushes the screen with the specified name to the top.
        /// </summary>
        /// <param name="screenName">Name of the screen.</param>
        public void PushScreen(string screenName)
        {
            var html = Game.Content.Load<string>(screenName);
            var screen = WebCore.CreateWebView(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);
            screen.FlushAlpha = false;
            screen.IsTransparent = true;
            screen.ResourceRequest += HandleResourceRequest;
            foreach (var callback in _callbacks)
            {
                var parts = callback.Key.Split(new[] {'.'}, 2);
                screen.CreateObject(parts[0]);
                screen.SetObjectCallback(parts[0], parts[1], callback.Value);
            }
            foreach (var value in _values)
            {
                var parts = value.Key.Split(new[] { '.' }, 2);
                screen.CreateObject(parts[0]);
                screen.SetObjectProperty(parts[0], parts[1], value.Value);
            }
            screen.LoadHTML(html);
            screen.Focus();
            _screens.Push(screen);
            _renderBuffer = null;
        }

        /// <summary>
        /// Pops the top screen and disposes it.
        /// </summary>
        public void PopScreen()
        {
            if (_screens.Count < 1)
            {
                return;
            }

            var top = _screens.Pop();
            if (_screens.Count > 0)
            {
                _screens.Peek().Focus();
            }
            top.Close();
            _renderBuffer = null;
        }

        /// <summary>
        /// Adds the callback to all screens, and will ensure the callback
        /// is added to all future screens.
        /// </summary>
        /// <param name="nameSpace">The name space to add the callback to.</param>
        /// <param name="name">The name of the callback.</param>
        /// <param name="callback">The callback.</param>
        public void AddCallback(string nameSpace, string name, JSCallback callback)
        {
            if (String.IsNullOrWhiteSpace(nameSpace))
            {
                throw new ArgumentException("Invalid namespace, must not be empty.");
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid name, must not be empty.");
            }

            _callbacks.Add(nameSpace + "." + name, callback);
            foreach (var screen in _screens)
            {
                screen.CreateObject(nameSpace);
                screen.SetObjectCallback(nameSpace, name, callback);
            }
        }

        /// <summary>
        /// Removes the callback from all screens.
        /// </summary>
        /// <param name="nameSpace">The name space to remove the callback from.</param>
        /// <param name="name">The name of the callback to remove.</param>
        public void RemoveCallback(string nameSpace, string name)
        {
            if (_callbacks.Remove(nameSpace + "." + name))
            {
                foreach (var screen in _screens)
                {
                    screen.SetObjectCallback(nameSpace, name, null);
                }
            }
        }

        /// <summary>
        /// Sets a variable to be available to all screens.
        /// </summary>
        /// <param name="nameSpace">The name space to add the value to.</param>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value.</param>
        public void SetVariable(string nameSpace, string name, JSValue value)
        {
            if (String.IsNullOrWhiteSpace(nameSpace))
            {
                throw new ArgumentException("Invalid namespace, must not be empty.");
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid name, must not be empty.");
            }

            _values[nameSpace + "." + name] = value;
            foreach (var screen in _screens)
            {
                screen.CreateObject(nameSpace);
                screen.SetObjectProperty(nameSpace, name, value);
            }
        }

        #endregion

        #region Events handling

        /// <summary>
        /// Handles key presses.
        /// </summary>
        /// <param name="key">The pressed key.</param>
        private void HandleKeyPressed(Keys key)
        {
            if (_screens.Count < 1)
            {
                return;
            }

            var e = new WebKeyboardEvent
                    {
                        Type = WebKeyType.KeyDown,
                        IsSystemKey = false,
                        VirtualKeyCode = (VirtualKey)key
                    };
            SetModifiers(e);
            _screens.Peek().InjectKeyboardEvent(e);

        }

        /// <summary>
        /// Handles character input.
        /// </summary>
        /// <param name="character">The typed character.</param>
        private void HandleCharacterEntered(char character)
        {
            if (_screens.Count < 1)
            {
                return;
            }

            var e = new WebKeyboardEvent
                    {
                        Type = WebKeyType.Char,
                        IsSystemKey = false,
                        Text = new ushort[] {character, 0, 0, 0},
                        UnmodifiedText = new ushort[] {character, 0, 0, 0},
                        VirtualKeyCode = (VirtualKey)VkKeyScan(character),
                        NativeKeyCode = character
                    };
            SetModifiers(e);
            _screens.Peek().InjectKeyboardEvent(e);
        }

        /// <summary>
        /// Handles key releases.
        /// </summary>
        /// <param name="key">The released key.</param>
        private void HandleKeyReleased(Keys key)
        {
            if (_screens.Count < 1)
            {
                return;
            }

            var e = new WebKeyboardEvent
                    {
                        Type = WebKeyType.KeyUp,
                        IsSystemKey = false,
                        VirtualKeyCode = (VirtualKey)key
                    };
            SetModifiers(e);
            _screens.Peek().InjectKeyboardEvent(e);
        }

        /// <summary>
        /// Sets the currently active keyboard modifiers for the specified event.
        /// </summary>
        /// <param name="e">The event.</param>
        private static void SetModifiers(WebKeyboardEvent e)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl) ||
                Keyboard.GetState().IsKeyDown(Keys.RightControl))
            {
                e.Modifiers |= WebKeyModifiers.ControlKey;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) ||
                Keyboard.GetState().IsKeyDown(Keys.RightShift))
            {
                e.Modifiers |= WebKeyModifiers.ShiftKey;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.LeftAlt) ||
                Keyboard.GetState().IsKeyDown(Keys.RightAlt))
            {
                e.Modifiers |= WebKeyModifiers.ShiftKey;
            }
        }

        /// <summary>
        /// Handles mouse button presses.
        /// </summary>
        /// <param name="buttons">The pressed button.</param>
        private void HandleMouseButtonPressed(MouseButtons buttons)
        {
            if (_screens.Count < 1)
            {
                return;
            }

            switch (buttons)
            {
                case MouseButtons.Left:
                    _screens.Peek().InjectMouseDown(MouseButton.Left);
                    break;
                case MouseButtons.Middle:
                    _screens.Peek().InjectMouseDown(MouseButton.Middle);
                    break;
                case MouseButtons.Right:
                    _screens.Peek().InjectMouseDown(MouseButton.Right);
                    break;
            }
        }

        /// <summary>
        /// Handles mouse button releases.
        /// </summary>
        /// <param name="buttons">The released button.</param>
        private void HandleMouseButtonReleased(MouseButtons buttons)
        {
            if (_screens.Count < 1)
            {
                return;
            }

            switch (buttons)
            {
                case MouseButtons.Left:
                    _screens.Peek().InjectMouseUp(MouseButton.Left);
                    break;
                case MouseButtons.Middle:
                    _screens.Peek().InjectMouseUp(MouseButton.Middle);
                    break;
                case MouseButtons.Right:
                    _screens.Peek().InjectMouseUp(MouseButton.Right);
                    break;
            }
        }

        /// <summary>
        /// Handles mouse movement.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        private void HandleMouseMoved(float x, float y)
        {
            if (_screens.Count < 1)
            {
                return;
            }

            _screens.Peek().InjectMouseMove((int)x, (int)y);
        }

        /// <summary>
        /// Handles mouse wheel rotation.
        /// </summary>
        /// <param name="ticks">The ticks scrolled.</param>
        private void HandleMouseWheelRotated(float ticks)
        {
            if (_screens.Count < 1)
            {
                return;
            }

            _screens.Peek().InjectMouseWheel((int)(ticks * ScrollAmount));
        }

        /// <summary>
        /// Handles downloads by redirecting them to the content manager.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Awesomium.Core.ResourceRequestEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        private ResourceResponse HandleResourceRequest(object sender, ResourceRequestEventArgs e)
        {
            try
            {
                var url = e.Request.Url.Replace("local://base_request.html/", "");
                var extIndex = url.LastIndexOf(".", StringComparison.InvariantCulture);
                var assetName = url.Substring(0, extIndex);
                var assetExtension = url.Substring(extIndex + 1);
                switch (assetExtension)
                {
                    case "png":
                    case "gif":
                    case "jpg":
                    case "jpeg":
                        var image = Game.Content.Load<Texture2D>(assetName);
                        using (var pngStream = new MemoryStream())
                        {
                            image.SaveAsPng(pngStream, image.Width, image.Height);
                            return new ResourceResponse(pngStream.GetBuffer(), "image/png");
                        }
                    case "css":
                        return new ResourceResponse(Encoding.UTF8.GetBytes(Game.Content.Load<string>(assetName)), "text/css");
                    case "html":
                    case "xhtml":
                        return new ResourceResponse(Encoding.UTF8.GetBytes(Game.Content.Load<string>(assetName)), "text/html");
                    case "js":
                        return new ResourceResponse(Encoding.UTF8.GetBytes(Game.Content.Load<string>(assetName)), "text/javascript");
                }
            }
            catch (ContentLoadException)
            {
                // Failed loading that asset, return null.
            }
            // We cannot handle that request, abort it.
            e.Request.Cancel();
            return null;
        }

        #endregion

        #region Default callbacks

        /// <summary>
        /// Allow pushing screens from within screens.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JSPushScreen(object sender, JSCallbackEventArgs e)
        {
            if (e.Arguments.Length == 1 && e.Arguments[0].Type == JSValueType.String)
            {
                PushScreen(e.Arguments[0].ToString());
            }
        }

        /// <summary>
        /// Allow popping screens from within screens.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Awesomium.Core.JSCallbackEventArgs"/> instance containing the event data.</param>
        private void JSPopScreen(object sender, JSCallbackEventArgs e)
        {
            PopScreen();
        }

        #endregion

        #region Externals

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        #endregion
    }
}
