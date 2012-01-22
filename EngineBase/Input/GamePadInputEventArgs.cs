using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{
    public sealed class GamePadInputEventArgs:EventArgs
    {
         /// <summary>
        /// The key that was pressed or released.
        /// </summary>
        public Buttons Buttons { get; private set; }

        ///// <summary>
        ///// The active keyboard modifier combination.
        ///// </summary>
        //public KeyModifier Modifier { get; private set; }

        /// <summary>
        /// Whether this press was generated due to key auto repeat.
        /// </summary>
        public bool IsRepeat { get; private set; }

        /// <summary>
        /// The overall keyboard state that's now active.
        /// </summary>
        public GamePadState State { get; private set; }

        internal GamePadInputEventArgs(GamePadState state, Buttons button,  bool isRepeat)
        {
            this.State = state;
            this.Buttons = button;
            
            this.IsRepeat = isRepeat;
        }

        internal GamePadInputEventArgs(GamePadState state)
        {
            this.State = state;
        }
    }
}
