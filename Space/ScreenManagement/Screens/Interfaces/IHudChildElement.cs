using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Space.ScreenManagement.Screens.Interfaces
{
    interface IHudChildElement
    {

        /// <summary>
        /// The current top-left position of the child element.
        /// </summary>
        Point Position { get; set; }
    }
}
