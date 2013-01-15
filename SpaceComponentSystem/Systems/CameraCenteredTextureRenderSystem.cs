using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.FarMath;
using Engine.Math;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Defines a render system which always translates the view to be centered to the camera.</summary>
    public sealed class CameraCenteredTextureRenderSystem : TextureRenderSystem
    {
        #region Fields

        /// <summary>
        ///     We cache rendered ship models, indexed by the equipment hash, to avoid having to re-render them each frame,
        ///     which is slow due to the frequent texture switching for each item.
        /// </summary>
        private readonly Dictionary<uint, CacheEntry> _modelCache = new Dictionary<uint, CacheEntry>();

        /// <summary>The struct used to store single model cache entries.</summary>
        private sealed class CacheEntry
        {
            /// <summary>The actual model texture used for rendering.</summary>
            public Texture2D Texture;

            /// <summary>Offset to render the cached texture at.</summary>
            public Vector2 Offset;

            /// <summary>The last frame the model was used in (to clean up unused models).</summary>
            public long LastUsedFrame;
        }

        #endregion

        #region Logic

        /// <summary>Called when the graphics device is being disposed, and any assets manually allocated should be disposed.</summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();

            foreach (var entry in _modelCache)
            {
                entry.Value.Texture.Dispose();
            }
            _modelCache.Clear();
        }

        public override void Draw(long frame, float elapsedMilliseconds)
        {
            // Rebuild model cache. We have to do that in advance, because
            // we want to perform the actual render pass in one block, and
            // using a custom spritebatch in between would break that.
            foreach (var entity in GetVisibleEntities())
            {
                var component = ((TextureRenderer) Manager.GetComponent(entity, TextureRenderer.TypeId));

                // Skip invalid or disabled entities.
                if (component == null || !component.Enabled)
                {
                    continue;
                }

                // Skip if it's not a ship.
                ShipInfo shipInfo;
                if ((shipInfo = (ShipInfo) Manager.GetComponent(component.Entity, ShipInfo.TypeId)) == null ||
                    shipInfo.Equipment == null || shipInfo.Equipment.Item <= 0)
                {
                    continue;
                }

                // Compute the hash for this model.
                var equipmentHash = HashEquipment(shipInfo.Equipment);

                // Create cache texture if necessary.
                if (!_modelCache.ContainsKey(equipmentHash))
                {
                    // No cache entry yet, create it.
                    var size = ComputeModelSize(shipInfo.Equipment, Vector2.Zero);
                    var target = new RenderTarget2D(
                        SpriteBatch.GraphicsDevice,
                        (int) System.Math.Ceiling(size.Width),
                        (int) System.Math.Ceiling(size.Height));
                    var previousRenderTargets = SpriteBatch.GraphicsDevice.GetRenderTargets();
                    SpriteBatch.GraphicsDevice.SetRenderTarget(target);
                    SpriteBatch.GraphicsDevice.Clear(Color.Transparent);
                    SpriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    RenderEquipment(shipInfo.Equipment, new Vector2(-size.X, -size.Y));
                    SpriteBatch.End();
                    SpriteBatch.GraphicsDevice.SetRenderTargets(previousRenderTargets);

                    _modelCache[equipmentHash] = new CacheEntry
                    {
                        Texture = target,
                        Offset = new Vector2(-size.X, -size.Y)
                    };
                }

                // Update last access time.
                _modelCache[equipmentHash].LastUsedFrame = frame;
            }

            // Prune old, unused cache entries.
            var deprecated = _modelCache
                .Where(c => c.Value.LastUsedFrame < frame - 10 * Settings.TicksPerSecond)
                .Select(c => c.Key).ToList();
            foreach (var entry in deprecated)
            {
                _modelCache[entry].Texture.Dispose();
                _modelCache.Remove(entry);
            }

            // Then do actual rendering.
            base.Draw(frame, elapsedMilliseconds);
        }

        /// <summary>Draws the component.</summary>
        /// <param name="component">The component.</param>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="layerDepth">The base layer depth to render at.</param>
        protected override void DrawComponent(
            TextureRenderer component, Vector2 position, float rotation, float layerDepth)
        {
            // See what we're drawing.
            ShipInfo shipInfo;
            if ((shipInfo = (ShipInfo) Manager.GetComponent(component.Entity, ShipInfo.TypeId)) != null &&
                shipInfo.Equipment != null && shipInfo.Equipment.Item > 0)
            {
                // Cached drawing, just render the texture.
                var cachedModel = _modelCache[HashEquipment(shipInfo.Equipment)];
                SpriteBatch.Draw(
                    cachedModel.Texture,
                    position,
                    null,
                    Color.White,
                    rotation,
                    cachedModel.Offset,
                    component.Scale,
                    SpriteEffects.None,
                    layerDepth);
            }
            else
            {
                // Default drawing, just render the texture.
                Vector2 origin;
                origin.X = component.Texture.Width / 2f;
                origin.Y = component.Texture.Height / 2f;

                // Draw.
                SpriteBatch.Draw(
                    component.Texture,
                    position,
                    null,
                    component.Tint,
                    rotation,
                    origin,
                    component.Scale,
                    SpriteEffects.None,
                    layerDepth);
            }
        }

        /// <summary>
        ///     Computes a hash based on the currently equipped items, using the values relevant for rendering that item. An
        ///     identical hash will therefore mean that an equipment branch will look exactly the same when rendered.
        /// </summary>
        /// <param name="slot">The slot to start from.</param>
        /// <returns>Hash for the equipment branch.</returns>
        private uint HashEquipment(ItemSlot slot)
        {
            var hasher = new Hasher();

            foreach (var itemId in slot.AllItems)
            {
                // Get item info.
                var item = (SpaceItem) Manager.GetComponent(itemId, Item.TypeId);
                hasher.Write(item.DrawBelowParent);
                hasher.Write(item.ModelOffset);
                hasher.Write(item.RequiredSlotSize.Scale());
                var renderer = (TextureRenderer) Manager.GetComponent(item.Entity, TextureRenderer.TypeId);
                hasher.Write(renderer.TextureName);
                hasher.Write(renderer.Tint);
            }

            return hasher.Value;
        }

        /// <summary>
        ///     Computes the size of the model, i.e. the area covered by the fully rendered equipment starting with the
        ///     specified slot.
        /// </summary>
        private RectangleF ComputeModelSize(
            SpaceItemSlot slot, Vector2 offset, ItemSlotSize parentSize = ItemSlotSize.Small, bool? mirrored = null)
        {
            // Nothing to do if there's no item in the slot.
            if (slot.Item <= 0)
            {
                return RectangleF.Empty;
            }

            // Get item info.
            var item = (SpaceItem) Manager.GetComponent(slot.Item, Item.TypeId);

            // Get renderer and load texture if necessary.
            var renderer = (TextureRenderer) Manager.GetComponent(item.Entity, TextureRenderer.TypeId);
            if (renderer.Texture == null)
            {
                var graphicsSystem = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                renderer.Texture = graphicsSystem.Content.Load<Texture2D>(renderer.TextureName);
            }

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
            else if (slotOffset.Y != 0.0f)
            {
                mirrored = slot.Offset.Y < 0;
            }
            if (mirrored.HasValue && mirrored.Value)
            {
                itemOffset.Y = -itemOffset.Y;
            }

            // Move the offset according to rotation and accumulate it.
            var localOffset = offset + parentSize.Scale() * slotOffset;
            var renderOffset = localOffset + item.RequiredSlotSize.Scale() * itemOffset;

            // Get area the texture is rendered to.
            var bounds = (RectangleF) renderer.Texture.Bounds;
            bounds.Offset(-bounds.Width / 2, -bounds.Height / 2);
            bounds.Offset(renderOffset);
            bounds.Inflate(
                -renderer.Texture.Width / 2f * (1 - item.RequiredSlotSize.Scale()),
                -renderer.Texture.Height / 2f * (1 - item.RequiredSlotSize.Scale()));

            // Check sub-items.
            foreach (SpaceItemSlot childSlot in Manager.GetComponents(item.Entity, ItemSlot.TypeId))
            {
                bounds = RectangleF.Union(
                    bounds, ComputeModelSize(childSlot, localOffset, item.RequiredSlotSize, mirrored));
            }

            return bounds;
        }

        /// <summary>Renders the equipment starting from the specified slot.</summary>
        private void RenderEquipment(
            SpaceItemSlot slot,
            Vector2 offset,
            int depth = 1,
            float order = 0.5f,
            ItemSlotSize parentSize = ItemSlotSize.Small,
            bool? mirrored = null)
        {
            // Nothing to do if there's no item in the slot.
            if (slot.Item <= 0)
            {
                return;
            }

            // Get item info.
            var item = (SpaceItem) Manager.GetComponent(slot.Item, Item.TypeId);

            // Get renderer and load texture if necessary.
            var renderer = (TextureRenderer) Manager.GetComponent(item.Entity, TextureRenderer.TypeId);
            if (renderer.Texture == null)
            {
                var graphicsSystem = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                renderer.Texture = graphicsSystem.Content.Load<Texture2D>(renderer.TextureName);
            }

            // Adjust depth we want to render at.
            order -= (item.DrawBelowParent ? -0.25f : 0.25f) / depth;

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
            else if (slotOffset.Y != 0.0f)
            {
                mirrored = slot.Offset.Y < 0;
            }
            if (mirrored.HasValue && mirrored.Value)
            {
                itemOffset.Y = -itemOffset.Y;
            }

            // Move the offset according to rotation and accumulate it.
            var localOffset = offset + parentSize.Scale() * slotOffset;
            var renderOffset = localOffset + item.RequiredSlotSize.Scale() * itemOffset;

            // Get center of texture.
            Vector2 origin;
            origin.X = renderer.Texture.Width / 2f;
            origin.Y = renderer.Texture.Height / 2f;

            // And render.
            SpriteBatch.Draw(
                renderer.Texture,
                renderOffset,
                null,
                renderer.Tint,
                0,
                origin,
                item.RequiredSlotSize.Scale(),
                mirrored.HasValue && mirrored.Value ? SpriteEffects.FlipVertically : SpriteEffects.None,
                order);

            // Render sub-items.
            foreach (SpaceItemSlot childSlot in Manager.GetComponents(item.Entity, ItemSlot.TypeId))
            {
                RenderEquipment(childSlot, localOffset, depth + 1, order, item.RequiredSlotSize, mirrored);
            }
        }

        /// <summary>Gets the list of currently visible entities.</summary>
        /// <returns>The list of visible entities.</returns>
        protected override IEnumerable<int> GetVisibleEntities()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).VisibleEntities;
        }

        /// <summary>
        ///     Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected override Matrix GetTransform()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Transform;
        }

        /// <summary>
        ///     Returns the <em>translation</em> for globally offsetting rendered content.
        /// </summary>
        /// <returns>The translation.</returns>
        protected override FarPosition GetTranslation()
        {
            return ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId)).Translation;
        }

        #endregion
    }
}