using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system is used to draw boxes representing the collision bounds of entities.
    /// </summary>
    public sealed class DebugCollisionBoundsRenderSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this system, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should perform
        /// updates and react to events.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The renderer we use to render our bounds for box collidables.
        /// </summary>
        private static AbstractShape _boxShape;

        /// <summary>
        /// The renderer we use to render our bounds for spherical collidables.
        /// </summary>
        private static AbstractShape _sphereShape;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components when updating, to avoid
        /// modifications to the list of components breaking the update.
        /// </summary>
        private ISet<int> _collidablesInView = new HashSet<int>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugCollisionBoundsRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="graphics">The graphics.</param>
        public DebugCollisionBoundsRenderSystem(ContentManager content, GraphicsDevice graphics)
        {
            if (_boxShape == null)
            {
                _boxShape = new FilledRectangle(content, graphics);
            }
            if (_sphereShape == null)
            {
                _sphereShape = new FilledEllipse(content, graphics);
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws all collidable bounds in the viewport.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Get all renderable entities in the viewport.
            var view = camera.ComputeVisibleBounds(_boxShape.GraphicsDevice.Viewport);
            ((IndexSystem)Manager.GetSystem(IndexSystem.TypeId)).Find(ref view, ref _collidablesInView, CollisionSystem.IndexGroupMask);

            // Skip there rest if nothing is visible.
            if (_collidablesInView.Count == 0)
            {
                return;
            }

            // Set/get loop invariants.
            var translation = camera.Transform.Translation;
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);
            _boxShape.Transform = camera.Transform.Matrix;
            _sphereShape.Transform = camera.Transform.Matrix;

            // Iterate over all visible collidables.
            foreach (var entity in _collidablesInView)
            {
                var component = (Collidable)Manager.GetComponent(entity, Collidable.TypeId);

                // See what type of collidable we have.
                AbstractShape shape;
                if (component is CollidableBox)
                {
                    shape = _boxShape;
                }
                else if (component is CollidableSphere)
                {
                    shape = _sphereShape;
                }
                else
                {
                    throw new InvalidOperationException("Trying to render unknown collidable type.");
                }

                // Set color based on collidable state.
                if (component.Enabled)
                {
                    switch (component.State)
                    {
                        case Collidable.CollisionState.None:
                            shape.Color = Color.Green;
                            break;
                        case Collidable.CollisionState.HasNeighbors:
                            shape.Color = Color.Blue;
                            break;
                        case Collidable.CollisionState.HasCollidableNeighbors:
                            shape.Color = Color.Yellow;
                            break;
                        case Collidable.CollisionState.Collides:
                            shape.Color = Color.DarkRed;
                            break;
                    }
                }
                else
                {
                    shape.Color = Color.Gray;
                }
                shape.Color *= 0.25f;
                shape.BlendState = BlendState.Additive;

                // Get interpolated position.
                FarPosition position;
                interpolation.GetInterpolatedPosition(entity, out position);

                // Get the bounds, translate them and get a "normal" rectangle.
                var bounds = component.ComputeBounds();
                bounds.Offset(position + translation - bounds.Center);
                var relativeBounds = (Microsoft.Xna.Framework.Rectangle)bounds;
                
                // Set renderer parameters and draw.
                shape.SetCenter(relativeBounds.Center.X, relativeBounds.Center.Y);
                shape.SetSize(relativeBounds.Width, relativeBounds.Height);
                shape.Draw();
            }

            // Clear for next iteration.
            _collidablesInView.Clear();
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
