using System;
using System.Net;
using Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space;
using Space.Control;
using Space.Model;

namespace GameStateManagement
{

    class ConnectScreen : MenuScreen
    {
        #region Fields

        EditableMenueEntry connect;

        public GameClient Client { get; private set; }
        #endregion
        public ConnectScreen(GameClient client)
            : base(Strings.connect)
        {
            Client = client;
            client.Session.JoinResponse += LoginSucces;
            connect = new EditableMenueEntry(String.Empty);
            MenuEntry back = new MenuEntry("Back");
            connect.SetActive(true);

            connect.Selected += ConnectEntrySelected;

            back.Selected += OnCancel;

            MenuEntries.Add(connect);
            MenuEntries.Add(back);

        }

        public override void LoadContent()
        {
            var keyboard = (IKeyboardInputManager)ScreenManager.Game.Services.GetService(typeof(IKeyboardInputManager));
            if (keyboard != null)
            {
                keyboard.Pressed += ((EditableMenueEntry)MenuEntries[0]).HandleKeyPressed;
            }
        }
        //Called if the Connect Entry is selected
        void ConnectEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            if (connect.Editable)
            {
                PlayerInfo info = new PlayerInfo();
                info.ShipType = "Sparrow";
                connect.locked = true;
                Client.Session.Join(new IPEndPoint(IPAddress.Parse(connect.Text), 50100), Settings.Instance.PlayerName, info);
            }
            else
            {
                connect.Editable = true;
            }
            
        }
        //Called if the login was handeled
        private void LoginSucces(object sender, EventArgs e)
        {
            ((EditableMenueEntry)MenuEntries[0]).locked = false;
            LoadingScreen.Load(ScreenManager, true,
                                new GameplayScreen(Client));
        }

        public override void Draw(GameTime gameTime)
        {

            base.Draw(gameTime);
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            spriteBatch.Begin();

            spriteBatch.End();
        }
    }
}
