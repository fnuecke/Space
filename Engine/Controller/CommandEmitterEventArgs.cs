using System;

namespace Engine.Controller
{
    public class CommandEmitterEventArgs : EventArgs
    {
        /// <summary>
        /// The command that was generated.
        /// </summary>
        public object Command { get; private set; }

        /// <summary>
        /// Creates a new instance, initialized to the given command.
        /// </summary>
        /// <param name="command"></param>
        public CommandEmitterEventArgs(object command)
        {
            this.Command = command;
        }
    }
}
