namespace Space.Simulation.Commands
{
    /// <summary>
    /// Used to inject debugging commands into the simulation.
    /// </summary>
    /// <remarks>
    /// Values must be changed like this from the outside, so as not to mess up
    /// different states in a TSS simulation.
    /// </remarks>
    internal sealed class ScriptCommand : Engine.Simulation.Commands.FrameCommand
    {
        #region Fields

        /// <summary>
        /// The debug command to apply.
        /// </summary>
        public string Script;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCommand"/> class.
        /// </summary>
        /// <param name="script">The script to execute.</param>
        public ScriptCommand(string script)
            : base(SpaceCommandType.ScriptCommand)
        {
            Script = script;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCommand"/> class.
        /// </summary>
        /// <remarks>
        /// For serialization.
        /// </remarks>
        public ScriptCommand()
            : this(string.Empty)
        {
        }

        #endregion
    }
}
