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

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

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

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device.</param>
        protected TextureRenderSystem(ContentManager content, GraphicsDevice graphics)
        {
            Content = content;
            SpriteBatch = new SpriteBatch(graphics);
            Enabled = true;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public virtual void Draw(long frame, float elapsedMilliseconds)
        {
            // Get the interpolation system for interpolated positions.
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            // Get the transformation to use.
            var cameraTransform = GetTransform();

            // Begin rendering.
            SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, cameraTransform.Matrix);
                
            // We increment the base depth for each component we render, as a tie breaker,
            // i.e. to avoid z-fighting.
            var layerDepth = 0f;
            foreach (var entity in GetVisibleEntities())
            {
                var component = ((TextureRenderer)Manager.GetComponent(entity, TextureRenderer.TypeId));

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    BeginDrawComponent(component, cameraTransform.Translation, interpolation, layerDepth);
                    layerDepth += 0.00001f;
                }
            }

            // Done rendering.
            SpriteBatch.End();
        }

        /// <summary>
        /// Prepares for drawing the component. Computes screen space coordinates and then calls DrawComponent.
        /// </summary>
        /// <param name="component">The component to draw.</param>
        /// <param name="translation">The camera translation.</param>
        /// <param name="interpolation">The interpolation system to get position and rotation from.</param>
        /// <param name="layerDepth">The base layer depth to render at.</param>
        private void BeginDrawComponent(TextureRenderer component, FarPosition translation, InterpolationSystem interpolation, float layerDepth)
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
            DrawComponent(component, ((Vector2)(position + translation)) * layer, rotation, layerDepth);
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="layerDepth">The base layer depth to render at.</param>
        protected virtual void DrawComponent(TextureRenderer component, Vector2 position, float rotation, float layerDepth)
        {
            // Get the rectangle at which we'll draw.
            Vector2 origin;
            origin.X = component.Texture.Width / 2f;
            origin.Y = component.Texture.Height / 2f;

            SpriteBatch.Draw(component.Texture, position, null, component.Tint, rotation, origin, component.Scale, SpriteEffects.None, layerDepth);
        }

        /// <summary>
        /// Gets the list of currently visible entities.
        /// </summary>
        /// <returns>The list of visible entities.</returns>
        protected virtual IEnumerable<int> GetVisibleEntities()
        {
            foreach (var component in Components)
            {
                yield return component.Entity;
            }
        }

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
