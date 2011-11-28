using Engine.Commands;
using Engine.Serialization;
using Engine.Util;

namespace Space.Commands
{
    class PlayerInputCommand : SimulationCommand<GameCommandType>
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
        {
        }

        public PlayerInputCommand(PlayerInput input, Direction direction, long frame)
            : base(GameCommandType.PlayerInput, frame)
        {
            this.Input = input;
            this.Direction = direction;
        }

        public override void Packetize(Packet packet)
        {
            packet.Write((byte)Input);
            packet.Write((byte)Direction);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            Input = (PlayerInput)packet.ReadByte();
            Direction = (Direction)packet.ReadByte();

            base.Depacketize(packet);
        }
    }
}
