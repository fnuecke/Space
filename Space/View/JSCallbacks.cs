using System;
using System.Net;
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

        private void HandleClientInitialized(object sender, ClientInitializedEventArgs e)
        {
            var session = e.Client.Controller.Session;
            session.GameInfoReceived += SessionOnGameInfoReceived;
            session.JoinResponse += SessionOnJoinResponse;
            session.Disconnected += SessionOnDisconnected;
            session.PlayerJoined += SessionOnPlayerJoined;
            session.PlayerLeft += SessionOnPlayerLeft;
        }

        private void SessionOnGameInfoReceived(object sender, GameInfoReceivedEventArgs e)
        {
            var args = new JSObject();
            args["host"] = new JSValue(e.Host.Address.ToString());
            args["numPlayers"] = new JSValue(e.NumPlayers);
            args["maxPlayers"] = new JSValue(e.MaxPlayers);
            args["data"] = JSValue.CreateNull(); // TODO think of what else we might want to send
            _game.ScreenManager.RaiseEvent("Space", "onGameInfoReceived", new[] {new JSValue(args)});
        }

        private void SessionOnJoinResponse(object sender, JoinResponseEventArgs e)
        {
            _game.ScreenManager.RaiseEvent("Space", "onConnected", new JSValue[0]);
        }

        private void SessionOnDisconnected(object sender, EventArgs e)
        {
            _game.ScreenManager.RaiseEvent("Space", "onDisconnected", new JSValue[0]);
        }

        private void SessionOnPlayerJoined(object sender, PlayerEventArgs e)
        {
            var args = new JSObject();
            args["number"] = new JSValue(e.Player.Number);
            args["name"] = new JSValue(e.Player.Name);
            _game.ScreenManager.RaiseEvent("Space", "onPlayerJoined", new[] { new JSValue(args) });
        }

        private void SessionOnPlayerLeft(object sender, PlayerEventArgs e)
        {
            var args = new JSObject();
            args["number"] = new JSValue(e.Player.Number);
            args["name"] = new JSValue(e.Player.Name);
            _game.ScreenManager.RaiseEvent("Space", "onPlayerLeft", new[] { new JSValue(args) });
        }

        #endregion

        #region Setup

        private void SetupJavaScriptApi()
        {
            var s = _game.ScreenManager;
            s.AddCallback("Space", "hostGame", HostGame);
            s.AddCallback("Space", "joinGame", JoinGame);
            s.AddCallback("Space", "leaveGame", LeaveGame);
            s.AddCallback("Space", "searchGames", SearchGames);

            s.AddEvent("Space", "onGameInfoReceived");
            s.AddEvent("Space", "onConnected");
            s.AddEvent("Space", "onDisconnected");
            s.AddEvent("Space", "onPlayerJoined");
            s.AddEvent("Space", "onPlayerLeft");

            s.AddCallbackWithReturnValue("Space", "getNumPlayers", GetNumPlayers);
            s.AddCallbackWithReturnValue("Space", "getMaxPlayers", GetMaxPlayers);
            s.AddCallbackWithReturnValue("Space", "getLocalPlayerNumber", GetLocalPlayerNumber);
            s.AddCallbackWithReturnValue("Space", "getHealth", GetHealth);
            s.AddCallbackWithReturnValue("Space", "getMaxHealth", GetMaxHealth);
            s.AddCallbackWithReturnValue("Space", "getEnergy", GetEnergy);
            s.AddCallbackWithReturnValue("Space", "getMaxEnergy", GetMaxEnergy);
        }

        #endregion

        #region Javascript API

        #region Main Menu
        
        private void HostGame(JSValue[] args)
        {
            _game.RestartServer();
            _game.RestartClient(true);
        }

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

        private void LeaveGame(JSValue[] args)
        {
            _game.DisposeControllers();
        }

        private void SearchGames(JSValue[] args)
        {
            _game.Client.Controller.Session.Search();
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

        #endregion

        #endregion

    }
}
