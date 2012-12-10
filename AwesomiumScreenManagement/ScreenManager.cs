using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Awesomium.Core;
using Awesomium.Core.Data;
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
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        /// <summary>
        /// Some custom default CSS to add to all screens.
        /// </summary>
        private const string DefaultCSS = @"
body {
    background-color: transparent;
}
* {
    -webkit-user-select: none;
}
input[type=""text""], input[type=""password""], textarea {
    -webkit-user-select: text;
}";

        #endregion

        #region Properties

        /// <summary>
        /// How many pixels to scroll per tick.
        /// </summary>
        public int ScrollAmount { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Whether we own the web core, i.e. we caused its initialization.
        /// </summary>
        private readonly bool _ownsWebCore;

        /// <summary>
        /// The sprite batch to render into.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The input manager used for updating.
        /// </summary>
        private readonly InputManager _inputManager;

        /// <summary>
        /// Used to render screens into before painting them to the screen.
        /// </summary>
        private readonly Texture2D _backBuffer;

        /// <summary>
        /// Data source to allow loading stuff from content loader.
        /// </summary>
        private readonly ContentLoaderDataSource _dataSource;

        /// <summary>
        /// WebSession in use for all our views.
        /// </summary>
        private readonly WebSession _session;

        /// <summary>
        /// List of callbacks to expose to all screens.
        /// </summary>
        private readonly List<JSCallBackInfo<JavascriptMethodEventHandler>> _callbacks = new List<JSCallBackInfo<JavascriptMethodEventHandler>>();

        /// <summary>
        /// List of callbacks with return values to expose to all screens.
        /// </summary>
        private readonly List<JSCallBackInfo<JavascriptMethodEventHandler>> _callbacksWithReturnValue = new List<JSCallBackInfo<JavascriptMethodEventHandler>>();

        /// <summary>
        /// All currently tracked screens.
        /// </summary>
        private readonly Stack<WebView> _screens = new Stack<WebView>();

        /// <summary>
        /// We skip every second draw call with copying the texture, because 30 fps
        /// should be enough for a GUI.
        /// </summary>
        private bool _wantToRender;

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

            // Create texture to blit web screen into for XNA rendering.
            _backBuffer = new Texture2D(Game.GraphicsDevice,
                                        Game.GraphicsDevice.Viewport.Width,
                                        Game.GraphicsDevice.Viewport.Height);

            // Register for events.
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

            // Start webcore if it's not running.
            if (!WebCore.IsRunning)
            {
                WebCore.Initialize(new WebConfig
                {
#if DEBUG
                    LogLevel = LogLevel.Verbose,
#else
                    LogLevel = LogLevel.None,
#endif
                    LogPath = Environment.CurrentDirectory + "\\awesomium.log",
                    RemoteDebuggingPort = 1337
                });
                // If we created it, we shut it down on disposal, too.
                _ownsWebCore = true;
            }

            // Create our session.
            _session = WebCore.CreateWebSession(new WebPreferences
            {
                CustomCSS = DefaultCSS,
                WebSecurity = false,
                ProxyConfig = "none"
            });

            // Register our custom data source. Keep a reference to it, as the
            // one the session holds seems to be a weak one.
            _dataSource = new ContentLoaderDataSource(Game.Content);
            _session.AddDataSource("xna", _dataSource);

            // Add default callbacks to allow JS to modify screens.
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
                    screen.Dispose();
                }
                _screens.Clear();

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

                _session.Dispose();

                _dataSource.Dispose();

                if (_ownsWebCore)
                {
                    WebCore.Shutdown();
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Update the Awesomium WebCore.
        /// </summary>
        /// <param name="gameTime">Time that passed since the last call to update.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            WebCore.Update();
        }

        /// <summary>
        /// Draws the active screens (topmost one).
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
            if (screen.Surface != null)
            {
                if (_wantToRender)
                {
                    var surface = (BitmapSurface)screen.Surface;
                    _backBuffer.RenderTexture2D(surface.Buffer);
                }
                _wantToRender = !_wantToRender;

                _spriteBatch.Begin();
                _spriteBatch.Draw(_backBuffer, Vector2.Zero, Color.White);
                _spriteBatch.End();
            }
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Pushes the screen with the specified name to the top.
        /// </summary>
        /// <param name="screenName">Name of the screen.</param>
        public void PushScreen(string screenName)
        {
            // Create our screen.
            var screen = WebCore.CreateWebView(Game.GraphicsDevice.Viewport.Width,
                                               Game.GraphicsDevice.Viewport.Height,
                                               _session);

            // Force screen to be transparent and register callbacks.
            screen.DocumentReady += HandleWebViewDocumentReady;

            // Load the HTML for that screen and focus it.
            screen.LoadHTML(Game.Content.Load<string>("Screens/" + screenName));
            screen.FocusView();
            _screens.Push(screen);
        }

        private void HandleWebViewDocumentReady(object sender, UrlEventArgs e)
        {
            var screen = (WebView)sender;
            screen.IsTransparent = true;

            // Register normal callbacks.
            foreach (var callback in _callbacks)
            {
                GetNamespace(screen, callback.Namespace, true).Bind(callback.Name, false, callback.Callback);
            }

            // Register callbacks with return value.
            foreach (var callback in _callbacksWithReturnValue)
            {
                GetNamespace(screen, callback.Namespace, true).Bind(callback.Name, true, callback.Callback);
            }

            // Trigger init event.
            Call("Screens", "onInit");
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
            top.UnfocusView();
            if (_screens.Count > 0)
            {
                _screens.Peek().FocusView();
            }
            top.Dispose();
        }

        /// <summary>
        /// Adds the callback to all screens, and will ensure the callback
        /// is added to all future screens.
        /// </summary>
        /// <param name="nameSpace">The name space to add the callback to.</param>
        /// <param name="name">The name of the callback.</param>
        /// <param name="callback">The callback.</param>
        public void AddCallback(string nameSpace, string name, JavascriptMethodEventHandler callback)
        {
            if (String.IsNullOrWhiteSpace(nameSpace))
            {
                throw new ArgumentException("Invalid namespace, must not be empty.");
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid name, must not be empty.");
            }

            _callbacks.Add(new JSCallBackInfo<JavascriptMethodEventHandler>
            {
                Name = name,
                Namespace = nameSpace,
                Callback = callback
            });
            foreach (var screen in _screens)
            {
                GetNamespace(screen, nameSpace, true).Bind(name, false, callback);
            }
        }

        /// <summary>
        /// Adds the callback to all screens, and will ensure the callback
        /// is added to all future screens.
        /// </summary>
        /// <param name="nameSpace">The name space to add the callback to.</param>
        /// <param name="name">The name of the callback.</param>
        /// <param name="callback">The callback.</param>
        public void AddCallbackWithReturnValue(string nameSpace, string name, JavascriptMethodEventHandler callback)
        {
            if (String.IsNullOrWhiteSpace(nameSpace))
            {
                throw new ArgumentException("Invalid namespace, must not be empty.");
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid name, must not be empty.");
            }

            _callbacksWithReturnValue.Add(new JSCallBackInfo<JavascriptMethodEventHandler>
            {
                Name = name,
                Namespace = nameSpace,
                Callback = callback
            });
            foreach (var screen in _screens)
            {
                GetNamespace(screen, nameSpace, true).Bind(name, true, callback);
            }
        }

        /// <summary>
        /// Call a JavaScript method in the currently active screen.
        /// </summary>
        /// <param name="nameSpace">The name of the global object in which the method resides.</param>
        /// <param name="name">The name of the method to call.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        public void Call(string nameSpace, string name, params JSValue[] args)
        {
            if (String.IsNullOrWhiteSpace(nameSpace))
            {
                throw new ArgumentException("Invalid namespace, must not be empty.");
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid name, must not be empty.");
            }

            if (_screens.Count < 1)
            {
                return;
            }

            var obj = GetNamespace(_screens.Peek(), nameSpace, false);
            if (obj.HasMethod(name))
            {
                obj.Invoke(name, args);
            }
        }

        /// <summary>
        /// Utility method to get global namespace of specified name if it exists,
        /// or create it if not.
        /// </summary>
        /// <param name="screen">The screen to create the global namespace for.</param>
        /// <param name="nameSpace">The name of the namespace.</param>
        /// <param name="create">Whether to create the namespace if it doesn't exist, yet.</param>
        /// <returns>The namespace.</returns>
        private static JSObject GetNamespace(IWebView screen, string nameSpace, bool create)
        {
            var ns = screen.ExecuteJavascriptWithResult("window." + nameSpace);
            if (ns.IsObject)
            {
                return ns;
            }
            else if (create)
            {
                // Not an object, but if there is something there, log a warning, as we're overwriting it.
                if (!ns.IsUndefined && !ns.IsNull)
                {
                    Logger.Warn("Possibly overwriting a global object '{0}'.", nameSpace);
                }
                return screen.ExecuteJavascriptWithResult("window." + nameSpace + " = {};");
            }
            return JSValue.Undefined;
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
                Type = WebKeyboardEventType.KeyDown,
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
                Type = WebKeyboardEventType.Char,
                IsSystemKey = false,
                Text = new string(character, 1),
                UnmodifiedText = new string(character, 1),
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
                Type = WebKeyboardEventType.KeyUp,
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
                e.Modifiers |= Modifiers.ControlKey;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) ||
                Keyboard.GetState().IsKeyDown(Keys.RightShift))
            {
                e.Modifiers |= Modifiers.ShiftKey;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.LeftAlt) ||
                Keyboard.GetState().IsKeyDown(Keys.RightAlt))
            {
                e.Modifiers |= Modifiers.ShiftKey;
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

            _screens.Peek().InjectMouseWheel((int)(ticks * ScrollAmount), 0);
        }

        #endregion

        private sealed class JSCallBackInfo<T>
        {
            public string Namespace;

            public string Name;

            public T Callback;
        }

        #region DataSource

        /// <summary>
        /// Handles downloads by redirecting them to the content manager.
        /// </summary>
        private sealed class ContentLoaderDataSource : DataSource
        {
            private readonly ContentManager _content;

            public ContentLoaderDataSource(ContentManager content)
            {
                _content = content;
            }

            protected override void OnRequest(DataSourceRequest request)
            {
                var url = request.Path;
                var extIndex = url.LastIndexOf(".", StringComparison.InvariantCulture);
                string assetName;
                string assetExtension;
                if (extIndex < 0)
                {
                    // In case we have no extension we fall back to assuming it's HTML,
                    // so use the entire string as the url.
                    assetName = url;
                    assetExtension = string.Empty;
                }
                else
                {
                    assetName = url.Substring(0, extIndex);
                    assetExtension = url.Substring(extIndex + 1);
                }
                try
                {
                    switch (assetExtension)
                    {
                        case "png":
                        case "gif":
                        case "jpg":
                        case "jpeg":
                            var image = _content.Load<Texture2D>(assetName);
                            using (var pngStream = new MemoryStream())
                            {
                                image.SaveAsPng(pngStream, image.Width, image.Height);
                                SendResponse(request, pngStream.GetBuffer(), "image/png");
                                return;
                            }
                        case "css":
                            SendResponse(request, Encoding.UTF8.GetBytes(_content.Load<string>(assetName)), "text/css");
                            return;
                        case "js":
                            SendResponse(request, Encoding.UTF8.GetBytes(_content.Load<string>(assetName)), "text/javascript");
                            return;
                        case "xml":
                            SendResponse(request, Encoding.UTF8.GetBytes(_content.Load<string>(assetName)), "application/xml");
                            return;
                        //case "html":
                        //case "xhtml":
                        default:
                            SendResponse(request, Encoding.UTF8.GetBytes(_content.Load<string>(assetName)), "text/html");
                            return;
                    }
                }
                catch (ContentLoadException ex)
                {
                    // Failed loading that asset, return null.
                    Logger.WarnException("Failed loading a resource for a web view: '" + url + "'. Is the file registered in the content project?", ex);
                }
                // We cannot handle that request, abort it.
                SendRequestFailed(request);
            }

            private void SendResponse(DataSourceRequest request, byte[] buffer, string mimeType)
            {
                var size = Marshal.SizeOf(buffer[0]) * buffer.Length;
                var pointer = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.Copy(buffer, 0, pointer, buffer.Length);
                    SendResponse(request,
                                 new DataSourceResponse
                                 {
                                     Buffer = pointer,
                                     Size = (uint)buffer.Length,
                                     MimeType = mimeType
                                 });
                }
                finally
                {
                    Marshal.FreeHGlobal(pointer);
                }
            }
        }

        #endregion

        #region Default callbacks

        /// <summary>
        /// Allow pushing screens from within screens.
        /// </summary>
        private void JSPushScreen(object sender, JavascriptMethodEventArgs e)
        {
            if (e.Arguments.Length == 1 && e.Arguments[0].IsString)
            {
                PushScreen(e.Arguments[0].ToString());
            }
        }

        /// <summary>
        /// Allow popping screens from within screens.
        /// </summary>
        private void JSPopScreen(object sender, JavascriptMethodEventArgs e)
        {
            PopScreen();
        }

        #endregion

        #region Externals

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        #endregion
    }
}
