using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders suns.
    /// </summary>
    public sealed class SunRenderSystem : AbstractComponentSystem<SunRenderer>
    {
        #region Fields

        /// <summary>
        /// The sun renderer we use.
        /// </summary>
        private static Sun _sun;

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
        /// Initializes a new instance of the <see cref="SunRenderSystem"/> class.
        /// </summary>
        /// <param name="game">The game to create the system for.</param>
        /// <param name="spriteBatch">The sprite batch to use for rendering.</param>
        public SunRenderSystem(Game game, SpriteBatch spriteBatch)
        {
            if (_sun == null)
            {
                _sun = new Sun(game);
                _sun.LoadContent(spriteBatch, game.Content);
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Draw(long frame)
        {
            var camera = Manager.GetSystem<CameraSystem>();

            // Get all renderable entities in the viewport.
            var view = camera.ComputeVisibleBounds(_sun.GraphicsDevice.Viewport);
            Manager.GetSystem<IndexSystem>().Find(ref view, ref _drawablesInView, CullingTextureRenderSystem.IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_drawablesInView.Count == 0)
            {
                return;
            }

            // Set/get loop invariants.
            var translation = camera.GetTranslation();
            _sun.Time = frame;
            _sun.Scale = camera.Zoom;

            // Iterate over the shorter list.
            if (_drawablesInView.Count < Components.Count)
            {
                foreach (var entity in _drawablesInView)
                {
                    var component = Manager.GetComponent<SunRenderer>(entity);

                    // Skip invalid or disabled entities.
                    if (component != null && component.Enabled)
                    {
                        RenderSun(component, ref translation);
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
                        RenderSun(component, ref translation);
                    }
                }
            }

            _drawablesInView.Clear();
        }

        private void RenderSun(SunRenderer component, ref Vector2 translation)
        {
            var transform = Manager.GetComponent<Transform>(component.Entity);

            _sun.SetSize(component.Radius * 2);
            _sun.Center = transform.Translation + translation;
            _sun.SurfaceRotation = component.SurfaceRotation;
            _sun.PrimaryTurbulenceRotation = component.PrimaryTurbulenceRotation;
            _sun.SecondaryTurbulenceRotation = component.SecondaryTurbulenceRotation;
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
            var copy = (SunRenderSystem)base.NewInstance();

            copy._drawablesInView = new HashSet<int>();

            return copy;
        }

        #endregion
    }
}
