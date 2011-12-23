using System;
using Engine.Serialization;

namespace Engine.Simulation.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class FrameCommand : Command, IFrameCommand
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

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Frame);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            Frame = packet.ReadInt64();
        }

        #endregion

        #region Equality

        public override bool Equals(ICommand other)
        {
            return other is IFrameCommand && base.Equals(other) &&
                ((IFrameCommand)other).Frame == this.Frame;
        }

        #endregion
    }
}
