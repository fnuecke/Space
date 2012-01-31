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
    /// gamepad input handler methods. 
    /// </summary>
    public interface IGamepadInput
    {
        /// <summary>
        /// Handle game pad button presses.
        /// </summary>
        void HandleGamePadPressed(Buttons buttons);

        /// <summary>
        /// Handle game pad key releases.
        /// </summary>
        void HandleGamePadReleased(Buttons buttons);
    }
}
