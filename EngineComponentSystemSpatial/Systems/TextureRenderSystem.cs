using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
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
    [Packetizable(false)]
    public abstract class TextureRenderSystem : AbstractComponentSystem<IDrawable>, IDrawingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>Interface type id, get once for better performance.</summary>
        private static readonly int DrawableTypeId = ComponentSystem.Manager.GetComponentTypeId<IDrawable>();

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
            SpriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, null, null, null, cameraTransform);

            // We increment the base depth for each component we render, as a tie breaker,
            // i.e. to avoid z-fighting.
            var layerDepth = 0f;
            foreach (IDrawable drawable in GetVisibleEntities()
                // Get all drawable components of the entity.
                .SelectMany(entity => Manager.GetComponents(entity, DrawableTypeId))
                // Skip disabled components.
                .Where(drawable => drawable.Enabled))
            {
                // Get interpolated position.
                WorldPoint position;
                float angle;
                interpolation.GetInterpolatedTransform(drawable.Entity, out position, out angle);

                // Get parallax layer.
                var parallax = (Parallax) Manager.GetComponent(drawable.Entity, Parallax.TypeId);
                var parallaxLayer = 1.0f;
                if (parallax != null)
                {
                    parallaxLayer = parallax.Layer;
                }

                // Draw.
                drawable.Draw(
                    SpriteBatch,
                    ((Vector2) (position + cameraTranslation)) * parallaxLayer,
                    angle,
                    UnitConversion.ToSimulationUnits(parallaxLayer),
                    SpriteEffects.None,
                    // HACK not entirely sure why the "shrinking" of the values is necessary... value range should be [0,1], but whatever.
                    UnitConversion.ToSimulationUnits(layerDepth + (1f - parallaxLayer)));

                // Tie breaker.
                layerDepth += 0.00001f;
            }

            // Done rendering.
            SpriteBatch.End();
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

        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            LoadContent(((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content, message.Graphics);
        }

        [MessageCallback]
        public void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
        {
            UnloadContent();
        }

        [MessageCallback]
        public void OnGraphicsDeviceReset(GraphicsDeviceReset message)
        {
            UnloadContent();
            LoadContent(((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content, message.Graphics);
        }

        /// <summary>Called when the graphics device has been (re)created, and assets should be loaded.</summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device service.</param>
        protected virtual void LoadContent(ContentManager content, IGraphicsDeviceService graphics)
        {
            SpriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            foreach (var component in Components)
            {
                component.LoadContent(content, graphics);
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

        #endregion
    }
}