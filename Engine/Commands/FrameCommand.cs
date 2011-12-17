using System;
using Engine.Serialization;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class FrameCommand<TPlayerData> : Command<TPlayerData>, IFrameCommand<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
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

        public override void Depacketize(Packet packet, IPacketizerContext<TPlayerData> context)
        {
            Frame = packet.ReadInt64();

            base.Depacketize(packet, context);
        }

        #endregion

        #region Equality

        public override bool Equals(ICommand<TPlayerData> other)
        {
            return other is IFrameCommand<TPlayerData> && base.Equals(other) &&
                ((IFrameCommand<TPlayerData>)other).Frame == this.Frame;
        }

        #endregion
    }
}
