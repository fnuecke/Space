using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Awesomium.Core;
using Engine.ComponentSystem.Common.Systems;
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
            args["host"] = new JSValue(e.Host.Address.ToString());
            args["numPlayers"] = new JSValue(e.NumPlayers);
            args["maxPlayers"] = new JSValue(e.MaxPlayers);
            args["data"] = JSValue.CreateNull(); // TODO think of what else we might want to send
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
            args["number"] = new JSValue(e.Player.Number);
            args["name"] = new JSValue(e.Player.Name);
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
            args["number"] = new JSValue(e.Player.Number);
            args["name"] = new JSValue(e.Player.Name);
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
            s.AddCallback("Space", "hostGame", HostGame);
            s.AddCallback("Space", "joinGame", JoinGame);
            s.AddCallback("Space", "leaveGame", LeaveGame);
            s.AddCallback("Space", "searchGames", SearchGames);

            // Settings related callbacks.
            s.AddCallbackWithReturnValue("Space", "getSettingInfos", GetSettingInfos);
            s.AddCallbackWithReturnValue("Space", "getSetting", GetSetting);
            s.AddCallback("Space", "setSetting", SetSetting);
            s.AddCallbackWithReturnValue("Space", "getGameCommands", GetGameCommands);

            // Ingame information.
            s.AddCallbackWithReturnValue("Space", "getNumPlayers", GetNumPlayers);
            s.AddCallbackWithReturnValue("Space", "getMaxPlayers", GetMaxPlayers);
            s.AddCallbackWithReturnValue("Space", "getLocalPlayerNumber", GetLocalPlayerNumber);
            s.AddCallbackWithReturnValue("Space", "getHealth", GetHealth);
            s.AddCallbackWithReturnValue("Space", "getMaxHealth", GetMaxHealth);
            s.AddCallbackWithReturnValue("Space", "getEnergy", GetEnergy);
            s.AddCallbackWithReturnValue("Space", "getMaxEnergy", GetMaxEnergy);

            s.AddCallbackWithReturnValue("Space", "getFps", GetFps);
            s.AddCallbackWithReturnValue("Space", "getXCoordinate", GetXCoordinate);
            s.AddCallbackWithReturnValue("Space", "getYCoordinate", GetYCoordinate);
            s.AddCallbackWithReturnValue("Space", "getXCell", GetXCell);
            s.AddCallbackWithReturnValue("Space", "getYCell", GetYCell);
            s.AddCallbackWithReturnValue("Space", "getUpdateLoad", GetUpdateLoad);
            s.AddCallbackWithReturnValue("Space", "getUpdateSpeed", GetUpdateSpeed);
            s.AddCallbackWithReturnValue("Space", "getIndexes", GetIndexes);
            s.AddCallbackWithReturnValue("Space", "getTotalEntries", GetTotalEntries);
            s.AddCallbackWithReturnValue("Space", "getQueries", GetQueries);
            s.AddCallbackWithReturnValue("Space", "getSpeed", GetSpeed);
            s.AddCallbackWithReturnValue("Space", "getMaxSpeed", GetMaxSpeed);
            s.AddCallbackWithReturnValue("Space", "getMaxAcceleration", GetMaxAcceleration);
            s.AddCallbackWithReturnValue("Space", "getMass", GetMass);
        }

        #endregion

        #region Javascript API

        #region Localization

        /// <summary>
        /// Gets the GUI string for the specified id.
        /// </summary>
        /// <param name="args">The name of the GUI string to get.</param>
        /// <returns>The value for that string for the current language setting.</returns>
        private static JSValue GetGuiString(JSValue[] args)
        {
            if (args.Length != 1 || !args[0].IsString)
            {
                Logger.Warn("Invalid call to 'L', must specify one string argument.");
                return JSValue.CreateUndefined();
            }
            var s = GuiStrings.ResourceManager.GetString(args[0].ToString());
            return new JSValue(s ?? ("!!" + args[0] + "!!"));
        }

        #endregion

        #region Main Menu

        /// <summary>
        /// Hosts a new game, launching a local server and connecting to it.
        /// </summary>
        /// <param name="args">The args.</param>
        private void HostGame(JSValue[] args)
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
        /// <param name="args">The args.</param>
        private void JoinGame(JSValue[] args)
        {
            if (args.Length != 1 || !args[0].IsString)
            {
                Logger.Warn("Invalid call to 'Space.joinGame', must specify one string argument.");
                return;
            }
            _game.DisposeServer();
            _game.RestartClient();
            _game.Client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(args[0].ToString()), 7777),
                                                 Settings.Instance.PlayerName, Settings.Instance.CurrentProfile);
        }

        /// <summary>
        /// Leaves the game we are currently in, if any.
        /// </summary>
        /// <param name="args">The args.</param>
        private void LeaveGame(JSValue[] args)
        {
            _game.DisposeControllers();
        }

        /// <summary>
        /// Searches for games in the local network.
        /// </summary>
        /// <param name="args">The args.</param>
        private void SearchGames(JSValue[] args)
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
        /// <returns>An array with available setting names.</returns>
        private static JSValue GetSettingInfos(JSValue[] args)
        {
            var settings = new JSObject();
            foreach (var setting in SettingInfo)
            {
                var obj = new JSObject();
                obj["name"] = new JSValue(setting.Key);

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
                    obj["options"] = new JSValue(options);
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
                obj["show"] = new JSValue(attribute.ShouldList);

                // Localized strings.
                obj["title"] = new JSValue(GuiStrings.ResourceManager.
                    GetString(attribute.TitleLocalizationId) ?? ("!!" + attribute.TitleLocalizationId + "!!"));
                var description = GuiStrings.ResourceManager.GetString(attribute.DescriptionLocalizationId);
                if (description != null)
                {
                    obj["description"] = new JSValue(description);
                }

                settings[setting.Key] = new JSValue(obj);
            }
            return new JSValue(settings);
        }

        /// <summary>
        /// Gets the current value of the setting with the specified name,
        /// which must be one of the array that can be read via GetSettingNames.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>The current value for that setting.</returns>
        private static JSValue GetSetting(JSValue[] args)
        {
            if (args.Length != 1 || !args[0].IsString)
            {
                Logger.Warn("Invalid call to 'Space.getSetting', must specify one string argument.");
                return JSValue.CreateUndefined();
            }
            Tuple<string, ScriptAccessAttribute> settingInfo;
            if (!SettingInfo.TryGetValue(args[0].ToString(), out settingInfo))
            {
                Logger.Warn("Invalid call to 'Space.getSetting', unknown setting.");
                return JSValue.CreateUndefined();
            }
            var info = typeof(Settings);
            var fieldInfo = info.GetField(settingInfo.Item1);
            return ObjectToJSValue(fieldInfo.GetValue(Settings.Instance));
        }

        /// <summary>
        /// Sets a new value for a setting.
        /// </summary>
        /// <param name="args">The args.</param>
        private static void SetSetting(JSValue[] args)
        {
            if (args.Length != 2 || !args[0].IsString)
            {
                Logger.Warn("Invalid call to 'Space.setSetting', must specify two arguments, of which the first must be a string.");
                return;
            }

            Tuple<string, ScriptAccessAttribute> settingInfo;
            if (!SettingInfo.TryGetValue(args[0].ToString(), out settingInfo))
            {
                Logger.Warn("Invalid call to 'Space.setSetting', unknown setting.");
                return;
            }

            var info = typeof(Settings);
            var fieldInfo = info.GetField(settingInfo.Item1);
            try
            {
                fieldInfo.SetValue(Settings.Instance, JSValueToObject(fieldInfo.FieldType, args[1]));
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
        /// <returns></returns>
        private static JSValue GetGameCommands(JSValue[] args)
        {
            return new JSValue(Enum.GetNames(typeof(GameCommand)).Select(name => new JSValue(name)).ToArray());
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
                {typeof(Point), o => new JSValue(((Point)o).X + "x" + ((Point)o).Y)},
            };

        /// <summary>
        /// Simple type conversions, mapping type to converter, JSValue->object.
        /// </summary>
        private static readonly Dictionary<Type, FromJSValue> FromJSValueConverters =
            new Dictionary<Type, FromJSValue>
            {
                {typeof(string), v => v.ToString()},
                {typeof(int), v => v.ToInteger()},
                {typeof(short), v => (short)v.ToInteger()},
                {typeof(ushort), v => (ushort)v.ToInteger()},
                {typeof(byte), v => (byte)v.ToInteger()},
                {typeof(float), v => (float)v.ToDouble()},
                {typeof(double), v => v.ToDouble()},
                {typeof(bool), v => v.ToBoolean()},
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
                return JSValue.CreateNull();
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
                return new JSValue(result);
            }

            // Cannot handle this type.
            Logger.Warn("Cannot convert C# object of type '" + type.Name + "' to JSValue.");
            return JSValue.CreateUndefined();
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
            if (session.ConnectionState != ClientState.Connected)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(session.NumPlayers);
        }

        private JSValue GetMaxPlayers(JSValue[] args)
        {
            var session = _game.Client.Controller.Session;
            if (session.ConnectionState != ClientState.Connected)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(session.MaxPlayers);
        }

        private JSValue GetLocalPlayerNumber(JSValue[] args)
        {
            var session = _game.Client.Controller.Session;
            if (session.ConnectionState != ClientState.Connected)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(session.LocalPlayer.Number);
        }

        private JSValue GetHealth(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(info.Health);
        }

        private JSValue GetMaxHealth(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(info.MaxHealth);
        }

        private JSValue GetEnergy(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(info.Energy);
        }

        private JSValue GetMaxEnergy(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(info.MaxEnergy);
        }

        private JSValue GetFps(JSValue[] args)
        {
            // TODO:
            return new JSValue("XX.XX");
        }

        private JSValue GetXCoordinate(JSValue[] args)
        {
            
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            
            return new JSValue(Math.Round(info.Position.X,2));
        }

        private JSValue GetYCoordinate(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }

            return new JSValue(Math.Round(info.Position.Y,2));
        }

        private JSValue GetXCell(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            var pos = info.Position.X;
            var cellX = ((int)pos) >> CellSystem.CellSizeShiftAmount;
            return new JSValue(cellX);
        }

        private JSValue GetYCell(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }

            var pos = info.Position.Y;
            var cell = ((int)pos) >> CellSystem.CellSizeShiftAmount;
            return new JSValue(cell);
        }

        private JSValue GetUpdateLoad(JSValue[] args)
        {
            
            return new JSValue(Math.Round(_game.Client.Controller.CurrentLoad,2));
        }

        private JSValue GetUpdateSpeed(JSValue[] args)
        {
            return new JSValue(_game.Client.Controller.ActualSpeed);
        }

        private JSValue GetIndexes(JSValue[] args)
        {
            var manager = _game.Client.Controller.Simulation.Manager;
            var index = (IndexSystem)manager.GetSystem(IndexSystem.TypeId);
            if (index != null)
            {
                
               
                return new JSValue(index.NumIndexes);
            }
            // TODO: Flo schau mal ob das so past da oben und unten mit den indexen usw da ich was rausgenommen hab aber irgendwie das selbe angezeigt wird
            //FN: i don't care :P abgesehen davon dass ich das ned fuer sinnvoll halt das ueber awe anzuzeigen
            return JSValue.CreateUndefined();
        }

        private JSValue GetTotalEntries(JSValue[] args)
        {
            var manager = _game.Client.Controller.Simulation.Manager;
            var index = (IndexSystem)manager.GetSystem(IndexSystem.TypeId);
            if (index != null)
            {

                
                return new JSValue(index.Count);
            }
            // TODO:
            return JSValue.CreateUndefined();
        }

        private JSValue GetQueries(JSValue[] args)
        {
            var manager = _game.Client.Controller.Simulation.Manager;
            var index = (IndexSystem)manager.GetSystem(IndexSystem.TypeId);
            if (index != null)
            {

                //, , );
                // \o\  \o/  /o/ ... /o\
                return new JSValue(index.NumQueriesSinceLastUpdate);
            }
            // TODO:
            return JSValue.CreateUndefined();
        }

        private JSValue GetSpeed(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(Math.Round(info.Speed,1));
        }

        private JSValue GetMaxSpeed(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(Math.Round(info.MaxSpeed,1));
        }

        private JSValue GetMaxAcceleration(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(Math.Round(info.MaxAcceleration,1));
        }

        private JSValue GetMass(JSValue[] args)
        {
            var info = _game.Client.GetPlayerShipInfo();
            if (info == null)
            {
                return JSValue.CreateUndefined();
            }
            return new JSValue(info.Mass);
        }

        #endregion

        #endregion

    }
}
