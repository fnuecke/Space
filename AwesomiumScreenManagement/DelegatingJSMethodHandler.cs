using System;
using System.Collections.Generic;
using Awesomium.Core;

namespace Awesomium.ScreenManagement
{
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

    /// <summary>
    /// This is a simple implementation of a JavaScript method handler that
    /// leaves callback logic to registered delegate functions.
    /// 
    /// <para>
    /// Usage:
    /// * Create instance for a WebView. This will automatically
    /// register the handler with the WebView.
    /// * Use the RegisterCallback methods to add delegates.
    /// * In Javascript, call the methods.
    /// </para>
    /// 
    /// <para>
    /// Example:
    /// C#
    /// <code>
    /// void initWebView(WebView webView) {
    ///   var handler = new DelegatingJSMethodHandler(webView);
    ///   handler.RegisterCallback("Globals", "test", (args) => Console.WriteLine("in test"));
    /// }
    /// </code>
    /// JS
    /// <code>
    /// Globals.test();
    /// </code>
    /// </para>
    /// </summary>
    public sealed class DelegatingJSMethodHandler : IJSMethodHandler
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Fields

        /// <summary>
        /// The WebView this handler is attached to.
        /// </summary>
        private readonly WebView _webView;

        /// <summary>
        /// A list of registered callbacks.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, JSCallback>> _callbacks = new Dictionary<string, Dictionary<string, JSCallback>>();

        /// <summary>
        /// A list of registered callbacks with return value.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, JSCallbackWithReturnValue>> _callbacksWithReturnValue = new Dictionary<string, Dictionary<string, JSCallbackWithReturnValue>>();

        /// <summary>
        /// A mapping of names to proxies representing the global object with that name.
        /// </summary>
        private readonly Dictionary<string, JSValue> _objects = new Dictionary<string, JSValue>();

        /// <summary>
        /// Maps remote IDs to the name of the global object they represent.
        /// </summary>
        private readonly Dictionary<uint, string> _objectNames = new Dictionary<uint, string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new delegating method handler for the specified WebView.
        /// </summary>
        /// <param name="webView">The webview to create the handler for.</param>
        public DelegatingJSMethodHandler(WebView webView)
        {
            _webView = webView;
            _webView.JSMethodHandler = this;
            _webView.DocumentReady += WebViewOnDocumentReady;
            if (_webView.IsDocumentReady)
            {
                WebViewOnDocumentReady(null, null);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// When we are done loading, recreate our callbacks.
        /// </summary>
        private void WebViewOnDocumentReady(object sender, UrlEventArgs urlEventArgs)
        {
            // Clear previous references, as they are invalid now.
            _objects.Clear();
            _objectNames.Clear();

            // Register normal callbacks.
            foreach (var nameSpace in _callbacks)
            {
                foreach (var name in nameSpace.Value.Keys)
                {
                    SetCallback(nameSpace.Key, name);
                }
            }

            // Register callbacks with return value.
            foreach (var nameSpace in _callbacksWithReturnValue)
            {
                foreach (var name in nameSpace.Value.Keys)
                {
                    SetCallbackWithReturnValue(nameSpace.Key, name);
                }
            }
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Register a new callback.
        /// </summary>
        /// <param name="nameSpace">The namespace to expose the callback in.</param>
        /// <param name="name">The name of the method in JavaScript.</param>
        /// <param name="callback">The callback to associate with that method.</param>
        public void RegisterCallback(string nameSpace, string name, JSCallback callback)
        {
            // Make sure we don't get the same handler twice.
            RemoveCallback(nameSpace, name);

            // Remember the callback.
            if (!_callbacks.ContainsKey(nameSpace))
            {
                _callbacks[nameSpace] = new Dictionary<string, JSCallback>();
            }
            _callbacks[nameSpace][name] = callback;

            // Set it in the current Javascript context, if we have one.
            if (_webView.IsDocumentReady)
            {
                SetCallback(nameSpace, name);
            }
        }

        /// <summary>
        /// Register a new callback with a return value.
        /// </summary>
        /// <param name="nameSpace">The namespace to expose the callback in.</param>
        /// <param name="name">The name of the method in JavaScript.</param>
        /// <param name="callback">The callback to associate with that method.</param>
        public void RegisterCallback(string nameSpace, string name, JSCallbackWithReturnValue callback)
        {
            // Make sure we don't get the same handler twice.
            RemoveCallback(nameSpace, name);

            // Remember the callback.
            if (!_callbacksWithReturnValue.ContainsKey(nameSpace))
            {
                _callbacksWithReturnValue[nameSpace] = new Dictionary<string, JSCallbackWithReturnValue>();
            }
            _callbacksWithReturnValue[nameSpace][name] = callback;

            // Set it in the current Javascript context, if we have one.
            if (_webView.IsDocumentReady)
            {
                SetCallbackWithReturnValue(nameSpace, name);
            }
        }

        /// <summary>
        /// Remove a callback. This does not discriminate between normal
        /// callbacks, and callbacks with return value. It will remove either.
        /// </summary>
        /// <param name="nameSpace">The namespace to remove the callback from.</param>
        /// <param name="name">The name of the method in Javascript.</param>
        public void RemoveCallback(string nameSpace, string name)
        {
            // Remove from callback list.
            if (_callbacks.ContainsKey(nameSpace) &&
                _callbacks[nameSpace].ContainsKey(name))
            {
                _callbacks[nameSpace].Remove(name);
                // Clean up empty namespaces.
                if (_callbacks[nameSpace].Count == 0)
                {
                    _callbacks.Remove(nameSpace);
                }
            }
            // Remove from callback with return value list.
            if (_callbacksWithReturnValue.ContainsKey(nameSpace) &&
                _callbacksWithReturnValue[nameSpace].ContainsKey(name))
            {
                _callbacksWithReturnValue[nameSpace].Remove(name);
                // Clean up empty namespaces.
                if (_callbacksWithReturnValue[nameSpace].Count == 0)
                {
                    _callbacksWithReturnValue.Remove(nameSpace);
                }
            }

            // Remove from Javascript context.
            if (_webView.IsDocumentReady)
            {
                // Only if the namespace exists.
                var ns = GetNameSpace(nameSpace, false);
                if (ns != null)
                {
                    using (var obj = ns.ToObject())
                    {
                        // And the method exists.
                        if (obj.HasMethod(name))
                        {
                            obj.RemoveProperty(name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Call a method in JavaScript. This is really just a convenience wrapper
        /// for <code>JSObject.Invoke</code>. Useful for implementing events, i.e.
        /// to call <code>handler.Call("G", "onSomething", args);</code>, as it will
        /// avoid raising errors if there is no such method.
        /// </summary>
        /// <param name="nameSpace">The namespace the method to call resides in.</param>
        /// <param name="name">The name of the method to call.</param>
        /// <param name="args">The arguments to pass to the called method.</param>
        public void Call(string nameSpace, string name, params JSValue[] args)
        {
            // Ignore if we're not ready for JS yet.
            if (_webView.IsDocumentReady)
            {
                // Get the object.
                var ns = GetNameSpace(nameSpace, false);
                if (ns != null && ns.IsObject)
                {
                    using (var obj = ns.ToObject())
                    {
                        if (obj.HasMethod(name))
                        {
                            obj.Invoke(name, args);
                        }
                    }
                }
            }
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Utility method to get the proxy for the global object with the
        /// specified name. Note that the object will be a remote object,
        /// living in the V8 context, <em>not</em> an object created via
        /// <code>WebView.CreateGlobalJavascriptObject</code>. This allows
        /// for the possibility to register functions from within JavaScript,
        /// used as event listeners, e.g.
        /// It also means changes will be lost on page changes, though.
        /// 
        /// <para>
        /// If no namespace with that name exists, yet, or the global variable
        /// with that name is not an object, one will be created.
        /// </para>
        /// </summary>
        /// <param name="nameSpace">The namespace to get a reference to.</param>
        /// <param name="create">Whether to create the namespace if it doesn't exist.</param>
        /// <returns></returns>
        private JSValue GetNameSpace(string nameSpace, bool create = true)
        {
            // If we already have the reference skip this part.
            if (!_objects.ContainsKey(nameSpace))
            {
                // Check if a global object of that name exists...
                _objects[nameSpace] = _webView.ExecuteJavascriptWithResult("window." + nameSpace, string.Empty);
                // ... and is a valid object.
                if (!_objects[nameSpace].IsObject)
                {
                    if (!create)
                    {
                        // No such object, and we're not to create one.
                        return null;
                    }

                    // Not an object, but if there is something there, log a warning, as we're overwriting it.
                    if (!_objects[nameSpace].IsUndefined && !_objects[nameSpace].IsNull)
                    {
                        Logger.Warn("Possibly overwriting a global object '{0}'.", nameSpace);
                    }

                    // Create our object representing our namespace.
                    _webView.ExecuteJavascript("window." + nameSpace + " = {};", string.Empty);
                    // Then get a reference to it.
                    _objects[nameSpace] = _webView.ExecuteJavascriptWithResult("window." + nameSpace, string.Empty);
                }
                
                // Remember the name of the namespace for the object with that remote id.
                using (var ns = _objects[nameSpace].ToObject())
                {
                    if (!_objectNames.ContainsKey(ns.RemoteID))
                    {
                        _objectNames[ns.RemoteID] = nameSpace;
                    }
                }
            }
            return _objects[nameSpace];
        }

        /// <summary>
        /// Utility method to register a callback in a namespace.
        /// </summary>
        /// <param name="nameSpace">The namespace.</param>
        /// <param name="name">The name of the method.</param>
        private void SetCallback(string nameSpace, string name)
        {
            using (var ns = GetNameSpace(nameSpace).ToObject())
            {
                ns.SetCustomMethod(name, false);
            }
        }

        /// <summary>
        /// Utility method to register a callback with return value in a namespace.
        /// </summary>
        /// <param name="nameSpace">The namespace.</param>
        /// <param name="name">The name of the method.</param>
        private void SetCallbackWithReturnValue(string nameSpace, string name)
        {
            using (var ns = GetNameSpace(nameSpace).ToObject())
            {
                ns.SetCustomMethod(name, true);
            }
        }

        #endregion

        #region Interface

        public void OnMethodCall(IWebView caller, uint remoteObjectID, string methodName, params JSValue[] args)
        {
            if (_objectNames.ContainsKey(remoteObjectID))
            {
                var nameSpace = _objectNames[remoteObjectID];
                if (_callbacks.ContainsKey(nameSpace) && _callbacks[nameSpace].ContainsKey(methodName))
                {
                    try
                    {
                        _callbacks[nameSpace][methodName](args);
                    }
                    catch (Exception ex)
                    {
                        Logger.WarnException("Error in JSCallback '" + methodName + "':", ex);
                    }
                }
            }
        }

        public JSValue OnMethodCallWithReturnValue(IWebView caller, uint remoteObjectID, string methodName,
                                                    params JSValue[] args)
        {
            if (_objectNames.ContainsKey(remoteObjectID))
            {
                var nameSpace = _objectNames[remoteObjectID];
                if (_callbacksWithReturnValue.ContainsKey(nameSpace) && _callbacksWithReturnValue[nameSpace].ContainsKey(methodName))
                {
                    try
                    {
                        return _callbacksWithReturnValue[nameSpace][methodName](args);
                    }
                    catch (Exception ex)
                    {
                        Logger.WarnException("Error in JSCallbackWithReturnValue '" + methodName + "':", ex);
                    }
                }
            }

            return JSValue.CreateUndefined();
        }

        #endregion
    }
}
