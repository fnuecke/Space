using Engine.Commands;
using Engine.Serialization;

namespace Space.Commands
{
    class AddPlayerCommand : SimulationCommand<GameCommandType>
    {
        public int PlayerNumber { get; private set; }
        
        /// <summary>
        /// For deserialization.
        /// </summary>
        public AddPlayerCommand()
            : base(GameCommandType.AddPlayerShip)
        {
        }

        public AddPlayerCommand(int playerNumber, long frame)
            : base(GameCommandType.AddPlayerShip, frame)
        {
            this.PlayerNumber = playerNumber;
        }

        public override void Packetize(Packet packet)
        {
            packet.Write(PlayerNumber);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            PlayerNumber = packet.ReadInt32();

            base.Depacketize(packet);
        }
    }
}
