using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Util
{
    public interface IInformation
    {

        String[] getDisplayText();

         Color getDisplayColor();
        bool shallDraw();
    }
}
