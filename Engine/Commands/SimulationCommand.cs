using Engine.Serialization;

namespace Engine.Commands
{
    public class SimulationCommand : Command, ISimulationCommand
    {

        public long Frame { get; private set; }

        protected SimulationCommand(uint type, long frame)
            : base(type)
        {
            this.Frame = frame;
        }

        protected SimulationCommand(Packet packet)
            : base(packet)
        {
            Frame = packet.ReadInt64();
        }

        override public void Write(Packet packet)
        {
            base.Write(packet);
            packet.Write(Frame);
        }
    }
}
