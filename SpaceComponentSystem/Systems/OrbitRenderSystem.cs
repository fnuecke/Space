using System.Collections.Generic;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Graphics;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    public sealed class OrbitRenderSystem : AbstractSystem
    {
        #region Constants

        /// <summary>
        /// Thickness of the rendered orbit ellipses.
        /// </summary>
        private const int OrbitThickness = 6;

        /// <summary>
        /// Diffuse area of the dead zone (no immediate cutoff but fade to
        /// red).
        /// </summary>
        private const int DeadZoneDiffuseWidth = 100;

        /// <summary>
        /// Color to paint orbits in.
        /// </summary>
        private static readonly Color OrbitColor = Color.Turquoise * 0.3f;

        /// <summary>
        /// Color to paint the dead zone around gravitational attractors in.
        /// </summary>
        private static readonly Color DeadZoneColor = Color.DarkRed * 0.2f;

        #endregion

        #region Fields

        /// <summary>
        /// The local client session, to allow getting the local player's avatar.
        /// </summary>
        private readonly IClientSession _session;

        /// <summary>
        /// The sprite batch to render the orbits into.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// Used to draw orbits.
        /// </summary>
        private readonly Ellipse _orbitEllipse;

        /// <summary>
        /// Used to draw areas where gravitation force is stronger than ship's
        /// thrusters' force.
        /// </summary>
        private readonly FilledEllipse _deadZoneEllipse;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private readonly HashSet<int> _reusableNeighborList = new HashSet<int>();

        #endregion
        
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="spriteBatch">The sprite batch to use for rendering.</param>
        /// <param name="session">The local session to get the local player.</param>
        public OrbitRenderSystem(ContentManager content, SpriteBatch spriteBatch, IClientSession session)
        {
            _spriteBatch = spriteBatch;
            _session = session;

            _orbitEllipse = new Ellipse(content, spriteBatch.GraphicsDevice) { Thickness = OrbitThickness };
            _deadZoneEllipse = new FilledEllipse(content, spriteBatch.GraphicsDevice) { Gradient = DeadZoneDiffuseWidth, Color = DeadZoneColor };
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Render our local radar system, with whatever detectables are close
        /// enough.
        /// </summary>
        public override void Draw(long frame)
        {
            // Get local player's avatar.
            var avatar = ((AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(_session.LocalPlayer.Number);
            if (!avatar.HasValue)
            {
                return;
            }

            // Get info on the local player's ship.
            var info = Manager.GetComponent<ShipInfo>(avatar.Value);

            // Get the index we use for looking up nearby objects.
            var index = (IndexSystem)Manager.GetSystem(IndexSystem.TypeId);

            // Get camera information.
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Get camera position.
            var position = camera.CameraPositon;

            // Get zoom from camera.
            var zoom = camera.Zoom;

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
            var radius = (float)System.Math.Sqrt(center.X * center.X + center.Y * center.Y);

            // Increase radius accordingly, to include stuff possibly further away.
            radius /= zoom;

            // Loop through all our neighbors.
            ICollection<int> neighbors = _reusableNeighborList;
            index.Find(position, radarRange, ref neighbors, DetectableSystem.IndexGroupMask);

            // Begin drawing.
            _spriteBatch.Begin();
            foreach (var neighbor in neighbors)
            {
                // Get the components we need.
                var neighborTransform = Manager.GetComponent<Transform>(neighbor);
                var neighborDetectable = Manager.GetComponent<Detectable>(neighbor);

                // Bail if we're missing something.
                if (neighborTransform == null || neighborDetectable.Texture == null)
                {
                    continue;
                }

                // We don't show the icons for anything that's inside our
                // viewport. Get the position of the detectable inside our
                // viewport. This will also serve as our direction vector.
                var direction = neighborTransform.Translation - position;

                // We'll make stuff far away a little less opaque. First get
                // the linear relative distance.
                float ld = direction.LengthSquared() / radarRangeSquared;
                // Then apply a exponential fall-off, and make it cap a little
                // early to get the 100% alpha when nearby, not only when
                // exactly on top of the object ;)
                ld = System.Math.Min(1, (1.1f - ld * ld * ld) * 1.1f);

                // If it's an astronomical object, check if its orbit is
                // potentially in our screen space, if so draw it.
                var ellipse = Manager.GetComponent<EllipsePath>(neighbor);
                if (ellipse != null)
                {
                    // The entity we're orbiting around is at one of the two
                    // foci of the ellipse. We want the center, though.

                    // Get the current position of the entity we're orbiting.
                    var focusTransform = Manager.GetComponent<Transform>(ellipse.CenterEntityId).Translation;

                    // Compute the distance of the ellipse's foci to the center
                    // of the ellipse.
                    var ellipseFocusDistance = (float)System.Math.Sqrt(ellipse.MajorRadius * ellipse.MajorRadius - ellipse.MinorRadius * ellipse.MinorRadius);
                    Vector2 ellipseCenter;
                    ellipseCenter.X = ellipseFocusDistance;
                    ellipseCenter.Y = 0;
                    Matrix rotation = Matrix.CreateRotationZ(ellipse.Angle);
                    Vector2.Transform(ref ellipseCenter, ref rotation, out ellipseCenter);
                    ellipseCenter += focusTransform;

                    // Far clipping, i.e. don't render if we're outside and
                    // not seeing the ellipse.
                    var distanceToCenterSquared = (ellipseCenter - position).LengthSquared();
                    var farClipDistance = ellipse.MajorRadius + radius;
                    farClipDistance *= farClipDistance;

                    // Near clipping, i.e. don't render if we're inside the
                    // ellipse, but not seeing its border.
                    float nearClipDistance = System.Math.Max(0, ellipse.MinorRadius - radius);
                    nearClipDistance *= nearClipDistance;

                    // Check if we're cutting (potentially seeing) the orbit
                    // ellipse of the neighbor.
                    if (farClipDistance > distanceToCenterSquared &&
                        nearClipDistance <= distanceToCenterSquared)
                    {
                        // Yes, set the properties for our ellipse renderer.
                        _orbitEllipse.Center = ellipseCenter - position + center;
                        _orbitEllipse.MajorRadius = ellipse.MajorRadius;
                        _orbitEllipse.MinorRadius = ellipse.MinorRadius;
                        _orbitEllipse.Rotation = ellipse.Angle;

                        // Diameter the opacity based on our distance to the
                        // actual object. Apply a exponential fall-off, and
                        // make it cap a little early to get the 100% alpha
                        // when nearby, not only when exactly on top of the
                        // object ;)
                        _orbitEllipse.Color = OrbitColor * ld;
                        // Scale ellipse based on camera zoom.
                        _orbitEllipse.Scale = zoom;
                        // And draw it!
                        _orbitEllipse.Draw();
                    }
                }

                // If the neighbor does collision damage and is an attractor,
                // show the "dead zone" (i.e. the area beyond the point of no
                // return).
                var neighborGravitation = Manager.GetComponent<Gravitation>(neighbor);
                var neighborCollisionDamage = Manager.GetComponent<CollisionDamage>(neighbor);
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
                var pointOfNoReturn = (float)System.Math.Sqrt(mass * neighborMass / maxAcceleration);
                _deadZoneEllipse.Center = neighborTransform.Translation - position + center;
                // Add the complete diffuse width, not just the half (which
                // would be the exact point), because it's unlikely someone
                // will exactly hit that point, so give them some fair
                // warning.
                _deadZoneEllipse.Radius = pointOfNoReturn + DeadZoneDiffuseWidth;
                // Scale ellipse based on camera zoom.
                _deadZoneEllipse.Scale = zoom;
                // And draw it!
                _deadZoneEllipse.Draw();
            }
            // Done drawing.
            _spriteBatch.End();

            // Clear the list for the next run.
            _reusableNeighborList.Clear();
        }

        #endregion
    }
}
