using System;

namespace Engine.Util
{
    /// <summary>
    /// Event args used for line written events in the <see cref="GameConsole"/>.
    /// </summary>
    public class LineWrittenEventArgs : EventArgs
    {
        /// <summary>
        /// The text that was written to the console.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineWrittenEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public LineWrittenEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
