using Engine.Serialization;

namespace Engine.Commands
{
    public class SimulationCommand<T> : Command<T>, ISimulationCommand<T>
        where T : struct
    {
        #region Properties

        public long Frame { get; private set; }

        #endregion

        #region Constructor

        protected SimulationCommand(T type)
            : base(type)
        {
        }

        protected SimulationCommand(T type, long frame)
            : base(type)
        {
            this.Frame = frame;
        }

        #endregion

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write(Frame);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            Frame = packet.ReadInt64();

            base.Depacketize(packet);
        }

        #endregion
    }
}
