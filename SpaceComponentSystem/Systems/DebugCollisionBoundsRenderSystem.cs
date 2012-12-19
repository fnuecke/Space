using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system is used to draw boxes representing the collision bounds of entities.
    /// </summary>
    public sealed class DebugCollisionBoundsRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
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
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The renderer we use to render our bounds for box collidables.
        /// </summary>
        private AbstractShape _boxShape;

        /// <summary>
        /// The renderer we use to render our bounds for spherical collidables.
        /// </summary>
        private AbstractShape _sphereShape;

        #endregion

        #region Logic

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    if (_boxShape == null)
                    {
                        _boxShape = new FilledRectangle(cm.Value.Content, cm.Value.Graphics);
                        _boxShape.LoadContent();
                    }
                    if (_sphereShape == null)
                    {
                        _sphereShape = new FilledEllipse(cm.Value.Content, cm.Value.Graphics);
                        _sphereShape.LoadContent();
                    }
                }
            }
        }

        /// <summary>
        /// Draws all collidable bounds in the viewport.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Set/get loop invariants.
            var translation = camera.Transform.Translation;
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);
            _boxShape.Transform = camera.Transform.Matrix;
            _sphereShape.Transform = camera.Transform.Matrix;

            // Iterate over all visible collidables.
            foreach (var entity in ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId)).VisibleEntities)
            {
                var component = (Collidable)Manager.GetComponent(entity, Collidable.TypeId);
                if (component == null)
                {
                    continue;
                }

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
                        case Collidable.CollisionState.Contact:
                            shape.Color = Color.Blue;
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
                shape.Color *= 0.6f;
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
