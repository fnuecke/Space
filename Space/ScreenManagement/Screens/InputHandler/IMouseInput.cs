using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nuclex.Input;

namespace Space.ScreenManagement.Screens.Interfaces
{
    /// <summary>
    /// An interface that guarantees an object having all necessary
    /// mouse input handler methods. 
    /// </summary>
    public interface IMouseInput
    {
        /// <summary>
        /// Handle mouse presses.
        /// </summary>
        void HandleMousePressed(MouseButtons buttons);

        /// <summary>
        /// Handle mouse releases.
        /// </summary>
        void HandleMouseReleased(MouseButtons buttons);

        /// <summary>
        /// Handle mouse movings.
        /// </summary>
        void HandleMouseMoved(float x, float y);
    }
}
