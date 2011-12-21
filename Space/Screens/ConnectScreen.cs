using System;
using System.Net;
using Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space;
using Space.Control;
using Space.Data;
using Space.Simulation;

namespace GameStateManagement
{

    class ConnectScreen : MenuScreen
    {
        #region Fields

        EditableMenueEntry connect;

        #endregion
        public ConnectScreen(GameClient client)
            : base(Strings.join)
        {
            
            client.Session.JoinResponse += LoginSucces;
            client.Session.Disconnected += LoginFailed;
            connect = new EditableMenueEntry(String.Empty);
            MenuEntry back = new MenuEntry(Strings.Back);
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
                info.Ship = this.ScreenManager.Game.Content.Load<ShipData[]>("Data/ships")[0];
                info.Weapon = this.ScreenManager.Game.Content.Load<WeaponData[]>("Data/weapons")[0];
                ((EditableMenueEntry)MenuEntries[0]).locked = true;
                try
                {
                    ScreenManager.Client.Session.Join(new IPEndPoint(IPAddress.Parse(MenuEntries[0].Text), 50100), Settings.Instance.PlayerName, info);
                }
                catch (Exception)
                {

                    ErrorText = Strings.InvalidHost;
                }
                
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
                                new GameplayScreen());
        }
        //Called if the login was handeled
        private void LoginFailed(object sender, EventArgs e)
        {
            ErrorText = Strings.ConnectionFailed;
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
