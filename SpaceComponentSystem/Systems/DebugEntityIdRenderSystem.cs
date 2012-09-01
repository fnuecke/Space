using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders entity ids at their position, if they have a position.
    /// </summary>
    public sealed class DebugEntityIdRenderSystem : AbstractSystem, IDrawingSystem
    {
        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The spritebatch to use for rendering.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The font to use for rendering.
        /// </summary>
        private readonly SpriteFont _font;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private ISet<int> _entitiesInView = new HashSet<int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugEntityIdRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        public DebugEntityIdRenderSystem(ContentManager content, SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;
            _font = content.Load<SpriteFont>("Fonts/ConsoleFont");
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId));

            // Get all renderable entities in the viewport.
            var view = camera.ComputeVisibleBounds(_spriteBatch.GraphicsDevice.Viewport);
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _entitiesInView, InterpolationSystem.IndexGroupMask);

            // Get camera transform.
            var transform = camera.Transform;
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            // Iterate over all visible entities.
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transform.Matrix);
            foreach (var entity in _entitiesInView)
            {
                var component = (Transform)Manager.GetComponent(entity, Transform.TypeId);
                if (component != null)
                {
                    FarPosition position;
                    interpolation.GetInterpolatedPosition(component.Entity, out position);
                    position += transform.Translation;
                    _spriteBatch.DrawString(_font, "ID: " + component.Entity, (Vector2)position, Color.White);
                }
            }
            _spriteBatch.End();

            // Clear for next iteration.
            _entitiesInView.Clear();
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
