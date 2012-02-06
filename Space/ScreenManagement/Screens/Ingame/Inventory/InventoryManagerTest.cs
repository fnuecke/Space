using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;

namespace Space.ScreenManagement.Screens.Ingame.Hud
{
    class InventoryManagerTest
    {

        Texture2D _img0;
        Texture2D _img1;
        Texture2D _img2;
        Texture2D _img3;

        public InventoryManagerTest(GameClient client)
        {
            _img0 = client.Game.Content.Load<Texture2D>("Textures/Icons/Buffs/default");
            _img1 = client.Game.Content.Load<Texture2D>("Textures/Icons/Buffs/stabilisator");
            _img2 = null;
            _img3 = null;
        }

        public Texture2D GetImage(int id)
        {
            switch (id)
            {
                case 0: return _img0;
                case 1: return _img1;
                case 2: return _img2;
                case 3: return _img3;
            }
            return null;
        }

        public void SetImage(Texture2D image, int id)
        {
            switch (id)
            {
                case 0: _img0 = image; break;
                case 1: _img1 = image; break;
                case 2: _img2 = image; break;
                case 3: _img3 = image; break;
            }
        }

    }
}
