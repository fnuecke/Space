using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class FrameCommand<TCommandType, TPlayerData, TPacketizerContext> : Command<TCommandType, TPlayerData, TPacketizerContext>, IFrameCommand<TCommandType, TPlayerData, TPacketizerContext>
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

        protected FrameCommand(TCommandType type)
            : base(type)
        {
        }

        protected FrameCommand(TCommandType type, Player<TPlayerData, TPacketizerContext> player, long frame)
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
            return other is IFrameCommand<TCommandType, TPlayerData, TPacketizerContext> &&
                base.Equals(other) &&
                ((IFrameCommand<TCommandType, TPlayerData, TPacketizerContext>)other).Frame == this.Frame;
        }

        #endregion
    }
}
