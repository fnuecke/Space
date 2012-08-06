using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Basic implementation of a render system. Subclasses may override the
    /// GetTranslation() method to implement camera positioning.
    /// </summary>
    public abstract class TextureRenderSystem : AbstractComponentSystem<TextureRenderer>, IDrawingSystem
    {
        #region Constants

        /// <summary>
        /// Index group mask for the index we use to track positions of renderables.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The sprite batch to render textures into.
        /// </summary>
        protected readonly SpriteBatch SpriteBatch;

        /// <summary>
        /// The content manager used to load textures.
        /// </summary>
        private readonly ContentManager _content;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private ISet<int> _drawablesInView = new HashSet<int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        protected TextureRenderSystem(ContentManager content, SpriteBatch spriteBatch)
        {
            _content = content;
            SpriteBatch = spriteBatch;
            IsEnabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public void Draw(long frame)
        {
            // Get all renderable entities in the viewport.
            var view = ComputeViewport();
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count == 0)
            {
                return;
            }

            // Iterate over the shorter list.
            if (_drawablesInView.Count < Components.Count)
            {
                foreach (var entity in _drawablesInView)
                {
                    var component = ((TextureRenderer)Manager.GetComponent(entity, TextureRenderer.TypeId));

                    // Skip invalid or disabled entities.
                    if (component != null && component.Enabled)
                    {
                        DrawComponent(component);
                    }
                }
            }
            else
            {
                foreach (var component in Components)
                {
                    // Skip disabled or invisible entities.
                    if (component.Enabled && _drawablesInView.Contains(component.Entity))
                    {
                        DrawComponent(component);
                    }
                }
            }

            // Clear for next iteration.
            _drawablesInView.Clear();
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="component">The component.</param>
        private void DrawComponent(TextureRenderer component)
        {
            // Load the texture if it isn't already.
            if (component.Texture == null)
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }

            // Draw the texture based on its position.
            var transform = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId));

            // Get parallax layer.
            var parallax = (Parallax)Manager.GetComponent(component.Entity, Parallax.TypeId);
            var layer = 1.0f;
            if (parallax != null)
            {
                layer = parallax.Layer;
            }

            // Get the rectangle at which we'll draw.
            Vector2 origin;
            origin.X = component.Texture.Width / 2f;
            origin.Y = component.Texture.Height / 2f;

            // Get the transformation to use.
            var cameraTransform = GetTransform();

            // Draw. Use transformation of the camera for sprites drawn, so no transformation
            // for the sprite itself is needed.
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, cameraTransform.Matrix);
            SpriteBatch.Draw(component.Texture, ((Vector2)(transform.Translation + cameraTransform.Translation)) * layer, null, component.Tint, transform.Rotation, origin, component.Scale, SpriteEffects.None, 0);
            SpriteBatch.End();
        }

        /// <summary>
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        protected abstract FarRectangle ComputeViewport();

        /// <summary>
        /// Returns the <em>transformation</em> for offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected abstract FarTransform GetTransform();

        #endregion

        #region Serialization

        /// <summary>
        /// We're purely visual, so don't hash anything.
        /// </summary>
        /// <param name="hasher">The hasher to use.</param>
        public override void Hash(Hasher hasher)
        {
        }

        #endregion

        #region Copying

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override AbstractSystem NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override void CopyInto(AbstractSystem into)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
