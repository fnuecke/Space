using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    public sealed class UseCommand : FrameCommand
    {
        #region Fields
        /// <summary>
        /// The slot of the item to be used
        /// </summary>
        public int Slot;
        #endregion

        public UseCommand(int slot)
            : base(SpaceCommandType.UseItem)
        {
            Slot = slot;
        }

        public UseCommand()
            : this(-1)
        { }

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
                .Write(Slot);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            
            Slot = packet.ReadInt32();
        }

        #endregion

        #region Equals

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public override bool Equals(Command other)
        {
            return base.Equals(other) &&
                Slot == ((UseCommand)other).Slot;
        }

        #endregion
    }
}
