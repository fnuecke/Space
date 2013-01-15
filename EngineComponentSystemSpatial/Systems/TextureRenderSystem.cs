using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>
    ///     Basic implementation of a render system. Subclasses may override the GetTranslation() method to implement
    ///     camera positioning.
    /// </summary>
    public abstract class TextureRenderSystem
        : AbstractComponentSystem<TextureRenderer>, IDrawingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The sprite batch to render textures into.</summary>
        protected SpriteBatch SpriteBatch;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="TextureRenderSystem"/> class.
        /// </summary>
        protected TextureRenderSystem()
        {
            Enabled = true;
        }

        #endregion

        #region Logic

        /// <summary>Handle a message of the specified type.</summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    LoadContent(cm.Value.Content, cm.Value.Graphics);
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    UnloadContent();
                }
            }
            {
                var cm = message as GraphicsDeviceReset?;
                if (cm != null)
                {
                    UnloadContent();
                    LoadContent(cm.Value.Content, cm.Value.Graphics);
                }
            }
        }

        /// <summary>Called when the graphics device has been (re)created, and assets should be loaded.</summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device service.</param>
        protected virtual void LoadContent(ContentManager content, IGraphicsDeviceService graphics)
        {
            SpriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            foreach (var component in Components)
            {
                if (!string.IsNullOrWhiteSpace(component.TextureName))
                {
                    component.Texture = content.Load<Texture2D>(component.TextureName);
                }
            }
        }

        /// <summary>Called when the graphics device is being disposed, and any assets manually allocated should be disposed.</summary>
        protected virtual void UnloadContent()
        {
            if (SpriteBatch != null)
            {
                SpriteBatch.Dispose();
                SpriteBatch = null;
            }
        }

        /// <summary>
        ///     Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public virtual void Draw(long frame, float elapsedMilliseconds)
        {
            // Get the interpolation system for interpolated positions.
            var interpolation = (InterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);

            // Get the transformation to use.
            var cameraTransform = GetTransform();
            var cameraTranslation = GetTranslation();

            // Begin rendering.
            SpriteBatch.Begin(
                SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, cameraTransform);

            // We increment the base depth for each component we render, as a tie breaker,
            // i.e. to avoid z-fighting.
            var layerDepth = 0f;
            foreach (var entity in GetVisibleEntities())
            {
                var component = ((TextureRenderer) Manager.GetComponent(entity, TextureRenderer.TypeId));

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    BeginDrawComponent(component, cameraTranslation, interpolation, layerDepth);
                    layerDepth += 0.00001f;
                }
            }

            // Done rendering.
            SpriteBatch.End();
        }

        /// <summary>Prepares for drawing the component. Computes screen space coordinates and then calls DrawComponent.</summary>
        /// <param name="component">The component to draw.</param>
        /// <param name="translation">The camera translation.</param>
        /// <param name="interpolation">The interpolation system to get position and rotation from.</param>
        /// <param name="layerDepth">The base layer depth to render at.</param>
        private void BeginDrawComponent(
            TextureRenderer component, WorldPoint translation, InterpolationSystem interpolation, float layerDepth)
        {
            // Load the texture if it isn't already.
            if (component.Texture == null && !string.IsNullOrWhiteSpace(component.TextureName))
            {
                var graphicsSystem = ((GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId));
                component.Texture = graphicsSystem.Content.Load<Texture2D>(component.TextureName);
            }

            // Get interpolated position.
            WorldPoint position;
            interpolation.GetInterpolatedPosition(component.Entity, out position);
            float rotation;
            interpolation.GetInterpolatedRotation(component.Entity, out rotation);

            // Get parallax layer.
            var parallax = (Parallax) Manager.GetComponent(component.Entity, Parallax.TypeId);
            var layer = 1.0f;
            if (parallax != null)
            {
                layer = parallax.Layer;
            }

            // Draw.
            DrawComponent(component, ((Vector2) (position + translation)) * layer, rotation, layerDepth);
        }

        /// <summary>Draws the component.</summary>
        /// <param name="component">The component.</param>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="layerDepth">The base layer depth to render at.</param>
        protected virtual void DrawComponent(
            TextureRenderer component, Vector2 position, float rotation, float layerDepth)
        {
            // Get the rectangle at which we'll draw.
            Vector2 origin;
            origin.X = component.Texture.Width / 2f;
            origin.Y = component.Texture.Height / 2f;

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

        /// <summary>Gets the list of currently visible entities.</summary>
        /// <returns>The list of visible entities.</returns>
        protected virtual IEnumerable<int> GetVisibleEntities()
        {
            return Components.Select(component => component.Entity);
        }

        /// <summary>
        ///     Returns the <em>transformation</em> for locally offsetting and scaling rendered content.
        /// </summary>
        /// <returns>The transformation.</returns>
        protected abstract Matrix GetTransform();

        /// <summary>
        ///     Returns the <em>translation</em> for globally offsetting rendered content.
        /// </summary>
        /// <returns>The translation.</returns>
        protected abstract WorldPoint GetTranslation();

        #endregion
    }
}