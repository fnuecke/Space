using Engine.Commands;
using Engine.Math;
using Engine.Serialization;

namespace Space.Commands
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
        public Fixed TargetRotation { get; private set; }

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

        public PlayerInputCommand(PlayerInput input, Fixed targetRotation)
            : this()
        {
            this.Input = input;
            this.TargetRotation = targetRotation;
        }

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write((byte)Input);
            packet.Write(TargetRotation);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            Input = (PlayerInput)packet.ReadByte();
            TargetRotation = packet.ReadFixed();

            base.Depacketize(packet);
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
