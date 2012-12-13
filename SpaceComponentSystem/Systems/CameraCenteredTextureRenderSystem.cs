using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Defines a render system which always translates the view to be
    /// centered to the camera.
    /// </summary>
    public sealed class CameraCenteredTextureRenderSystem : TextureRenderSystem
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraCenteredTextureRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        public CameraCenteredTextureRenderSystem(ContentManager content, SpriteBatch spriteBatch)
            : base(content, spriteBatch)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="layerDepth">The base layer depth to render at.</param>
        protected override void DrawComponent(TextureRenderer component, Vector2 position, float rotation, float layerDepth)
        {
            // See what we're drawing.
            ShipInfo shipInfo;
            if ((shipInfo = (ShipInfo)Manager.GetComponent(component.Entity, ShipInfo.TypeId)) != null &&
                shipInfo.Equipment != null && shipInfo.Equipment.Item > 0)
            {
                // Precompute sine and cosine (because they're expensive).
                var cosRadians = (float)Math.Cos(rotation);
                var sinRadians = (float)Math.Sin(rotation);
                RenderEquipment(shipInfo.Equipment, position, rotation, cosRadians, sinRadians, layerDepth);
            }
            else
            {
                // Default drawing, just render the texture. Get the rectangle at which we'll draw.
                Vector2 origin;
                origin.X = component.Texture.Width / 2f;
                origin.Y = component.Texture.Height / 2f;

                // Draw.
                SpriteBatch.Draw(component.Texture, position, null, component.Tint, rotation, origin, component.Scale,
                                 SpriteEffects.None, layerDepth);
            }
        }

        private void RenderEquipment(SpaceItemSlot slot, Vector2 offset, float rotation, float cosRadians, float sinRadians, float layerDepth, int depth = 1, float order = 0.5f, ItemSlotSize parentSize = ItemSlotSize.Small, bool? mirrored = null)
        {
            // Get item info.
            var item = (SpaceItem)Manager.GetComponent(slot.Item, Item.TypeId);

            // Get renderer and load texture if necessary.
            var renderer = (TextureRenderer)Manager.GetComponent(item.Entity, TextureRenderer.TypeId);
            if (renderer.Texture == null)
            {
                renderer.Texture = Content.Load<Texture2D>(renderer.TextureName);
            }

            // Adjust depth we want to render at.
            order += (item.DrawBelowParent ? -0.25f : 0.25f) / depth;

            // See if we should mirror rendering (e.g. left wing).
            var slotOffset = slot.Offset;
            var itemOffset = item.ModelOffset;
            if (mirrored.HasValue)
            {
                if (mirrored.Value)
                {
                    slotOffset.Y = -slotOffset.Y;
                }
            }
            else if (slotOffset.Y != 0)
            {
                mirrored = slot.Offset.Y < 0;
            }
            if (mirrored.HasValue && mirrored.Value)
            {
                itemOffset.Y = -itemOffset.Y;
            }

            // Move the offset according to rotation and accumulate it.
            var localOffset = offset;
            localOffset.X += parentSize.Scale(slotOffset.X * cosRadians - slotOffset.Y * sinRadians);
            localOffset.Y += parentSize.Scale(slotOffset.X * sinRadians + slotOffset.Y * cosRadians);

            var renderOffset = localOffset;
            renderOffset.X += item.RequiredSlotSize.Scale(itemOffset.X * cosRadians - itemOffset.Y * sinRadians);
            renderOffset.Y += item.RequiredSlotSize.Scale(itemOffset.X * sinRadians + itemOffset.Y * cosRadians);

            rotation += mirrored.HasValue && mirrored.Value ? -slot.Rotation : slot.Rotation;
            
            // Get center of texture.
            Vector2 origin;
            origin.X = renderer.Texture.Width / 2f;
            origin.Y = renderer.Texture.Height / 2f;

            // And render.
            SpriteBatch.Draw(renderer.Texture,
                renderOffset,
                null,
                renderer.Tint,
                rotation,
                origin,
                item.RequiredSlotSize.Scale(),
                mirrored.HasValue && mirrored.Value ? SpriteEffects.FlipVertically : SpriteEffects.None,
                order + layerDepth);

            // Render sub-items.
            foreach (SpaceItemSlot childSlot in Manager.GetComponents(item.Entity, ItemSlot.TypeId))
            {
                if (childSlot.Item > 0)
                {
                    RenderEquipment(childSlot, localOffset, rotation, cosRadians, sinRadians,
                        layerDepth, depth + 1, order, item.RequiredSlotSize, mirrored);
                }
            }
        }

        /// <summary>
        /// Gets the list of currently visible entities.
        /// </summary>
        /// <returns>
        /// The list of visible entities.
        /// </returns>
        protected override System.Collections.Generic.IEnumerable<int> GetVisibleEntities()
        {
            return ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).VisibleEntities;
        }

        /// <summary>
        /// Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected override FarTransform GetTransform()
        {
            return ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        #endregion
    }
}
