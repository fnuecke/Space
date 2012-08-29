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
    /// Renders planets.
    /// </summary>
    public sealed class PlanetRenderSystem : AbstractComponentSystem<PlanetRenderer>, IDrawingSystem
    {
        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        public bool IsEnabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The renderer we use to render our planet.
        /// </summary>
        private static Planet _planet;

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
        /// Initializes a new instance of the <see cref="PlanetRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        public PlanetRenderSystem(ContentManager content, GraphicsDevice graphics)
        {
            _content = content;
            if (_planet == null)
            {
                _planet = new Planet(content, graphics);
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
            var view = camera.ComputeVisibleBounds(_planet.GraphicsDevice.Viewport);
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, TextureRenderSystem.IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count == 0)
            {
                return;
            }

            // Set/get loop invariants.
            var translation = camera.Transform.Translation;
            _planet.Transform = camera.Transform.Matrix;
            _planet.Time = frame / Settings.TicksPerSecond;
            
            // Draw everything in view.
            foreach (var entity in _drawablesInView)
            {
                var component = (PlanetRenderer)Manager.GetComponent(entity, PlanetRenderer.TypeId);

                // Skip invalid or disabled entities.
                if (component != null && component.Enabled)
                {
                    RenderPlanet(component, ref translation);
                }
            }

            _drawablesInView.Clear();
        }

        /// <summary>
        /// Renders a single planet.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="translation">The translation.</param>
        private void RenderPlanet(PlanetRenderer component, ref FarPosition translation)
        {
            // Load the texture if we don't have it yet.
            if (component.Texture == null && !string.IsNullOrWhiteSpace(component.TextureName))
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }
            if (component.Lights == null && !string.IsNullOrWhiteSpace(component.LightsName))
            {
                component.Lights = _content.Load<Texture2D>(component.LightsName);
            }
            if (component.Normals == null && !string.IsNullOrWhiteSpace(component.NormalsName))
            {
                component.Normals = _content.Load<Texture2D>(component.NormalsName);
            }

            // The position and orientation we're rendering at and in.
            var transform = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId));
            var position = transform.Translation;
            var rotation = transform.Rotation;

            // Get position relative to our sun, to rotate atmosphere and shadow.
            var toSun = Vector2.Zero;
            var sun = GetSun(component.Entity);
            if (sun > 0)
            {
                var sunTransform = ((Transform)Manager.GetComponent(sun, Transform.TypeId));
                if (sunTransform != null)
                {
                    toSun = (Vector2)(sunTransform.Translation - position);
                    var matrix = Matrix.CreateRotationZ(-rotation);
                    Vector2.Transform(ref toSun, ref matrix, out toSun);
                    toSun.Normalize();
                }
            }

            // Apply transformation.
            _planet.Center = (Vector2)(position + translation);

            // Set remaining parameters for draw.
            _planet.Rotation = rotation;
            _planet.SetSize(component.Radius * 2);
            _planet.SurfaceTexture = component.Texture;
            _planet.SurfaceLights = component.Lights;
            _planet.SurfaceNormals = component.Normals;
            _planet.SurfaceTint = component.PlanetTint;
            _planet.AtmosphereTint = component.AtmosphereTint;
            _planet.AtmosphereInner = component.AtmosphereInner;
            _planet.AtmosphereOuter = component.AtmosphereOuter;
            _planet.AtmosphereInnerAlpha = component.AtmosphereInnerAlpha;
            _planet.AtmosphereOuterAlpha = component.AtmosphereOuterAlpha;
            _planet.SurfaceRotation = component.SurfaceRotation;
            _planet.LightDirection = toSun;

            // And draw it.
            _planet.Draw();
        }

        /// <summary>
        /// Utility method to find the sun we're rotating around.
        /// </summary>
        /// <returns></returns>
        private int GetSun(int entity)
        {
            var sun = 0;
            var ellipse = ((EllipsePath)Manager.GetComponent(entity, EllipsePath.TypeId));
            while (ellipse != null)
            {
                sun = ellipse.CenterEntityId;
                ellipse = ((EllipsePath)Manager.GetComponent(sun, EllipsePath.TypeId));
            }
            return sun;
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
