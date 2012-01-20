using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Space.ScreenManagement.Screens.Interfaces
{
    interface IHudParentElement
    {

        /// <summary>
        /// Updates the position of the element and of all (!) child elements.
        /// </summary>
        /// <param name="newPosition">The top-left new position of the parent element.</param>
        void SetPosition(Point newPosition);

        /// <summary>
        /// Returns the height of the element, that means: It calculates the height of all child elements.
        /// </summary>
        int GetHeight();
    }
}
