using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class SimulationCommand<T, TPlayerData, TPacketizerContext> : Command<T, TPlayerData, TPacketizerContext>, ISimulationCommand<T, TPlayerData, TPacketizerContext>
        where T : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
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

        protected SimulationCommand(T type, Player<TPlayerData, TPacketizerContext> player, long frame)
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

        public override void Depacketize(Packet packet, TPacketizerContext context)
        {
            Frame = packet.ReadInt64();

            base.Depacketize(packet, context);
        }

        #endregion

        #region Equality

        public override bool Equals(ICommand<T, TPlayerData, TPacketizerContext> other)
        {
            return other is ISimulationCommand<T, TPlayerData, TPacketizerContext> &&
                base.Equals(other) &&
                ((ISimulationCommand<T, TPlayerData, TPacketizerContext>)other).Frame == this.Frame;
        }

        #endregion
    }
}
