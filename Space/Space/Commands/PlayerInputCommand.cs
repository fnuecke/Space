using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
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
            TurnLeft,
            TurnRight,
            StopRotation
        }

        /// <summary>
        /// The player input.
        /// </summary>
        public PlayerInput Input { get; private set; }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public PlayerInputCommand()
            : base(GameCommandType.PlayerInput)
        {
        }

        public PlayerInputCommand(Player<PlayerInfo, PacketizerContext> player, long frame, PlayerInput input)
            : base(GameCommandType.PlayerInput, player, frame)
        {
            this.Input = input;
        }

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write((byte)Input);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, PacketizerContext context)
        {
            Input = (PlayerInput)packet.ReadByte();

            base.Depacketize(packet, context);
        }

        #endregion

        #region Equality

        public override bool Equals(ICommand<GameCommandType, PlayerInfo, PacketizerContext> other)
        {
            return other is PlayerInputCommand &&
                base.Equals(other) &&
                ((PlayerInputCommand)other).Input == this.Input;
        }

        #endregion
    }
}
