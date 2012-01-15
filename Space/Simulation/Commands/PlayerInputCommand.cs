using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// A player input command, issued by a player to control his ship.
    /// </summary>
    sealed class PlayerInputCommand : FrameCommand
    {
        #region Types
        
        /// <summary>
        /// Possible command types.
        /// </summary>
        public enum PlayerInputCommandType
        {
            /// <summary>
            /// No command at all.
            /// </summary>
            None,

            /// <summary>
            /// Accelerate upwards.
            /// </summary>
            AccelerateUp,

            /// <summary>
            /// Accelerate to the right.
            /// </summary>
            AccelerateRight,

            /// <summary>
            /// Accelerate downwards.
            /// </summary>
            AccelerateDown,

            /// <summary>
            /// Accelerate to the left.
            /// </summary>
            AccelerateLeft,

            /// <summary>
            /// Stop upwards acceleration.
            /// </summary>
            StopUp,

            /// <summary>
            /// Stop accelerating to the right.
            /// </summary>
            StopRight,

            /// <summary>
            /// Stop downwards acceleration.
            /// </summary>
            StopDown,

            /// <summary>
            /// Stop accelerating to the left.
            /// </summary>
            StopLeft,

            /// <summary>
            /// Rotate to a specified angle.
            /// </summary>
            Rotate,

            /// <summary>
            /// Begin shooting.
            /// </summary>
            Shoot,

            /// <summary>
            /// Stop shooting.
            /// </summary>
            CeaseFire
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// The player input.
        /// </summary>
        public PlayerInputCommandType Input;

        /// <summary>
        /// An additional value, e.g. for rotate commands this is the targeted
        /// rotation angle, for acceleration commands it is the relative
        /// thrust power.
        /// </summary>
        public float Value;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInputCommand"/> class.
        /// </summary>
        /// <param name="input">The player input.</param>
        /// <param name="value">The associated value.</param>
        public PlayerInputCommand(PlayerInputCommandType input, float value)
            : base(SpaceCommandType.PlayerInput)
        {
            this.Input = input;
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInputCommand"/> class.
        /// </summary>
        /// <param name="input">The player input.</param>
        public PlayerInputCommand(PlayerInputCommandType input)
            : this(input, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInputCommand"/> class.
        /// </summary>
        /// <remarks>
        /// For serialization.
        /// </remarks>
        public PlayerInputCommand()
            : this(PlayerInputCommandType.None, 0)
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
                .Write((byte)Input)
                .Write(Value);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Input = (PlayerInputCommandType)packet.ReadByte();
            Value = packet.ReadSingle();
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
            return other is PlayerInputCommand &&
                base.Equals(other) &&
                ((PlayerInputCommand)other).Input == this.Input &&
                ((PlayerInputCommand)other).Value == this.Value;
        }

        #endregion
    }
}
