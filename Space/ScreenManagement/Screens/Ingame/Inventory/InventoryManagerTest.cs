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

        String _img0 = "Textures/Icons/Buffs/default";
        String _img1 = "Textures/Icons/Buffs/stabilisator";
        String _img2 = null;
        String _img3 = null;

        public InventoryManagerTest()
        {
        }

        public String GetImagePath(int id)
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

        public void SetImage(String image, int id)
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
