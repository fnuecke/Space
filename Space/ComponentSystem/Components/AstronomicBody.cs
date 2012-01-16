using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    public sealed class AstronomicBody : AbstractComponent
    {
        #region Fields

        public AstronomicBodyType Type;

        #endregion

        #region Constructor

        public AstronomicBody(AstronomicBodyType type)
        {
            this.Type = type;
        }

        public AstronomicBody()
        {
        }

        #endregion

        #region Serialization / Hashing

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
                .Write((byte)Type);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Type = (AstronomicBodyType)packet.ReadByte();
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (AstronomicBody)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Type = Type;
            }

            return copy;
        }

        #endregion
    }
}
