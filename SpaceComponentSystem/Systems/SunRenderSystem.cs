using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Graphics;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders suns.
    /// </summary>
    public sealed class SunRenderSystem : AbstractComponentSystem<SunRenderer>, IDrawingSystem
    {
        #region Fields

        /// <summary>
        /// The sun renderer we use.
        /// </summary>
        private static Sun _sun;

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        public bool IsEnabled { get; set; }

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
        /// Initializes a new instance of the <see cref="SunRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        /// <param name="spriteBatch">The sprite batch to use for rendering.</param>
        public SunRenderSystem(ContentManager content, GraphicsDevice graphics, SpriteBatch spriteBatch)
        {
            if (_sun == null)
            {
                _sun = new Sun(content, graphics);
                _sun.LoadContent(spriteBatch, content);
            }

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
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Get all renderable entities in the viewport.
            var view = camera.ComputeVisibleBounds(_sun.GraphicsDevice.Viewport);
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, TextureRenderSystem.IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count == 0)
            {
                return;
            }

            // Set/get loop invariants.
            var transform = camera.Transform;
            _sun.Time = frame / Settings.TicksPerSecond;

            // Render everything in sight.
            foreach (var entity in _drawablesInView)
            {
                var component = (SunRenderer)Manager.GetComponent(entity, SunRenderer.TypeId);

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    RenderSun(component, ref transform);
                }
            }

            _drawablesInView.Clear();
        }

        /// <summary>
        /// Renders the specified sun.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="transform">The transform.</param>
        private void RenderSun(SunRenderer component, ref FarTransform transform)
        {
            // Get absolute position of sun.
            var position = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId)).Translation;

            // Apply transformation.
            _sun.Center = (Vector2)(position + transform.Translation);
            _sun.SetTransform(transform.Matrix);

            // Set remaining parameters for draw.
            _sun.SetSize(component.Radius * 2);
            _sun.SurfaceRotation = component.SurfaceRotation;
            _sun.PrimaryTurbulenceRotation = component.PrimaryTurbulenceRotation;
            _sun.SecondaryTurbulenceRotation = component.SecondaryTurbulenceRotation;

            // And draw it.
            _sun.Draw();
        }

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
