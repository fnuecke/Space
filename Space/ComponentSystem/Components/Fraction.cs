using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Allows assigning entities to fractions.
    /// </summary>
    public class Fraction : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// The fraction this component's entity belongs to.
        /// </summary>
        public Fractions Value { get; set; }

        #endregion

        #region Constructor

        public Fraction(Fractions fraction)
        {
            this.Value = fraction;
        }

        public Fraction()
            : this(Fractions.None)
        {
        }

        #endregion

        #region Serialization

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((byte)Value);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = (Fractions)packet.ReadByte();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put((byte)Value);
        }

        #endregion
    }
}
