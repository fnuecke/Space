namespace Space.Simulation.Commands
{
    /// <summary>
    /// Used to inject debugging commands into the simulation.
    /// </summary>
    /// <remarks>
    /// Values must be changed like this from the outside, so as not to mess up
    /// different states in a TSS simulation.
    /// </remarks>
    sealed class ScriptCommand : Engine.Simulation.Commands.FrameCommand
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

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Engine.Serialization.Packet Packetize(Engine.Serialization.Packet packet)
        {
            return base.Packetize(packet)
                .Write(Script);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Engine.Serialization.Packet packet)
        {
            base.Depacketize(packet);

            Script = packet.ReadString();
        }

        #endregion
    }
}
