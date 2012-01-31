using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nuclex.Input;
using Microsoft.Xna.Framework.Input;

namespace Space.ScreenManagement.Screens.Interfaces
{
    /// <summary>
    /// An interface that guarantees an object having all necessary
    /// keyboard input handler methods. 
    /// </summary>
    public interface IKeyboardInput
    {
        /// <summary>
        /// Player pressed a key.
        /// </summary>
        void HandleKeyPressed(Keys key);

        /// <summary>
        /// Player released a key.
        /// </summary>
        void HandleKeyReleased(Keys key);
    }
}
