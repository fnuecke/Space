using Engine.Commands;
using Engine.Serialization;

namespace Space.Simulation.Commands
{
    class PlayerInputCommand : SimulationCommand
    {

        public enum PlayerInput
        {
            Accelerate,
            Decelerate,
            StopMovement,
            TurnLeft,
            TurnRight,
            StopRotation
        }

        /// <summary>
        /// The player input.
        /// </summary>
        public PlayerInput Input { get; private set; }
        
        public PlayerInputCommand(PlayerInput input, long frame)
            : base((uint)GameCommandType.PlayerInput, frame)
        {
            this.Input = input;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public PlayerInputCommand(Packet packet)
            : base(packet)
        {
            Input = (PlayerInput)packet.ReadUInt32();
        }

        override public void Write(Packet packet)
        {
            base.Write(packet);
            packet.Write((uint)Input);
        }

    }
}
