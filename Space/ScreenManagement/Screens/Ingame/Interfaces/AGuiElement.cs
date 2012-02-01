using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Control;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Space.ScreenManagement.Screens.Ingame.Interfaces
{
    public abstract class AGuiElement
    {

        protected readonly GameClient _client;

        public bool Enabled { get; set; }
        public bool Visible { get; set; }

        public AGuiElement(GameClient client)
        {
            this._client = client;
            Enabled = false;
            Visible = false;
        }

        public abstract void LoadContent(SpriteBatch spriteBatch, ContentManager content);
        public abstract void Draw();
        public virtual void Update()
        {
        }

        public virtual bool HandleKeyPressed(Keys key)
        {
            return false;
        }

        public virtual bool HandleKeyReleased(Keys key)
        {
            return false;
        }

        public virtual bool HandleMousePressed(MouseButtons buttons)
        {
            return false;
        }

        public virtual bool HandleMouseReleased(MouseButtons buttons)
        {
            return false;
        }

        public virtual bool HandleMouseMoved(float x, float y)
        {
            return false;
        }

        public virtual bool HandleGamePadPressed(Buttons buttons)
        {
            return false;
        }

        public virtual bool HandleGamePadReleased(Buttons buttons)
        {
            return false;
        }

    }
}
