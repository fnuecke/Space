using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Marks an entity as being an item. This should be extended to add item
    /// specific properties, as necessary.
    /// </summary>
    public class Item : AbstractComponent
    {
        #region Constants

        /// <summary>
        /// Index group that tracks items
        /// </summary>
        public static readonly ulong IndexGroup = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Fields

        /// <summary>
        /// The base name of this item, i.e. its base type, as set in the XML.
        /// This is essentially an ID and should never be displayed directly,
        /// but instead used to localize the name.
        /// </summary>
        public string Name;

        /// <summary>
        /// The asset name of the texture to use to display the item in menus
        /// and the inventory, e.g.
        /// </summary>
        public string IconName;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new item with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        public Item(string name, string iconName)
        {
            this.Name = name;
            this.IconName = iconName;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Item()
            : this(string.Empty, string.Empty)
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
                .Write(Name)
                .Write(IconName);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Name = packet.ReadString();
            IconName = packet.ReadString();
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Item)base.DeepCopy(into);

            if (into == copy)
            {
                copy.Name = Name;
                copy.IconName = IconName;
            }

            return copy;
        }

        #endregion
    }
}
