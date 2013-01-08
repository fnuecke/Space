using Engine.Simulation.Commands;
using Microsoft.Xna.Framework;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// A player input command, issued by a player to control his ship.
    /// </summary>
    internal sealed class PlayerInputCommand : FrameCommand
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
            /// Accelerate .
            /// </summary>
            Accelerate,

            /// <summary>
            /// Rotate to a specified angle.
            /// </summary>
            Rotate,

            /// <summary>
            /// Try to stabilize the position.
            /// </summary>
            BeginStabilizing,

            /// <summary>
            /// Stop trying to stabilize the position.
            /// </summary>
            StopStabilizing,

            /// <summary>
            /// Begin shooting.
            /// </summary>
            BeginShooting,

            /// <summary>
            /// Stop shooting.
            /// </summary>
            StopShooting,

            /// <summary>
            /// Enable ship's shields (if it has some).
            /// </summary>
            BeginShielding,

            /// <summary>
            /// Disable ship's shields.
            /// </summary>
            StopShielding
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
        public Vector2 Value;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInputCommand"/> class.
        /// </summary>
        /// <param name="input">The player input.</param>
        /// <param name="value">The associated value.</param>
        public PlayerInputCommand(PlayerInputCommandType input, Vector2 value)
            : base(SpaceCommandType.PlayerInput)
        {
            Input = input;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInputCommand"/> class.
        /// </summary>
        /// <param name="input">The player input.</param>
        public PlayerInputCommand(PlayerInputCommandType input)
            : this(input, Vector2.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInputCommand"/> class.
        /// </summary>
        /// <remarks>
        /// For serialization.
        /// </remarks>
        public PlayerInputCommand()
            : this(PlayerInputCommandType.None, Vector2.Zero)
        {
        }

        #endregion
    }
}
