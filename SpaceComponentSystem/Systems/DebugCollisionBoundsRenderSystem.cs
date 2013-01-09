using System;
using Engine.Collections;
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
    /// <summary>This system is used to draw boxes representing the collision bounds of entities.</summary>
    public sealed class DebugCollisionBoundsRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The renderer we use to render our bounds for box collidables.</summary>
        private AbstractShape _boxShape;

        /// <summary>The renderer we use to render our bounds for spherical collidables.</summary>
        private AbstractShape _sphereShape;

        /// <summary>The spritebatch to use for rendering.</summary>
        private SpriteBatch _spriteBatch;

        /// <summary>Arrow texture to render indication of where AI is headed.</summary>
        private Texture2D _arrow;

        /// <summary>Keeps track of active contacts, to allow rendering ongoing collisions.</summary>
        private readonly SparseArray<ContactInfo> _contacts = new SparseArray<ContactInfo>(64);

        #endregion

        #region Logic

        /// <summary>Handle a message of the specified type.</summary>
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
                    _spriteBatch = new SpriteBatch(cm.Value.Graphics.GraphicsDevice);
                    _arrow = cm.Value.Content.Load<Texture2D>("Textures/arrow");
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    if (_spriteBatch != null)
                    {
                        _spriteBatch.Dispose();
                        _spriteBatch = null;
                    }
                }
            }
            {
                var cm = message as BeginCollision?;
                if (cm != null)
                {
                    _contacts[cm.Value.ContactId] = new ContactInfo
                    {
                        PositionA = ((Transform) Manager.GetComponent(cm.Value.EntityA, Transform.TypeId)).Translation,
                        PositionB = ((Transform) Manager.GetComponent(cm.Value.EntityB, Transform.TypeId)).Translation,
                        Normal = cm.Value.Normal
                    };
                }
            }
            {
                var cm = message as EndCollision?;
                if (cm != null)
                {
                    _contacts[cm.Value.ContactId] = null;
                }
            }
        }

        /// <summary>Draws all collidable bounds in the viewport.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Set/get loop invariants.
            var translation = camera.Transform.Translation;
            var interpolation = (InterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);
            _boxShape.Transform = camera.Transform.Matrix;
            _sphereShape.Transform = camera.Transform.Matrix;

            // Iterate over all visible collidables.
            foreach (var entity in camera.VisibleEntities)
            {
                var component = (Collidable) Manager.GetComponent(entity, Collidable.TypeId);
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
                var relativeBounds = (Microsoft.Xna.Framework.Rectangle) bounds;

                // Set renderer parameters and draw.
                shape.SetCenter(relativeBounds.Center.X, relativeBounds.Center.Y);
                shape.SetSize(relativeBounds.Width, relativeBounds.Height);
                shape.Draw();
            }

            // Render all active contacts.
            var view = camera.ComputeVisibleBounds();
            _spriteBatch.Begin();
            foreach (var contact in _contacts)
            {
                // Skip entries we don't see anyway.
                if (!view.Contains(contact.PositionA) &&
                    !view.Contains(contact.PositionB))
                {
                    continue;
                }

                DrawArrow((Vector2) (contact.PositionA + translation), contact.Normal * 30, Color.Turquoise);
            }
            _spriteBatch.End();
        }

        private void DrawArrow(Vector2 start, Vector2 toEnd, Color color)
        {
            // Don't draw tiny arrows...
            if (toEnd.LengthSquared() < 1f)
            {
                return;
            }
            _spriteBatch.Draw(
                _arrow,
                start,
                null,
                color,
                (float) Math.Atan2(toEnd.Y, toEnd.X),
                new Vector2(0, _arrow.Height / 2f),
                new Vector2(toEnd.Length() / _arrow.Width, 1),
                SpriteEffects.None,
                0);
        }

        #endregion

        #region Types

        private sealed class ContactInfo
        {
            public FarPosition PositionA;

            public FarPosition PositionB;

            public Vector2 Normal;
        }

        #endregion
    }
}