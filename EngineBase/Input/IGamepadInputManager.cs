using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input
{
    public interface IGamepadInputManager
    {

        /// <summary>
        /// Fired when a key is newly pressed or repeated.
        /// </summary>
        event EventHandler<EventArgs> Pressed;

        /// <summary>
        /// Fired when a key is released.
        /// </summary>
        event EventHandler<EventArgs> Released;

        event EventHandler<EventArgs> LeftMoved;
        event EventHandler<EventArgs> RightMoved;
    }
}
