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
        String _img4 = null;
        String _img5 = null;
        String _img6 = null;
        String _img7 = null;
        String _img8 = null;
        String _img9 = null;
        String _img10 = null;
        String _img11 = null;
        String _img12 = null;
        String _img13 = null;

        public int Elements = 14;

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
                case 4: return _img4;
                case 5: return _img5;
                case 6: return _img6;
                case 7: return _img7;
                case 8: return _img8;
                case 9: return _img9;
                case 10: return _img10;
                case 11: return _img11;
                case 12: return _img12;
                case 13: return _img13;
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
                case 4: _img4 = image; break;
                case 5: _img5 = image; break;
                case 6: _img6 = image; break;
                case 7: _img7 = image; break;
                case 8: _img8 = image; break;
                case 9: _img9 = image; break;
                case 10: _img10 = image; break;
                case 11: _img11 = image; break;
                case 12: _img12 = image; break;
                case 13: _img13 = image; break;
            }
        }

    }
}
