using System;
using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class FrameCommand<TPlayerData, TPacketizerContext>
        : Command<TPlayerData, TPacketizerContext>, IFrameCommand<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// The frame this command applies to.
        /// </summary>
        public long Frame { get; set; }

        #endregion
        
        #region Constructor

        protected FrameCommand(Enum type)
            : base(type)
        {
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

        public override bool Equals(ICommand<TPlayerData, TPacketizerContext> other)
        {
            return other is IFrameCommand<TPlayerData, TPacketizerContext> &&
                base.Equals(other) &&
                ((IFrameCommand<TPlayerData, TPacketizerContext>)other).Frame == this.Frame;
        }

        #endregion
    }
}
