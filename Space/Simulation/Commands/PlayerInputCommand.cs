using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    class PlayerInputCommand : GameCommand
    {
        public enum PlayerInput
        {
            AccelerateUp,
            AccelerateRight,
            AccelerateDown,
            AccelerateLeft,
            StopUp,
            StopRight,
            StopDown,
            StopLeft,
            Rotate,
            Shoot,
            CeaseFire
        }

        /// <summary>
        /// The player input.
        /// </summary>
        public PlayerInput Input { get; private set; }

        /// <summary>
        /// For rotate commands this is the targeted rotation angle.
        /// </summary>
        public float TargetRotation { get; private set; }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public PlayerInputCommand()
            : base(GameCommandType.PlayerInput)
        {
        }

        public PlayerInputCommand(PlayerInput input)
            : this()
        {
            this.Input = input;
        }

        public PlayerInputCommand(PlayerInput input, float targetRotation)
            : this()
        {
            this.Input = input;
            this.TargetRotation = targetRotation;
        }

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)Input)
                .Write(TargetRotation);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Input = (PlayerInput)packet.ReadByte();
            TargetRotation = packet.ReadSingle();
        }

        #endregion

        #region Equality

        public override bool Equals(ICommand other)
        {
            return other is PlayerInputCommand &&
                base.Equals(other) &&
                ((PlayerInputCommand)other).Input == this.Input &&
                ((PlayerInputCommand)other).TargetRotation == this.TargetRotation;
        }

        #endregion
    }
}
