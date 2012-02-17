using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.Serialization;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Base class for items of our space game.
    /// </summary>
    public class SpaceItem : Item
    {
        #region Fields

        /// <summary>
        /// The quality level of this item.
        /// </summary>
        public ItemQuality Quality;

        /// <summary>
        /// The Name of the Item which shall be Displayed
        /// </summary>
        public string DisplayName;
        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new item with the specified parameters.
        /// </summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        public SpaceItem(string name, string iconName, ItemQuality quality)
            : base(name, iconName)
        {
            this.Quality = quality;
        }
        
        /// <summary>
        /// For deserialization.
        /// </summary>
        public SpaceItem()
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Puts item specific information into the given descripton object.
        /// </summary>
        /// <param name="descripton">The object to write the object information
        /// into.</param>
        public virtual void GetDescription(ref ItemDescription descripton)
        {
            // Reset.
            descripton.Attributes = descripton.Attributes ?? new List<AttributeModifier<AttributeType>>();
            descripton.Attributes.Clear();
            descripton.IsWeapon = false;

            // Add attributes.
            foreach (var component in Entity.Components)
            {
                if (component is Attribute<AttributeType>)
                {
                    descripton.Attributes.Add(((Attribute<AttributeType>)component).Modifier);
                }
            }
        }
        /// <summary>
        /// Calculates the Name of the Item according to the attributes.
        /// </summary>
        /// <param name="item"></param>
        protected void CalculateName()
        {
            DisplayName = "";
            var list = new List<AttributeModifier<AttributeType> >();
            // Add attributes.
            foreach (var component in Entity.Components)
            {
                if (component is Attribute<AttributeType>)
                {
                    list.Add(((Attribute<AttributeType>)component).Modifier);
                }
            }
            if (this is Armor)
            {
                DisplayName+= ItemNames.StarterArmor
            }
            else if (this is Reactor)
            {

            }
            else if (this is Sensor)
            {

            }
            else if (this is Shield)
            {

            }
            else if (this is Thruster)
            {

            }
            else if (this is Weapon)
            {

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
                .Write((byte)Quality);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Quality = (ItemQuality)packet.ReadByte();
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (SpaceItem)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Quality = Quality;
            }

            return copy;
        }

        #endregion
    }
}
