using System;
using System.Net;
using Engine.Session;
using Microsoft.Xna.Framework;
using Nuclex.Input.Devices;
using Space.Control;
using Space.ScreenManagement.Screens.Entries;
using Space.Session;
using Space.Util;

namespace Space.ScreenManagement.Screens
{
    sealed class ConnectScreen : MenuScreen
    {
        #region Fields

        private readonly GameClient _client;

        private EditableMenuEntry _address;

        private bool _connecting;

        #endregion

        public ConnectScreen(Game game)
            : base(MenuStrings.JoinGame)
        {
            _client = ((Spaaace)game).Client;

            _address = new EditableMenuEntry(MenuStrings.ServerAddress, (IKeyboard)game.Services.GetService(typeof(IKeyboard)),  Settings.Instance.LastServerAddress);
            var connect = new MenuEntry(MenuStrings.Connect);
            var back = new MenuEntry(MenuStrings.Back);

            connect.Activated += delegate(object sender, EventArgs e)
            {
                if (!_connecting)
                {
                    _connecting = true;
                    try
                    {
                        _client.Controller.Session.Join(new IPEndPoint(IPAddress.Parse(_address.InputText), 7777), Settings.Instance.PlayerName, (Profile)Settings.Instance.CurrentProfile);
                        Settings.Instance.LastServerAddress = _address.InputText;
                    }
                    catch (Exception)
                    {
                        ErrorText = MenuStrings.InvalidAddress;
                    }
                }
            };
            back.Activated += delegate(object sender, EventArgs e)
            {
                ExitScreen();
            };

            MenuEntries.Add(_address);
            MenuEntries.Add(connect);
            MenuEntries.Add(back);

            SetEscapeEntry(back);
        }

        public override void LoadContent()
        {
            _client.Controller.Session.JoinResponse += HandleLoginSuccess;
            _client.Controller.Session.Disconnected += HandleLoginFailure;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            _client.Controller.Session.JoinResponse -= HandleLoginSuccess;
            _client.Controller.Session.Disconnected -= HandleLoginFailure;
        }

        //Called if the login was handled
        private void HandleLoginSuccess(object sender, JoinResponseEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, true, new GameplayScreen(_client));
        }

        //Called if the login was handled
        private void HandleLoginFailure(object sender, EventArgs e)
        {
            _connecting = false;
            ErrorText = MenuStrings.ConnectionFailed;
        }
    }
}
