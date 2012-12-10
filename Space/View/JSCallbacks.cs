using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Awesomium.Core;
using Engine.Session;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Systems;
using Space.Util;

namespace Space.View
{
    /// <summary>
    /// Utility class that defines methods to expose to JavaScript in the GUI.
    /// </summary>
    internal sealed class JSCallbacks
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Fields

        private static JSCallbacks _instance;

        /// <summary>
        /// The game we work for.
        /// </summary>
        private readonly Program _game;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Registers all callbacks for the specified game.
        /// </summary>
        /// <param name="game">The instance of the game to initialize for.</param>
        private JSCallbacks(Program game)
        {
            _game = game;
            _game.ClientInitialized += HandleClientInitialized;

            SetupJavaScriptApi();
        }

        /// <summary>
        /// Registers all callbacks for the specified game.
        /// </summary>
        /// <param name="game">The instance of the game to initialize for.</param>
        public static void Initialize(Program game)
        {
            if (_instance == null)
            {
                _instance = new JSCallbacks(game);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handle client re-initialization, which means we have a new session
        /// to which to add listeners for forwarding events.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Space.ClientInitializedEventArgs"/> instance containing the event data.</param>
        private void HandleClientInitialized(object sender, ClientInitializedEventArgs e)
        {
            var session = e.Client.Controller.Session;
            session.GameInfoReceived += SessionOnGameInfoReceived;
            session.JoinResponse += SessionOnJoinResponse;
            session.Disconnected += SessionOnDisconnected;
            session.PlayerJoined += SessionOnPlayerJoined;
            session.PlayerLeft += SessionOnPlayerLeft;
        }

        /// <summary>
        /// Forwards info received on a running game session.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Engine.Session.GameInfoReceivedEventArgs"/> instance containing the event data.</param>
        private void SessionOnGameInfoReceived(object sender, GameInfoReceivedEventArgs e)
        {
            var args = new JSObject();
            args["host"] = e.Host.Address.ToString();
            args["numPlayers"] = e.NumPlayers;
            args["maxPlayers"] = e.MaxPlayers;
            args["data"] = JSValue.Null; // TODO think of what else we might want to send
            _game.ScreenManager.Call("Space", "onGameInfoReceived", new[] {new JSValue(args)});
        }

        /// <summary>
        /// Forwards the info that we successfully connected to a game.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Engine.Session.JoinResponseEventArgs"/> instance containing the event data.</param>
        private void SessionOnJoinResponse(object sender, JoinResponseEventArgs e)
        {
            _game.ScreenManager.Call("Space", "onConnected");
        }

        /// <summary>
        /// Forwards the info that we have been disconnected from a gaem.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void SessionOnDisconnected(object sender, EventArgs e)
        {
            _game.ScreenManager.Call("Space", "onDisconnected");
        }

        /// <summary>
        /// Forwards the info that another player has joined the game.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Engine.Session.PlayerEventArgs"/> instance containing the event data.</param>
        private void SessionOnPlayerJoined(object sender, PlayerEventArgs e)
        {
            var args = new JSObject();
            args["number"] = e.Player.Number;
            args["name"] = e.Player.Name;
            _game.ScreenManager.Call("Space", "onPlayerJoined", new[] { new JSValue(args) });
        }

        /// <summary>
        /// Forwards the info that another player has left the game.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Engine.Session.PlayerEventArgs"/> instance containing the event data.</param>
        private void SessionOnPlayerLeft(object sender, PlayerEventArgs e)
        {
            var args = new JSObject();
            args["number"] = e.Player.Number;
            args["name"] = e.Player.Name;
            _game.ScreenManager.Call("Space", "onPlayerLeft", new[] { new JSValue(args) });
        }

        #endregion

        #region Setup

        /// <summary>
        /// Registers all callbacks for the JavaScript API.
        /// </summary>
        private void SetupJavaScriptApi()
        {
            // Shorthand.
            var s = _game.ScreenManager;

            // Localization.
            s.AddCallbackWithReturnValue("window", "L", GetGuiString);

            // Global menu options.
            s.AddCallback("Space", "startLocalGame", StartLocalGame);
            s.AddCallback("Space", "hostGame", HostGame);
            s.AddCallback("Space", "joinGame", JoinGame);
            s.AddCallback("Space", "leaveGame", LeaveGame);
            s.AddCallback("Space", "searchGames", SearchGames);

            // Settings related callbacks.
            s.AddCallback("Space", "getSettingInfos", AsyncCallbackWithResult(GetSettingInfos));
            s.AddCallback("Space", "getSetting", AsyncCallbackWithResult(GetSetting, 1));
            s.AddCallback("Space", "setSetting", SetSetting);
            s.AddCallback("Space", "getGameCommands", AsyncCallbackWithResult(GetGameCommands));

            // Ingame information.
            s.AddCallback("Space", "getNumPlayers", AsyncCallbackWithResult(GetNumPlayers));
            s.AddCallback("Space", "getMaxPlayers", AsyncCallbackWithResult(GetMaxPlayers));
            s.AddCallback("Space", "getLocalPlayerNumber", AsyncCallbackWithResult(GetLocalPlayerNumber));
            s.AddCallback("Space", "getHealth", AsyncCallbackWithResult(GetHealth));
            s.AddCallback("Space", "getMaxHealth", AsyncCallbackWithResult(GetMaxHealth));
            s.AddCallback("Space", "getEnergy", AsyncCallbackWithResult(GetEnergy));
            s.AddCallback("Space", "getMaxEnergy", AsyncCallbackWithResult(GetMaxEnergy));

            s.AddCallback("Space", "getPositionX", AsyncCallbackWithResult(GetPositionX));
            s.AddCallback("Space", "getPositionY", AsyncCallbackWithResult(GetPositionY));
            s.AddCallback("Space", "getCellX", AsyncCallbackWithResult(GetCellX));
            s.AddCallback("Space", "getCellY", AsyncCallbackWithResult(GetCellY));
            s.AddCallback("Space", "getSpeed", AsyncCallbackWithResult(GetSpeed));
            s.AddCallback("Space", "getMaxSpeed", AsyncCallbackWithResult(GetMaxSpeed));
            s.AddCallback("Space", "getMaxAcceleration", AsyncCallbackWithResult(GetMaxAcceleration));
            s.AddCallback("Space", "getMass", AsyncCallbackWithResult(GetMass));
        }

        #endregion

        #region Javascript API

        private static JavascriptMethodEventHandler AsyncCallbackWithResult(Func<JSValue[], JSValue> f, int numArgs = 0)
        {
            return (sender, args) =>
            {
                // The webview that triggered the callback.
                var webView = (WebView)sender;

                // Make sure we have the right number of args.
                if (args.Arguments.Length != 1 + numArgs)
                {
                    Logger.Warn("Wrong number of arguments passed to callback '{0}'.", args.MethodName);
                    return;
                }

                // Make sure the callback is a string.
                var callback = args.Arguments[args.Arguments.Length - 1];
                if (!callback.IsString)
                {
                    Logger.Warn("Bad JS callback for '{0}', callback name is not a string.", args.MethodName);
                    return;
                }

                // Run actual handler
                try
                {
                    var result = f(new ArraySegment<JSValue>(args.Arguments, 0, numArgs).Array);
                    webView.ExecuteJavascript(callback + "(JSON.parse(" + ToJSON(result) + "))");
                }
                catch (Exception ex)
                {
                    Logger.WarnException("Error in JavaScript callback '" + args.MethodName + "'.", ex);
                }

                //// Make sure the callback is an object.
                //if (!args.Arguments[args.Arguments.Length - 1].IsObject)
                //{
                //    Logger.Warn("Bad JS callback for '{0}', callback does not appear to be a function.", args.MethodName);
                //    return;
                //}

                //// Get callback and make sure it's a function (or pretends it's one, at least).
                //var callback = (JSObject)args.Arguments[args.Arguments.Length - 1];
                //if (!callback.HasMethod("call"))
                //{
                //    Logger.Warn("Bad JS callback for '{0}', callback does not appear to be a function.", args.MethodName);
                //    return;
                //}

                //// Run actual handler
                //try
                //{
                //    var result = f(new ArraySegment<JSValue>(args.Arguments, 0, numArgs).Array);
                //    callback.Invoke("call", JSValue.Null, result);
                //}
                //catch (Exception ex)
                //{
                //    Logger.WarnException("Error in JavaScript callback '" + args.MethodName + "'.", ex);
                //}
            };
        }

        private static string ToJSON(JSValue value)
        {
            var s = new StringBuilder();
            Stringify(value, s);
            return s.ToString();
        }

        private static void Stringify(JSValue value, StringBuilder s)
        {
            if (value.IsObject)
            {
                var obj = (JSObject)value;
                s.Append('{');
                var entries = obj.GetPropertyNames();
                if (entries.Length > 0)
                {
                    Stringify(entries[0], s);
                    s.Append(':');
                    Stringify(obj[entries[0]], s);
                    for (var i = 1; i < entries.Length; i++)
                    {
                        s.Append(',');
                        Stringify(entries[1], s);
                        s.Append(':');
                        Stringify(obj[entries[1]], s);
                    }
                }
                s.Append('}');
            }
            else if (value.IsArray)
            {
                s.Append('[');
                var entries = (JSValue[])value;
                if (entries.Length > 0)
                {
                    Stringify(entries[0], s);
                    for (var i = 1; i < entries.Length; i++)
                    {
                        s.Append(',');
                        Stringify(entries[i], s);
                    }
                }
                s.Append(']');
            }
            else if (value.IsString)
            {
                s.Append('"');
                var sval = (string)value;
                for (var i = 0; i < sval.Length; i++)
                {
                    switch (sval[i])
                    {
                        case '\\':
                        case '"':
                            s.Append('\\');
                            s.Append(sval[i]);
                            break;
                        case '\b':
                            s.Append('\\');
                            s.Append('b');
                            break;
                        case '\f':
                            s.Append('\\');
                            s.Append('f');
                            break;
                        case '\n':
                            s.Append('\\');
                            s.Append('n');
                            break;
                        case '\r':
                            s.Append('\\');
                            s.Append('r');
                            break;
                        case '\t':
                            s.Append('\\');
                            s.Append('t');
                            break;
                        default:
                            s.Append(sval[i]);
                            break;
                    }
                }
                s.Append('"');
            }
            else if (value.IsInteger)
            {
                s.Append((int)value);
            }
            else if (value.IsDouble)
            {
                s.Append((double)value);
            }
            else if (value.IsBoolean)
            {
                s.Append((bool)value ? "true" : "false");
            }
            else if (value.IsNull)
            {
                s.Append("null");
            }
            else if (value.IsUndefined)
            {
                s.Append("\"undefined\"");
            }
        }

        #region Localization

        /// <summary>
        /// Gets the GUI string for the specified id.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The name of the GUI string to get.</param>
        private static void GetGuiString(object sender, JavascriptMethodEventArgs args)
        {
            if (args.Arguments.Length != 1 || !args.Arguments[0].IsString)
            {
                Logger.Warn("Invalid call to 'L', must specify one string argument.");
                args.Result = JSValue.Undefined; return;
            }
            var s = GuiStrings.ResourceManager.GetString(args.Arguments[0].ToString());
            args.Result = s ?? ("!!" + args.Arguments[0] + "!!");
        }

        #endregion

        #region Main Menu

        /// <summary>
        /// Hosts a new game, launching a local server and connecting to it.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private void StartLocalGame(object sender, JavascriptMethodEventArgs args)
        {
            _game.RestartServer(true);
            _game.RestartClient(true);
        }

        /// <summary>
        /// Hosts a new game, launching a local server and connecting to it.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private void HostGame(object sender, JavascriptMethodEventArgs args)
        {
            _game.RestartServer();
            /*
            _game.RestartClient(true);
            /*/
            _game.RestartClient();
            _game.Client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777),
                                                 Settings.Instance.PlayerName, Settings.Instance.CurrentProfile);
            //*/
        }

        /// <summary>
        /// Joins a game running on the specified server.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private void JoinGame(object sender, JavascriptMethodEventArgs args)
        {
            if (args.Arguments.Length != 1 || !args.Arguments[0].IsString)
            {
                Logger.Warn("Invalid call to 'Space.joinGame', must specify one string argument.");
                return;
            }
            _game.DisposeServer();
            _game.RestartClient();
            _game.Client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(args.Arguments[0].ToString()), 7777),
                                                 Settings.Instance.PlayerName, Settings.Instance.CurrentProfile);
        }

        /// <summary>
        /// Leaves the game we are currently in, if any.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private void LeaveGame(object sender, JavascriptMethodEventArgs args)
        {
            _game.DisposeControllers();
        }

        /// <summary>
        /// Searches for games in the local network.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private void SearchGames(object sender, JavascriptMethodEventArgs args)
        {
            _game.Client.Controller.Session.Search();
        }

        #endregion

        #region Settings

        /// <summary>
        /// The name of settings exposed to the scripting environment.
        /// </summary>
        private static readonly Dictionary<string, Tuple<string, ScriptAccessAttribute>> SettingInfo = InitSettings();

        /// <summary>
        /// Inits the setting names dictionary.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, Tuple<string, ScriptAccessAttribute>> InitSettings()
        {
            var settings = new Dictionary<string, Tuple<string, ScriptAccessAttribute>>();
            var info = typeof(Settings);
            foreach (var field in info.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!field.IsDefined(typeof(ScriptAccessAttribute), false))
                {
                    continue;
                }
                var attributes = field.GetCustomAttributes(typeof(ScriptAccessAttribute), false);
                if (attributes.Length > 0)
                {
                    var sa = (ScriptAccessAttribute)attributes[0];
                    settings.Add(sa.Name, Tuple.Create(field.Name, sa));
                }
            }
            return settings;
        }

        /// <summary>
        /// Gets the setting names available for scripting environment.
        /// </summary>
        /// <param name="args">The args.</param>
        private static JSValue GetSettingInfos(JSValue[] args)
        {
            var settings = new JSObject();
            foreach (var setting in SettingInfo)
            {
                var obj = new JSObject();
                obj["name"] = setting.Key;

                // Write meta info for this setting.
                var attribute = setting.Value.Item2;

                // Write options, if there are any.
                if (attribute.Options != null && attribute.Options.Length > 0)
                {
                    var options = new JSValue[attribute.Options.Length];
                    for (var i = 0; i < attribute.Options.Length; i++)
                    {
                        options[i] = ObjectToJSValue(attribute.Options[i]);
                    }
                    obj["options"] = options;
                }

                // Value ranges, if applicable.
                if (attribute.MinValue != null)
                {
                    obj["min"] = ObjectToJSValue(attribute.MinValue);
                }
                if (attribute.MaxValue != null)
                {
                    obj["max"] = ObjectToJSValue(attribute.MaxValue);
                }

                // Should we list the setting?
                obj["show"] = attribute.ShouldList;

                // Localized strings.
                obj["title"] = GuiStrings.ResourceManager.GetString(attribute.TitleLocalizationId) ?? ("!!" + attribute.TitleLocalizationId + "!!");
                var description = GuiStrings.ResourceManager.GetString(attribute.DescriptionLocalizationId);
                if (description != null)
                {
                    obj["description"] = description;
                }

                settings[setting.Key] = obj;
            }
            return settings;
        }

        /// <summary>
        /// Gets the current value of the setting with the specified name,
        /// which must be one of the array that can be read via GetSettingNames.
        /// </summary>
        /// <param name="args">The args.</param>
        private static JSValue GetSetting(JSValue[] args)
        {
            if (!args[0].IsString)
            {
                Logger.Warn("Invalid call to 'Space.getSetting', must specify one string argument.");
                return JSValue.Undefined;
            }
            Tuple<string, ScriptAccessAttribute> settingInfo;
            if (!SettingInfo.TryGetValue(args[0], out settingInfo))
            {
                Logger.Warn("Invalid call to 'Space.getSetting', unknown setting.");
                return JSValue.Undefined;
            }
            var info = typeof(Settings);
            var fieldInfo = info.GetField(settingInfo.Item1);
            return ObjectToJSValue(fieldInfo.GetValue(Settings.Instance));
        }

        /// <summary>
        /// Sets a new value for a setting.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        private static void SetSetting(object sender, JavascriptMethodEventArgs args)
        {
            if (args.Arguments.Length != 2 || !args.Arguments[0].IsString)
            {
                Logger.Warn("Invalid call to 'Space.setSetting', must specify two arguments, of which the first must be a string.");
                return;
            }

            Tuple<string, ScriptAccessAttribute> settingInfo;
            if (!SettingInfo.TryGetValue(args.Arguments[0].ToString(), out settingInfo))
            {
                Logger.Warn("Invalid call to 'Space.setSetting', unknown setting.");
                return;
            }

            var info = typeof(Settings);
            var fieldInfo = info.GetField(settingInfo.Item1);
            try
            {
                fieldInfo.SetValue(Settings.Instance, JSValueToObject(fieldInfo.FieldType, args.Arguments[1]));
            }
            catch (Exception)
            {
                Logger.Warn("Invalid call to 'Space.setSetting', invalid value for this setting.");
            }
        }

        /// <summary>
        /// Gets a list of all commands that can be handled in the GUI.
        /// </summary>
        /// <param name="args">The args.</param>
        private static JSValue GetGameCommands(JSValue[] args)
        {
            return Enum.GetNames(typeof(GameCommand)).Select(name => new JSValue(name)).ToArray();
        }

        #region Value conversion

        /// <summary>
        /// Method that converts a C# value to a JSValue.
        /// </summary>
        /// <param name="o">The object to convert.</param>
        /// <returns>The JSValue wrapper for that type.</returns>
        private delegate JSValue ToJSValue(object o);

        /// <summary>
        /// Method that converts a JSValue to a C# value.
        /// </summary>
        /// <param name="v">The value to convert.</param>
        /// <returns>The C# value for that wrapper.</returns>
        private delegate object FromJSValue(JSValue v);

        /// <summary>
        /// Simple type conversions, mapping type to converter, object->JSValue.
        /// </summary>
        private static readonly Dictionary<Type, ToJSValue> ToJSValueConverters =
            new Dictionary<Type, ToJSValue>
            {
                {typeof(string), o => new JSValue((string)o)},
                {typeof(int), o => new JSValue((int)o)},
                {typeof(short), o => new JSValue((short)o)},
                {typeof(ushort), o => new JSValue((ushort)o)},
                {typeof(byte), o => new JSValue((byte)o)},
                {typeof(float), o => new JSValue((float)o)},
                {typeof(double), o => new JSValue((double)o)},
                {typeof(bool), o => new JSValue((bool)o)},
                {typeof(Point), o => ((Point)o).X + "x" + ((Point)o).Y},
            };

        /// <summary>
        /// Simple type conversions, mapping type to converter, JSValue->object.
        /// </summary>
        private static readonly Dictionary<Type, FromJSValue> FromJSValueConverters =
            new Dictionary<Type, FromJSValue>
            {
                {typeof(string), v => v.ToString()},
                {typeof(int), v => (int)v},
                {typeof(short), v => (short)v},
                {typeof(ushort), v => (ushort)v},
                {typeof(byte), v => (byte)v},
                {typeof(float), v => (float)v},
                {typeof(double), v => (double)v},
                {typeof(bool), v => (bool)v},
                {typeof(Point), v => {
                    var a = v.ToString().Split('x');
                    return new Point(int.Parse(a[0]), int.Parse(a[1]));
                }},
            };

        /// <summary>
        /// Converts an object to a JSValue, based on the object's type.
        /// </summary>
        /// <param name="obj">The object to wrap.</param>
        /// <returns>The JSValue wrapper, or undefined if there is no converter.</returns>
        private static JSValue ObjectToJSValue(object obj)
        {
            // Null is a special case.
            if (obj == null)
            {
                return JSValue.Null;
            }

            // Check if we have a simple converter for this type.
            var type = obj.GetType();
            if (ToJSValueConverters.ContainsKey(type))
            {
                return ToJSValueConverters[type](obj);
            }

            // Custom conversions.
            if (type.GetInterfaces().Contains(typeof(IDictionary)))
            {
                var result = new JSObject();
                var dict = (IDictionary)obj;
                foreach (var key in dict.Keys)
                {
                    result[key.ToString()] = new JSValue(dict[key].ToString());
                }
                return result;
            }

            // Cannot handle this type.
            Logger.Warn("Cannot convert C# object of type '" + type.Name + "' to JSValue.");
            return JSValue.Undefined;
        }

        private static object JSValueToObject(Type fieldType, JSValue value)
        {
            // We have no equivalent for undefined.
            if (value.IsUndefined)
            {
                throw new ArgumentException("Invalid value, must not be 'undefined'.");
            }

            // Null is a special case.
            if (value.IsNull)
            {
                return null;
            }

            // Check for simple converter.
            if (FromJSValueConverters.ContainsKey(fieldType))
            {
                return FromJSValueConverters[fieldType](value);
            }

            // Custom conversions.
            if (fieldType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                // TODO
            }

            // Cannot handle this type.
            throw new ArgumentException("Unhandled value type.");
        }

        #endregion

        #endregion

        #region Ingame

        private JSValue GetNumPlayers(JSValue[] args)
        {
            var session = _game.Client.Controller.Session;
            return session.ConnectionState != ClientState.Connected ? JSValue.Undefined : session.NumPlayers;
        }

        private JSValue GetMaxPlayers(JSValue[] args)
        {
            var session = _game.Client.Controller.Session;
            return session.ConnectionState != ClientState.Connected ? JSValue.Undefined : session.MaxPlayers;
        }

        private JSValue GetLocalPlayerNumber(JSValue[] args)
        {
            var session = _game.Client.Controller.Session;
            return session.ConnectionState != ClientState.Connected ? JSValue.Undefined : session.LocalPlayer.Number;
        }

        private JSValue GetHealth(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : info.Health;
        }

        private JSValue GetMaxHealth(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : info.MaxHealth;
        }

        private JSValue GetEnergy(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : info.Energy;
        }

        private JSValue GetMaxEnergy(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : info.MaxEnergy;
        }

        private JSValue GetPositionX(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : (JSValue)info.Position.X.ToString();
        }

        private JSValue GetPositionY(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : (JSValue)info.Position.Y.ToString();
        }

        private JSValue GetCellX(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.Undefined;
            }
            else
            {
                var pos = info.Position.X;
                var cellX = ((int)pos) >> CellSystem.CellSizeShiftAmount;
                return cellX;
            }
        }

        private JSValue GetCellY(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.Undefined;
            }
            else
            {
                var pos = info.Position.Y;
                var cell = ((int)pos) >> CellSystem.CellSizeShiftAmount;
                return cell;
            }
        }

        private JSValue GetSpeed(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : Math.Round(info.Speed, 1);
        }

        private JSValue GetMaxSpeed(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : Math.Round(info.MaxSpeed, 1);
        }

        private JSValue GetMaxAcceleration(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : Math.Round(info.MaxAcceleration, 1);
        }

        private JSValue GetMass(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            return info == null ? JSValue.Undefined : info.Mass;
        }

        #endregion

        #endregion

    }
}
