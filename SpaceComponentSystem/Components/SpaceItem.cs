using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;
using Microsoft.Xna.Framework;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>Base class for items of our space game.</summary>
    public class SpaceItem : Item
    {
        #region Fields

        /// <summary>The quality level of this item.</summary>
        public ItemQuality Quality;

        /// <summary>The item's minimal required slot size.</summary>
        public ItemSlotSize RequiredSlotSize;

        /// <summary>Offset at which to render the equipped item relative to its slot.</summary>
        public Vector2 ModelOffset;

        /// <summary>Whether to render on below the parent item when equipped.</summary>
        public bool DrawBelowParent;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherItem = (SpaceItem) other;
            Quality = otherItem.Quality;
            RequiredSlotSize = otherItem.RequiredSlotSize;
            ModelOffset = otherItem.ModelOffset;
            DrawBelowParent = otherItem.DrawBelowParent;

            return this;
        }

        /// <summary>Creates a new item with the specified parameters.</summary>
        /// <param name="name">The logical base name of the item.</param>
        /// <param name="iconName">The name of the icon used for the item.</param>
        /// <param name="quality">The quality level of the item.</param>
        /// <param name="slotSize">Size of the slot.</param>
        /// <param name="modelOffset">The model offset.</param>
        /// <param name="drawBelowParent">Whether to draw below the parent item, when equipped.</param>
        /// <returns>This instance.</returns>
        public SpaceItem Initialize(
            string name,
            string iconName,
            ItemQuality quality,
            ItemSlotSize slotSize,
            Vector2 modelOffset,
            bool drawBelowParent)
        {
            Initialize(name, iconName);

            Quality = quality;
            RequiredSlotSize = slotSize;
            ModelOffset = modelOffset;
            DrawBelowParent = drawBelowParent;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Quality = ItemQuality.Common;
            RequiredSlotSize = ItemSlotSize.None;
            ModelOffset = Vector2.Zero;
            DrawBelowParent = false;
        }

        #endregion
    }
}