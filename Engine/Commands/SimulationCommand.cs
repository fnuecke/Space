using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class SimulationCommand<TCommandType, TPlayerData, TPacketizerContext> : Command<TCommandType, TPlayerData, TPacketizerContext>, ISimulationCommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// The frame this command applies to.
        /// </summary>
        public long Frame { get; private set; }

        #endregion

        #region Constructor

        protected SimulationCommand(TCommandType type)
            : base(type)
        {
        }

        protected SimulationCommand(TCommandType type, Player<TPlayerData, TPacketizerContext> player, long frame)
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

        public override bool Equals(ICommand<TCommandType, TPlayerData, TPacketizerContext> other)
        {
            return other is ISimulationCommand<TCommandType, TPlayerData, TPacketizerContext> &&
                base.Equals(other) &&
                ((ISimulationCommand<TCommandType, TPlayerData, TPacketizerContext>)other).Frame == this.Frame;
        }

        #endregion
    }
}
