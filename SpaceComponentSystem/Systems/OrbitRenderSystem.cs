using System;
using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    public sealed class OrbitRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
    {
        #region Constants

        /// <summary>Thickness of the rendered orbit ellipses.</summary>
        private const int OrbitThickness = 6;

        /// <summary>Diffuse area of the dead zone (no immediate cutoff but fade to red).</summary>
        private const int DeadZoneDiffuseWidth = 100;

        /// <summary>Color to paint orbits in.</summary>
        private static readonly Color OrbitColor = Color.Turquoise * 0.5f;

        /// <summary>Color to paint the dead zone around gravitational attractors in.</summary>
        private static readonly Color DeadZoneColor = Color.FromNonPremultiplied(255, 0, 0, 64);

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The spritebatch to use for rendering.</summary>
        private SpriteBatch _spriteBatch;

        /// <summary>Used to draw orbits.</summary>
        private Ellipse _ellipse;

        /// <summary>Used to draw areas where gravitation force is stronger than ship's thrusters' force.</summary>
        private FilledEllipse _filledEllipse;

        #endregion

        #region Single-Allocation

        /// <summary>Reused for iterating components.</summary>
        private ISet<int> _reusableNeighborList = new HashSet<int>();

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
                    _spriteBatch = new SpriteBatch(cm.Value.Graphics.GraphicsDevice);
                    if (_ellipse == null)
                    {
                        _ellipse = new Ellipse(cm.Value.Content, cm.Value.Graphics)
                        {
                            Thickness = OrbitThickness,
                            BlendState = BlendState.Additive
                        };
                        _ellipse.LoadContent();
                    }
                    if (_filledEllipse == null)
                    {
                        _filledEllipse = new FilledEllipse(cm.Value.Content, cm.Value.Graphics)
                        {
                            Gradient = DeadZoneDiffuseWidth,
                            Color = DeadZoneColor,
                            BlendState = BlendState.Additive
                        };
                        _filledEllipse.LoadContent();
                    }
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
        }

        /// <summary>Render our local radar system, with whatever detectables are close enough.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Get local player's avatar.
            var avatar = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            if (avatar <= 0)
            {
                return;
            }

            // Get info on the local player's ship.
            var info = (ShipInfo) Manager.GetComponent(avatar, ShipInfo.TypeId);

            // Get the index we use for looking up nearby objects.
            var index = (IndexSystem) Manager.GetSystem(IndexSystem.TypeId);

            // Get camera information.
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Get camera position.
            var position = camera.CameraPosition;

            // Get zoom from camera.
            var zoom = camera.Zoom;

            // Scale ellipse based on camera zoom.
            _filledEllipse.Scale = zoom;
            _ellipse.Scale = zoom;

            // Figure out the overall range of our radar system.
            var radarRange = info.RadarRange;

            // Our mass.
            var mass = info.Mass;

            // Get our viewport.
            var viewport = _spriteBatch.GraphicsDevice.Viewport;

            // Get the screen's center, used for diverse computations, and as
            // a center for relative computations (because the player's always
            // rendered in the center of the screen).
            Vector2 center;
            center.X = viewport.Width / 2f;
            center.Y = viewport.Height / 2f;

            // Precomputed for the loop.
            var radarRangeSquared = radarRange * radarRange;

            // Get the radius of the minimal bounding sphere of our viewport.
            var radius = (float) Math.Sqrt(center.X * center.X + center.Y * center.Y);

            // Increase radius accordingly, to include stuff possibly further away.
            radius /= zoom;

            // Loop through all our neighbors.
            index.Find(position, radarRange, _reusableNeighborList, DetectableSystem.IndexGroupMask);

            // Begin drawing.
            _spriteBatch.Begin();
            foreach (IIndexable neighbor in _reusableNeighborList.Select(Manager.GetComponentById))
            {
                // Get the components we need.
                var neighborTransform = Manager.GetComponent(neighbor.Entity, Transform.TypeId) as Transform;
                var neighborDetectable = Manager.GetComponent(neighbor.Entity, Detectable.TypeId) as Detectable;

                // Bail if we're missing something.
                if (neighborTransform == null || neighborDetectable == null || neighborDetectable.Texture == null)
                {
                    continue;
                }

                // We don't show the icons for anything that's inside our
                // viewport. Get the position of the detectable inside our
                // viewport. This will also serve as our direction vector.
                var direction = (Vector2) (neighborTransform.Translation - position);

                // We'll make stuff far away a little less opaque. First get
                // the linear relative distance.
                var ld = direction.LengthSquared() / radarRangeSquared;
                // Then apply a exponential fall-off, and make it cap a little
                // early to get the 100% alpha when nearby, not only when
                // exactly on top of the object ;)
                ld = Math.Min(1, (1.1f - ld * ld * ld) * 1.1f);

                // If it's an astronomical object, check if its orbit is
                // potentially in our screen space, if so draw it.
                var ellipse = ((EllipsePath) Manager.GetComponent(neighbor.Entity, EllipsePath.TypeId));
                if (ellipse != null)
                {
                    // The entity we're orbiting around is at one of the two
                    // foci of the ellipse. We want the center, though.

                    // Get the current position of the entity we're orbiting.
                    var focusTransform =
                        ((Transform) Manager.GetComponent(ellipse.CenterEntityId, Transform.TypeId)).Translation;

                    // Compute the distance of the ellipse's foci to the center
                    // of the ellipse.
                    var ellipseFocusDistance =
                        (float)
                        Math.Sqrt(ellipse.MajorRadius * ellipse.MajorRadius - ellipse.MinorRadius * ellipse.MinorRadius);
                    Vector2 ellipseCenter;
                    ellipseCenter.X = ellipseFocusDistance;
                    ellipseCenter.Y = 0;
                    var rotation = Matrix.CreateRotationZ(ellipse.Angle);
                    Vector2.Transform(ref ellipseCenter, ref rotation, out ellipseCenter);
                    focusTransform += ellipseCenter;

                    // Get relative vector from position to ellipse center.
                    var toCenter = (Vector2) (focusTransform - position);

                    // Far clipping, i.e. don't render if we're outside and
                    // not seeing the ellipse.
                    var distanceToCenterSquared = toCenter.LengthSquared();
                    var farClipDistance = ellipse.MajorRadius + radius;
                    farClipDistance *= farClipDistance;

                    // Near clipping, i.e. don't render if we're inside the
                    // ellipse, but not seeing its border.
                    float nearClipDistance = Math.Max(0, ellipse.MinorRadius - radius);
                    nearClipDistance *= nearClipDistance;

                    // Check if we're cutting (potentially seeing) the orbit
                    // ellipse of the neighbor.
                    if (farClipDistance > distanceToCenterSquared &&
                        nearClipDistance <= distanceToCenterSquared)
                    {
                        // Yes, set the properties for our ellipse renderer.
                        _ellipse.Center = toCenter + center;
                        _ellipse.MajorRadius = ellipse.MajorRadius;
                        _ellipse.MinorRadius = ellipse.MinorRadius;
                        _ellipse.Rotation = ellipse.Angle;

                        // Diameter the opacity based on our distance to the
                        // actual object. Apply a exponential fall-off, and
                        // make it cap a little early to get the 100% alpha
                        // when nearby, not only when exactly on top of the
                        // object ;)
                        _ellipse.Color = OrbitColor * ld;
                        // And draw it!
                        _ellipse.Draw();
                    }
                }

                // If the neighbor does collision damage and is an attractor,
                // show the "dead zone" (i.e. the area beyond the point of no
                // return).
                var neighborGravitation = Manager.GetComponent(neighbor.Entity, Gravitation.TypeId) as Gravitation;
                var neighborCollisionDamage = Manager.GetComponent(neighbor.Entity, CollisionDamage.TypeId) as CollisionDamage;
                if (neighborCollisionDamage == null || neighborGravitation == null ||
                    (neighborGravitation.GravitationType & Gravitation.GravitationTypes.Attractor) == 0)
                {
                    continue;
                }

                // The point of no return is the distance at which the
                // gravitation is stronger than our maximum thruster
                // output.
                var maxAcceleration = info.MaxAcceleration;
                var neighborMass = neighborGravitation.Mass;
                var dangerPoint =
                    (float) Math.Sqrt(mass * neighborMass / (maxAcceleration * 0.4f * Settings.TicksPerSecond)) +
                    DeadZoneDiffuseWidth;
                _filledEllipse.Center = direction + center;
                var distToCenter = Vector2.Distance(_filledEllipse.Center, center);
                // Check if we're potentially seeing the marker.
                if (radius >= distToCenter - dangerPoint)
                {
                    _filledEllipse.Radius = dangerPoint;
                    _filledEllipse.Draw();

                    var pointOfNoReturn =
                        (float) Math.Sqrt(mass * neighborMass / (maxAcceleration * Settings.TicksPerSecond)) +
                        DeadZoneDiffuseWidth;
                    if (radius >= distToCenter - pointOfNoReturn)
                    {
                        _filledEllipse.Radius = pointOfNoReturn;
                        _filledEllipse.Draw();
                    }
                }
            }
            // Done drawing.
            _spriteBatch.End();

            // Clear the list for the next run.
            _reusableNeighborList.Clear();
        }

        #endregion
    }
}