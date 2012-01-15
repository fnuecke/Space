using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Used to inject debugging commands into the simulation.
    /// </summary>
    /// <remarks>
    /// Values must be changed like this from the outside, so as not to mess up
    /// different states in a TSS simulation.
    /// </remarks>
    sealed class DebugCommand : FrameCommand
    {
        #region Types
        
        /// <summary>
        /// Possible debug commands.
        /// </summary>
        public enum DebugCommandType
        {
            /// <summary>
            /// No command at all.
            /// </summary>
            None,

            /// <summary>
            /// Jump to the specified position in the universe.
            /// </summary>
            GotoPosition,

            /// <summary>
            /// The the acceleration force of all equipped thruster.
            /// </summary>
            SetThrusterAccelerationForce,

            /// <summary>
            /// Set the energy consumption of all equipped thruster.
            /// </summary>
            SetThrusterEnergyConsumption
        }

        #endregion

        #region Fields

        /// <summary>
        /// The debug command to apply.
        /// </summary>
        public DebugCommandType Debug;

        /// <summary>
        /// The data (arguments) sent for the command.
        /// </summary>
        public Packet Data;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugCommand"/> class.
        /// </summary>
        /// <param name="command">The debug command.</param>
        /// <param name="data">The command arguments.</param>
        public DebugCommand(DebugCommandType command, Packet data)
            : base(SpaceCommandType.DebugCommand)
        {
            Debug = command;
            Data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugCommand"/> class.
        /// </summary>
        /// <param name="command">The debug command.</param>
        public DebugCommand(DebugCommandType command)
            : this(command, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugCommand"/> class.
        /// </summary>
        /// <remarks>
        /// For serialization.
        /// </remarks>
        public DebugCommand()
            : this(DebugCommandType.None, null)
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
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)Debug)
                .Write(Data);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Debug = (DebugCommandType)packet.ReadByte();
            Data = packet.ReadPacket();
        }

        #endregion

        #region Equality

        /// <summary>
        /// Tests for equality with the specified object.
        /// </summary>
        /// <param name="other">The object to test for equality with.</param>
        /// <returns>Whether this and the specified object are equal.</returns>
        public override bool Equals(Command other)
        {
            return other is DebugCommand &&
                base.Equals(other) &&
                ((DebugCommand)other).Debug == this.Debug &&
                ((DebugCommand)other).Data.Equals(this.Data);
        }

        #endregion
    }
}
