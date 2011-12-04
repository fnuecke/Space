using Engine.Commands;
using Engine.Math;
using Engine.Serialization;
using Space.Model;

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
            Rotate
        }

        /// <summary>
        /// The player input.
        /// </summary>
        public PlayerInput Input { get; private set; }

        /// <summary>
        /// For rotate commands this is the targeted rotation angle.
        /// </summary>
        public Fixed TargetAngle { get; private set; }

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

        public PlayerInputCommand(PlayerInput input, Fixed targetAngle)
            : this()
        {
            this.Input = input;
            this.TargetAngle = targetAngle;
        }

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write((byte)Input);
            packet.Write(TargetAngle);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, PacketizerContext context)
        {
            Input = (PlayerInput)packet.ReadByte();
            TargetAngle = packet.ReadFixed();

            base.Depacketize(packet, context);
        }

        #endregion

        #region Equality

        public override bool Equals(ICommand<GameCommandType, PlayerInfo, PacketizerContext> other)
        {
            return other is PlayerInputCommand &&
                base.Equals(other) &&
                ((PlayerInputCommand)other).Input == this.Input &&
                ((PlayerInputCommand)other).TargetAngle == this.TargetAngle;
        }

        #endregion
    }
}
