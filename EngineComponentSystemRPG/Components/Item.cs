using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Marks an entity as being an item. This should be extended to add item
    /// specific properties, as necessary.
    /// </summary>
    public class Item : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Index group that tracks items
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

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

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherItem = (Item)other;
            Name = otherItem.Name;
            IconName = otherItem.IconName;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <returns></returns>
        public Item Initialize(string name, string iconName)
        {
            Name = name;
            IconName = iconName;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Name = null;
            IconName = null;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Name);
            hasher.Put(IconName);
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
            return base.ToString() + ", Name=" + Name + ", IconName=" + IconName;
        }

        #endregion
    }
}
