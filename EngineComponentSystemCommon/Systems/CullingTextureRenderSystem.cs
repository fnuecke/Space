using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Systems
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
        /// <param name="gameTime">Time elapsed since the last call to Draw.</param>
        /// <param name="frame">The frame in which the update is applied.</param>
        public override void Draw(GameTime gameTime, long frame)
        {
            // Get all renderable entities in the viewport.
            var view = ComputeViewport();
            Manager.GetSystem<IndexSystem>().Find(ref view, ref _drawablesInView, IndexGroupMask);

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
                        DrawComponent(gameTime, frame, component);
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
                        DrawComponent(gameTime, frame, component);
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
        /// Creates a deep copy, with a component list only containing
        /// clones of components not bound to an entity. If possible, the
        /// specified instance will be reused.
        /// 
        /// <para>
        /// Subclasses must take care of duplicating reference types, to complete
        /// the deep-copy of the object. Caches, i.e. lists / dictionaries / etc.
        /// to quickly look up components must be reset / rebuilt.
        /// </para>
        /// </summary>
        /// <returns>A deep, with a semi-cleared copy of this system.</returns>
        public override AbstractSystem DeepCopy(AbstractSystem into)
        {
            // Get something to start with.
            var copy = (CullingTextureRenderSystem)base.DeepCopy(into);

            if (copy != into)
            {
                copy._drawablesInView = new HashSet<int>();
            }

            return copy;
        }

        #endregion
    }
}
