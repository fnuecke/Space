using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Types

        /// <summary>
        /// Interface for methods that can be called from JavaScript.
        /// </summary>
        /// <param name="args">Arguments received from JS.</param>
        public delegate void JSCallback(JSValue[] args);

        /// <summary>
        /// Interface for methods that can be called from JavaScript and return
        /// a value.
        /// </summary>
        /// <param name="args">Arguments received from JS.</param>
        /// <returns>The value to return.</returns>
        public delegate JSValue JSCallbackWithReturnValue(JSValue[] args);

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
        private readonly List<JSCallBackInfo<JSCallback>> _callbacks = new List<JSCallBackInfo<JSCallback>>();
        
        /// <summary>
        /// List of callbacks with return values to expose to all screens.
        /// </summary>
        private readonly List<JSCallBackInfo<JSCallbackWithReturnValue>> _callbacksWithReturnValue = new List<JSCallBackInfo<JSCallbackWithReturnValue>>();

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
                WebCore.Initialize(new WebConfig
                                   {
#if DEBUG
                                       LogLevel = LogLevel.Verbose,
#else
                                       LogLevel = LogLevel.None,
#endif
                                       LogPath = Environment.CurrentDirectory
                                   });
            }
            _session = WebCore.CreateWebSession(new WebPreferences
            {
                CustomCSS = DefaultCSS,
                ProxyConfig = "none"
            });
            _dataSource = new ContentLoaderDataSource(Game.Content);
            _session.AddDataSource("space", _dataSource);

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

                WebCore.Shutdown();
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
            if (screen.Surface != null)
            {
                var surface = (BitmapSurface)screen.Surface;
                _backBuffer.RenderTexture2D(surface.Buffer);
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
            var html = Game.Content.Load<string>("Screens/" + screenName);
            var screen = WebCore.CreateWebView(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height, _session);
            screen.JSMethodHandler = new JavaScriptHandler(screen, _callbacks, _callbacksWithReturnValue);
            screen.LoadHTML(html);
            screen.FocusView();
            _screens.Push(screen);
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

            _callbacks.Add(new JSCallBackInfo<JSCallback>
                           {
                               Name = name,
                               Namespace = nameSpace,
                               Callback = callback
                           });
            foreach (var screen in _screens)
            {
                ((JavaScriptHandler)screen.JSMethodHandler).AddCallback(nameSpace, name, callback);
            }
        }
        
        /// <summary>
        /// Adds the callback to all screens, and will ensure the callback
        /// is added to all future screens.
        /// </summary>
        /// <param name="nameSpace">The name space to add the callback to.</param>
        /// <param name="name">The name of the callback.</param>
        /// <param name="callback">The callback.</param>
        public void AddCallbackWithReturnValue(string nameSpace, string name, JSCallbackWithReturnValue callback)
        {
            if (String.IsNullOrWhiteSpace(nameSpace))
            {
                throw new ArgumentException("Invalid namespace, must not be empty.");
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid name, must not be empty.");
            }

            _callbacksWithReturnValue.Add(new JSCallBackInfo<JSCallbackWithReturnValue>
                                          {
                               Name = name,
                               Namespace = nameSpace,
                               Callback = callback
                           });
            foreach (var screen in _screens)
            {
                ((JavaScriptHandler)screen.JSMethodHandler).AddCallback(nameSpace, name, callback);
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

            _screens.Peek().InjectMouseWheel((int)(ticks * ScrollAmount),  0);
        }
        
        #endregion

        #region JS Callback handler

        private sealed class JavaScriptHandler : IJSMethodHandler
        {
            private readonly WebView _webView;
            
            private readonly List<JSCallBackInfo<JSCallback>> _exitingCallbacks;

            private readonly List<JSCallBackInfo<JSCallbackWithReturnValue>> _existingCallbacksWithReturnValue;

            private bool _areGlobalObjectsInitialized;

            private readonly Dictionary<string, JSValue> _nameSpaces = new Dictionary<string, JSValue>();

            private readonly Dictionary<uint, Dictionary<string, JSCallback>> _callbacks = new Dictionary<uint, Dictionary<string, JSCallback>>();

            private readonly Dictionary<uint, Dictionary<string, JSCallbackWithReturnValue>> _callbacksWithReturnValue = new Dictionary<uint, Dictionary<string, JSCallbackWithReturnValue>>();

            public JavaScriptHandler(WebView webView, List<JSCallBackInfo<JSCallback>> existingCallbacks, List<JSCallBackInfo<JSCallbackWithReturnValue>> existingCallbacksWithReturnValue)
            {
                _webView = webView;
                _exitingCallbacks = existingCallbacks;
                _existingCallbacksWithReturnValue = existingCallbacksWithReturnValue;
                if (webView.IsDocumentReady)
                {
                    CreateGlobalObjects();
                } else
                {
                    webView.DocumentReady += WebViewOnDocumentReady;
                }
            }

            private void WebViewOnDocumentReady(object sender, UrlEventArgs urlEventArgs)
            {
                _webView.DocumentReady -= WebViewOnDocumentReady;
                CreateGlobalObjects();
                _webView.IsTransparent = true;
            }

            private void CreateGlobalObjects()
            {
                foreach (var callbackInfo in _exitingCallbacks)
                {
                    SetCallback(callbackInfo.Namespace, callbackInfo.Name, callbackInfo.Callback);
                }
                foreach (var callbackInfo in _existingCallbacksWithReturnValue)
                {
                    SetCallback(callbackInfo.Namespace, callbackInfo.Name, callbackInfo.Callback);
                }
                _areGlobalObjectsInitialized = true;
            }

            private void SetCallback(string nameSpace, string name, JSCallback callback)
            {
                var ns = _nameSpaces.ContainsKey(nameSpace)
                             ? _nameSpaces[nameSpace]
                             : (_nameSpaces[nameSpace] = _webView.CreateGlobalJavascriptObject(nameSpace));
                using (var obj = ns.ToObject())
                {
                    obj.SetCustomMethod(name, false);
                    if (!_callbacks.ContainsKey(obj.RemoteID))
                    {
                        _callbacks[obj.RemoteID] = new Dictionary<string, JSCallback>();
                    }
                    _callbacks[obj.RemoteID][name] = callback;
                }
            }

            private void SetCallback(string nameSpace, string name, JSCallbackWithReturnValue callback)
            {
                var ns = _nameSpaces.ContainsKey(nameSpace)
                             ? _nameSpaces[nameSpace]
                             : (_nameSpaces[nameSpace] = _webView.CreateGlobalJavascriptObject(nameSpace));
                using (var obj = ns.ToObject())
                {
                    obj.SetCustomMethod(name, true);
                    if (!_callbacksWithReturnValue.ContainsKey(obj.RemoteID))
                    {
                        _callbacksWithReturnValue[obj.RemoteID] = new Dictionary<string, JSCallbackWithReturnValue>();
                    }
                    _callbacksWithReturnValue[obj.RemoteID][name] = callback;
                }
            }

            public void AddCallback(string nameSpace, string name, JSCallback callback)
            {
                if (_areGlobalObjectsInitialized)
                {
                    SetCallback(nameSpace, name, callback);
                }
            }

            public void AddCallback(string nameSpace, string name, JSCallbackWithReturnValue callback)
            {
                if (_areGlobalObjectsInitialized)
                {
                    SetCallback(nameSpace, name, callback);
                }
            }

            public void OnMethodCall(IWebView caller, uint remoteObjectID, string methodName, params JSValue[] args)
            {
                if (_callbacks.ContainsKey(remoteObjectID))
                {
                    var ns = _callbacks[remoteObjectID];
                    if (ns.ContainsKey(methodName))
                    {
                        ns[methodName](args);
                    }
                }
            }

            public JSValue OnMethodCallWithReturnValue(IWebView caller, uint remoteObjectID, string methodName, params JSValue[] args)
            {
                if (_callbacksWithReturnValue.ContainsKey(remoteObjectID))
                {
                    var ns = _callbacksWithReturnValue[remoteObjectID];
                    if (ns.ContainsKey(methodName))
                    {
                        return ns[methodName](args);
                    }
                }

                return JSValue.CreateUndefined();
            }
        }
        
        private sealed class JSCallBackInfo<T>
        {
            public string Namespace;

            public string Name;
            
            public T Callback;
        }

        #endregion

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

            public override void OnRequest(int requestId, string path)
            {
                try
                {
                    var url = path;
                    var extIndex = url.LastIndexOf(".", StringComparison.InvariantCulture);
                    var assetName = url.Substring(0, extIndex);
                    var assetExtension = url.Substring(extIndex + 1);
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
                                SendResponse(requestId, pngStream.GetBuffer(), "image/png");
                                return;
                            }
                        case "css":
                            SendResponse(requestId, Encoding.UTF8.GetBytes(_content.Load<string>(assetName)), "text/css");
                            return;
                        case "html":
                        case "xhtml":
                            SendResponse(requestId, Encoding.UTF8.GetBytes(_content.Load<string>(assetName)), "text/html");
                            return;
                        case "js":
                            SendResponse(requestId, Encoding.UTF8.GetBytes(_content.Load<string>(assetName)), "text/javascript");
                            return;
                    }
                }
                catch (ContentLoadException ex)
                {
                    // Failed loading that asset, return null.
                    Logger.WarnException("Failed loading a resource for a web view.", ex);
                }
                // We cannot handle that request, abort it.
            }

            private void SendResponse(int requestId, byte[] buffer, string mimeType)
            {
                var size = Marshal.SizeOf(buffer[0]) * buffer.Length;
                var pointer = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.Copy(buffer, 0, pointer, buffer.Length);
                    SendResponse(requestId, (uint)size, pointer, mimeType);
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
        private void JSPushScreen(JSValue[] args)
        {
            if (args.Length == 1 && args[0].IsString)
            {
                PushScreen(args[0].ToString());
            }
        }

        /// <summary>
        /// Allow popping screens from within screens.
        /// </summary>
        private void JSPopScreen(JSValue[] args)
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
