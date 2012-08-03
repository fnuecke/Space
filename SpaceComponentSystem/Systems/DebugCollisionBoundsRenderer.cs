using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    public sealed class DebugCollisionBoundsRenderer : AbstractComponentSystem<Collidable>
    {
        #region Fields

        /// <summary>
        /// The renderer we use to render our bounds.
        /// </summary>
        private static AbstractShape _shape;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private ICollection<int> _collidablesInView = new List<int>();

        #endregion

        public DebugCollisionBoundsRenderer(ContentManager content, GraphicsDevice graphics)
        {
            if (_shape == null)
            {
                _shape = new FilledRectangle(content, graphics);
            }
        }

        public override void Draw(long frame)
        {
            var camera = ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId));

            // Get all renderable entities in the viewport.
            var view = camera.ComputeVisibleBounds(_shape.GraphicsDevice.Viewport);
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _collidablesInView, CollisionSystem.IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_collidablesInView.Count == 0)
            {
                return;
            }

            // Set/get loop invariants.
            var translation = camera.Transform.Translation;
            _shape.Transform = camera.Transform.Matrix;

            foreach (var entity in _collidablesInView)
            {
                var component = ((Collidable)Manager.GetComponent(entity, Collidable.TypeId));

                _shape.Color = (component.Enabled ? Color.Blue : Color.Gray) * 0.25f;

                var transform = ((Transform)Manager.GetComponent(component.Entity, Transform.TypeId));
                var bounds = component.ComputeBounds();
                bounds.Offset(transform.Translation + translation - bounds.Center);
                var relativeBounds = (Microsoft.Xna.Framework.Rectangle)bounds;

                _shape.SetCenter(relativeBounds.Center.X, relativeBounds.Center.Y);
                _shape.SetSize(relativeBounds.Width, relativeBounds.Height);
                _shape.Draw();
            }

            _collidablesInView.Clear();
        }
    }
}
