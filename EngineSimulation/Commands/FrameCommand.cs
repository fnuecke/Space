using System;
using Engine.Serialization;

namespace Engine.Simulation.Commands
{
    /// <summary>
    /// Base class for commands that can be injected into running simulations.
    /// </summary>
    public abstract class FrameCommand : Command
    {
        #region Fields

        /// <summary>
        /// The frame this command applies to.
        /// </summary>
        public long Frame;

        #endregion
        
        #region Constructor

        protected FrameCommand(Enum type)
            : base(type)
        {
        }

        #endregion
        
        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Frame);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            Frame = packet.ReadInt64();
        }

        #endregion

        #region Equality

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public override bool Equals(Command other)
        {
            return other is FrameCommand && base.Equals(other) &&
                ((FrameCommand)other).Frame == Frame;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Frame = " + Frame;
        }

        #endregion
    }
}
