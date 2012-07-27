using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Systems
{
    public abstract class CullingTextureRenderSystem : TextureRenderSystem
    {
        #region Constants

        /// <summary>
        /// Index group mask for the index we use to track positions of renderables.
        /// </summary>
        public static readonly ulong IndexGroupMask = 1ul << IndexSystem.GetGroup();

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private ICollection<int> _drawablesInView = new HashSet<int>();

        #endregion

        #region Constructor

        protected CullingTextureRenderSystem(ContentManager content, SpriteBatch spriteBatch)
            : base(content, spriteBatch)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Loops over all components and calls <c>DrawComponent()</c>.
        /// </summary>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Draw(long frame)
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
                    var component = Manager.GetComponent<TextureRenderer>(entity);

                    // Skip invalid or disabled entities.
                    if (component != null && component.Enabled)
                    {
                        DrawComponent(frame, component);
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
                        DrawComponent(frame, component);
                    }
                }
            }

            // Clear for next iteration.
            _drawablesInView.Clear();
        }

        /// <summary>
        /// Returns the current bounds of the viewport, i.e. the rectangle of
        /// the world to actually render.
        /// </summary>
        protected abstract Rectangle ComputeViewport();

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
            var copy = (CullingTextureRenderSystem)base.NewInstance();

            copy._drawablesInView = new HashSet<int>();

            return copy;
        }

        #endregion
    }
}
