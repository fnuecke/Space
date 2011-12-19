using System;
using System.Net;
using Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space;
using Space.Control;
using Space.Simulation;
using SpaceData;

namespace GameStateManagement
{

    class ConnectScreen : MenuScreen
    {
        #region Fields

        EditableMenueEntry connect;

        public GameClient Client { get; private set; }
        #endregion
        public ConnectScreen(GameClient client)
            : base(Strings.join)
        {
            Client = client;
            client.Session.JoinResponse += LoginSucces;
            client.Session.Disconnected += LoginFailed;
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
            PlayerInfo info = new PlayerInfo();
            if (connect.Editable)
            {
                
                info.Ship = this.ScreenManager.Game.Content.Load<ShipData[]>("Data/ships")[0];
                ((EditableMenueEntry)MenuEntries[0]).locked = true;
                Client.Session.Join(new IPEndPoint(IPAddress.Parse(MenuEntries[0].Text), 50100), Settings.Instance.PlayerName, info);
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
        //Called if the login was handeled
        private void LoginFailed(object sender, EventArgs e)
        {
            //tell that an error occured
            connect.locked = false;
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
