using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components.Items;
using Engine.Serialization;

namespace Space.ComponentSystem.Components.Items.Consumables
{
    /// <summary>
    /// A repair kit is a consumable that can be used to restore a certain
    /// amount of health to the user's ship.
    /// </summary>
    public sealed class RepairKit : Consumable
    {
        #region Fields
        
        /// <summary>
        /// The amount of health the repair kit restores when used.
        /// </summary>
        public int HealthRestored;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new repair kit with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="healthRestored">The amount of health this kit restores.</param>
        public RepairKit(string name, string iconName, int healthRestored)
            : base(name, iconName)
        {
            this.HealthRestored = healthRestored;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public RepairKit()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Use the item, have it trigger its logic. Consumes one item of a
        /// stack, destroys the item if only one is left (or it's not
        /// stackable, which is equivalent). Restores some health.
        /// </summary>
        public override void Use()
        {
            base.Use();

            var health = Entity.GetComponent<Health>();
            if (health != null)
            {
                health.Value += HealthRestored;
            }
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
                .Write(HealthRestored);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            HealthRestored = packet.ReadInt32();
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (RepairKit)base.DeepCopy(into);

            if (copy == into)
            {
                copy.HealthRestored = HealthRestored;
            }

            return copy;
        }

        #endregion
    }
}
