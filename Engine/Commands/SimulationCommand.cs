using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public class SimulationCommand<T, TPlayerData> : Command<T, TPlayerData>, ISimulationCommand<T, TPlayerData>
        where T : struct
        where TPlayerData : IPacketizable
    {
        #region Properties

        /// <summary>
        /// The frame this command applies to.
        /// </summary>
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

        protected SimulationCommand(T type, Player<TPlayerData> player, long frame)
            : base(type, player)
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
