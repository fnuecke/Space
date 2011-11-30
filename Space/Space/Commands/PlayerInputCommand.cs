using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Util;
using Space.Model;

namespace Space.Commands
{
    class PlayerInputCommand : GameCommand
    {
        public enum PlayerInput
        {
            Accelerate,
            StopMovement,
            TurnLeft,
            TurnRight,
            StopRotation
        }

        /// <summary>
        /// The player input.
        /// </summary>
        public PlayerInput Input { get; private set; }

        /// <summary>
        /// Based on the player input, this is the corresponding direction (for Acceleration).
        /// </summary>
        public Direction Direction { get; private set; }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public PlayerInputCommand()
            : base(GameCommandType.PlayerInput)
        {
        }

        public PlayerInputCommand(Player<PlayerInfo, PacketizerContext> player, long frame, PlayerInput input, Direction direction)
            : base(GameCommandType.PlayerInput, player, frame)
        {
            this.Input = input;
            this.Direction = direction;
        }

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write((byte)Input);
            packet.Write((byte)Direction);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, PacketizerContext context)
        {
            Input = (PlayerInput)packet.ReadByte();
            Direction = (Direction)packet.ReadByte();

            base.Depacketize(packet, context);
        }

        #endregion

        #region Equality

        public override bool Equals(ICommand<GameCommandType, PlayerInfo, PacketizerContext> other)
        {
            return other is PlayerInputCommand &&
                base.Equals(other) &&
                ((PlayerInputCommand)other).Input == this.Input &&
                ((PlayerInputCommand)other).Direction == this.Direction;
        }

        #endregion
    }
}
