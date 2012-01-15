using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Allows assigning entities to factions.
    /// </summary>
    public class Faction : AbstractComponent
    {
        #region Fields
        
        /// <summary>
        /// The faction this component's entity belongs to.
        /// </summary>
        public Factions Value;

        #endregion

        #region Constructor

        public Faction(Factions factions)
        {
            this.Value = factions;
        }

        public Faction()
            : this(Factions.None)
        {
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write((uint)Value);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Value = (Factions)packet.ReadUInt32();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put((byte)Value);
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Faction)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Value = Value;
            }

            return copy;
        }

        #endregion
    }
}
