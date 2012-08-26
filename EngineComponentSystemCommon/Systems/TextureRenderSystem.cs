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
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

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
        protected readonly ContentManager Content;

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
            Content = content;
            SpriteBatch = spriteBatch;
            IsEnabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Get all renderable entities in the viewport.
            var view = ComputeViewport();
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count > 0)
            {
                // Get the interpolation system for interpolated positions.
                var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

                // Get the transformation to use.
                var cameraTransform = GetTransform();

                // Begin rendering.
                SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, cameraTransform.Matrix);

                foreach (var entity in _drawablesInView)
                {
                    var component = ((TextureRenderer)Manager.GetComponent(entity, TextureRenderer.TypeId));

                    // Skip invalid or disabled entities.
                    if (component != null && component.Enabled)
                    {
                        BeginDrawComponent(component, cameraTransform.Translation, interpolation);
                    }
                }

                // Done rendering.
                SpriteBatch.End();

                // Clear for next iteration.
                _drawablesInView.Clear();
            }
        }

        /// <summary>
        /// Prepares for drawing the component. Computes screen space coordinates and then calls DrawComponent.
        /// </summary>
        /// <param name="component">The component to draw.</param>
        /// <param name="translation">The camera translation.</param>
        /// <param name="interpolation">The interpolation system to get position and rotation from.</param>
        private void BeginDrawComponent(TextureRenderer component, FarPosition translation, InterpolationSystem interpolation)
        {
            // Load the texture if it isn't already.
            if (component.Texture == null)
            {
                component.Texture = Content.Load<Texture2D>(component.TextureName);
            }

            // Get interpolated position.
            FarPosition position;
            interpolation.GetInterpolatedPosition(component.Entity, out position);
            float rotation;
            interpolation.GetInterpolatedRotation(component.Entity, out rotation);

            // Get parallax layer.
            var parallax = (Parallax)Manager.GetComponent(component.Entity, Parallax.TypeId);
            var layer = 1.0f;
            if (parallax != null)
            {
                layer = parallax.Layer;
            }

            // Draw.
            DrawComponent(component, ((Vector2)(position + translation)) * layer, rotation);
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        protected virtual void DrawComponent(TextureRenderer component, Vector2 position, float rotation)
        {
            // Get the rectangle at which we'll draw.
            Vector2 origin;
            origin.X = component.Texture.Width / 2f;
            origin.Y = component.Texture.Height / 2f;

            SpriteBatch.Draw(component.Texture, position, null, component.Tint, rotation, origin, component.Scale, SpriteEffects.None, 0);
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
