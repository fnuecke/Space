using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders planets.
    /// </summary>
    public sealed class PlanetRenderSystem : AbstractComponentSystem<PlanetRenderer>
    {
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
        private ICollection<int> _drawablesInView = new HashSet<int>();

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
        }

        #endregion

        #region Logic

        /// <summary>
        /// Load surface texture if necessary.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, PlanetRenderer component)
        {
            if (component.Texture == null)
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }
        }

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Draw(long frame)
        {
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Get all renderable entities in the viewport.
            var view = camera.ComputeVisibleBounds(_planet.GraphicsDevice.Viewport);
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _drawablesInView, CullingTextureRenderSystem.IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count == 0)
            {
                return;
            }

            // Set/get loop invariants.
            var translation = camera.GetTranslation();
            var zoom = camera.Zoom;
            _planet.Time = frame;
            _planet.Scale = zoom;
            
            // Iterate over the shorter list.
            if (_drawablesInView.Count < Components.Count)
            {
                foreach (var entity in _drawablesInView)
                {
                    var component = Manager.GetComponent<PlanetRenderer>(entity);

                    // Skip invalid or disabled entities.
                    if (component != null && component.Enabled)
                    {
                        RenderPlanet(component, ref translation);
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
                        RenderPlanet(component, ref translation);
                    }
                }
            }

            _drawablesInView.Clear();
        }

        private void RenderPlanet(PlanetRenderer component, ref Vector2 translation)
        {
            // The position and orientation we're rendering at and in.
            var transform = Manager.GetComponent<Transform>(component.Entity);

            // Get position relative to our sun, to rotate atmosphere and shadow.
            var toSun = Vector2.Zero;
            var sun = GetSun(component.Entity);
            if (sun > 0)
            {
                var sunTransform = Manager.GetComponent<Transform>(sun);
                if (sunTransform != null)
                {
                    toSun = sunTransform.Translation - transform.Translation;
                    var matrix = Matrix.CreateRotationZ(-transform.Rotation);
                    Vector2.Transform(ref toSun, ref matrix, out toSun);
                    toSun.Normalize();
                }
            }

            // Set parameters and draw.
            _planet.Center = transform.Translation + translation;
            _planet.Rotation = transform.Rotation;
            _planet.SetSize(component.Radius * 2);
            _planet.SurfaceTexture = component.Texture;
            _planet.SurfaceTint = component.PlanetTint;
            _planet.AtmosphereTint = component.AtmosphereTint;
            _planet.SurfaceRotation = component.SurfaceRotation;
            _planet.LightDirection = toSun;
            _planet.Draw();
        }

        /// <summary>
        /// Utility method to find the sun we're rotating around.
        /// </summary>
        /// <returns></returns>
        private int GetSun(int entity)
        {
            var sun = 0;
            var ellipse = Manager.GetComponent<EllipsePath>(entity);
            while (ellipse != null)
            {
                sun = ellipse.CenterEntityId;
                ellipse = Manager.GetComponent<EllipsePath>(sun);
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
        /// Servers as a copy constructor that returns a new instance of the same
        /// type that is freshly initialized.
        /// 
        /// <para>
        /// This takes care of duplicating reference types to a new copy of that
        /// type (e.g. collections).
        /// </para>
        /// </summary>
        /// <returns>A cleared copy of this system.</returns>
        public override AbstractSystem NewInstance()
        {
            var copy = (PlanetRenderSystem)base.NewInstance();

            copy._drawablesInView = new HashSet<int>();

            return copy;
        }

        #endregion
    }
}
