using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.Simulation.Commands;
using Engine.Serialization;

namespace Space.Simulation.Commands
{
    public class MoveItemCommand : FrameCommand
    {

        public int Id1;
        public int Id2;

        public MoveItemCommand(int id1, int id2)
            :base(SpaceCommandType.MoveItem)
        {
            Id1 = id1;
            Id2 = id2;
        }
        public MoveItemCommand()
        :this(-1,-1)
        {

        }

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
                .Write(Id1)
                .Write(Id2);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Id1 = packet.ReadInt32();
            Id2 = packet.ReadInt32();
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
                Id1 == ((MoveItemCommand)other).Id1 &&
                Id2 == ((MoveItemCommand)other).Id2;
        }

        #endregion
    }
}
