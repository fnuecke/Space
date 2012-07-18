using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Awesomium.Core;
using Engine.Session;
using Space.Util;

namespace Space.View
{
    /// <summary>
    /// Utility class that defines methods to expose to JavaScript in the GUI.
    /// </summary>
    internal sealed class JSCallbacks
    {
        #region Fields
        
        /// <summary>
        /// The game we work for.
        /// </summary>
        private readonly Spaaace _game;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Registers all callbacks for the specified game.
        /// </summary>
        /// <param name="game"></param>
        public JSCallbacks(Spaaace game)
        {
            _game = game;
            _game.ClientInitialized += HandleClientInitialized;

            SetupJavaScriptApi();
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
            s.AddCallbackWithReturnValue("Space", "getSettingNames", GetSettingNames);
            s.AddCallbackWithReturnValue("Space", "getSetting", GetSetting);
            s.AddCallback("Space", "setSetting", SetSetting);
            s.AddCallbackWithReturnValue("Space", "getGuiCommands", GetGuiCommands);
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
            //*
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
            if (args.Length == 1 && args[0].IsString)
            {
                _game.DisposeServer();
                _game.RestartClient();
                _game.Client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(args[0].ToString()), 7777),
                                                     Settings.Instance.PlayerName, Settings.Instance.CurrentProfile);
            }
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
        private static readonly Dictionary<string, string> SettingNames = InitSettings();

        /// <summary>
        /// Inits the setting names dictionary.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> InitSettings()
        {
            var settings = new Dictionary<string, string>();
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
                    settings.Add(((ScriptAccessAttribute)attributes[0]).Name, field.Name);
                }
            }
            return settings;
        }

        /// <summary>
        /// Gets the setting names available for scripting environment.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>An array with available setting names.</returns>
        private static JSValue GetSettingNames(JSValue[] args)
        {
            var settings = new List<JSValue>();
            foreach (var setting in SettingNames.Keys)
            {
                settings.Add(new JSValue(setting));
            }
            return new JSValue(settings.ToArray());
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
                return JSValue.CreateUndefined();
            }
            string fieldName;
            if (!SettingNames.TryGetValue(args[0].ToString(), out fieldName))
            {
                return JSValue.CreateUndefined();
            }
            var info = typeof(Settings);
            var fieldInfo = info.GetField(fieldName);
            var fieldType = fieldInfo.FieldType;
            if (fieldType == typeof(string))
            {
                return new JSValue((string)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType == typeof(int))
            {
                return new JSValue((int)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType == typeof(short))
            {
                return new JSValue((short)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType == typeof(ushort))
            {
                return new JSValue((ushort)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType == typeof(byte))
            {
                return new JSValue((byte)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType == typeof(float))
            {
                return new JSValue((float)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType == typeof(double))
            {
                return new JSValue((double)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType == typeof(bool))
            {
                return new JSValue((bool)fieldInfo.GetValue(Settings.Instance));
            }
            else if (fieldType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                var result = new JSObject();
                var dict = (IDictionary)fieldInfo.GetValue(Settings.Instance);
                foreach (var key in dict.Keys)
                {
                    result[key.ToString()] = new JSValue(dict[key].ToString());
                }
                return new JSValue(result);
            }
            // Cannot handle this field type.
            return JSValue.CreateUndefined();
        }

        /// <summary>
        /// Sets a new value for a setting.
        /// </summary>
        /// <param name="args">The args.</param>
        private void SetSetting(JSValue[] args)
        {
            // TODO
        }

        /// <summary>
        /// Gets a list of all commands that can be handled in the GUI.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        private static JSValue GetGuiCommands(JSValue[] args)
        {
            return new JSValue(Enum.GetNames(typeof(Settings.GuiCommand)).Select(name => new JSValue(name)).ToArray());
        }

        /// <summary>
        /// Gets a list of all commands that can be handled in the GUI.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        private static JSValue GetGameCommands(JSValue[] args)
        {
            return new JSValue(Enum.GetNames(typeof(Settings.GameCommand)).Select(name => new JSValue(name)).ToArray());
        }

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
            // TODO:
            return new JSValue("XXXXX.XX");
        }

        private JSValue GetYCoordinate(JSValue[] args)
        {
            // TODO:
            return new JSValue("XXXXX.XX");
        }

        private JSValue GetXCell(JSValue[] args)
        {
            // TODO:
            return new JSValue("X");
        }

        private JSValue GetYCell(JSValue[] args)
        {
            // TODO:
            return new JSValue("X");
        }

        private JSValue GetUpdateLoad(JSValue[] args)
        {
            // TODO:
            return new JSValue("X.XX");
        }

        private JSValue GetUpdateSpeed(JSValue[] args)
        {
            // TODO:
            return new JSValue("X.XX");
        }

        private JSValue GetIndexes(JSValue[] args)
        {
            // TODO:
            return new JSValue("X");
        }

        private JSValue GetTotalEntries(JSValue[] args)
        {
            // TODO:
            return new JSValue("X");
        }

        private JSValue GetQueries(JSValue[] args)
        {
            // TODO:
            return new JSValue("X");
        }

        private JSValue GetSpeed(JSValue[] args)
        {
            // TODO:
            return new JSValue("X.XX");
        }

        private JSValue GetMaxSpeed(JSValue[] args)
        {
            // TODO:
            return new JSValue("X.XX");
        }

        private JSValue GetMaxAcceleration(JSValue[] args)
        {
            // TODO:
            return new JSValue("X.XX");
        }

        private JSValue GetMass(JSValue[] args)
        {
            // TODO:
            return new JSValue("X.XX");
        }

        #endregion

        #endregion

    }
}
