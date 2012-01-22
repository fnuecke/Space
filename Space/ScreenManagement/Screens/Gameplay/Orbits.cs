using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Systems;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Control;

namespace Space.ScreenManagement.Screens.Gameplay
{
    /// <summary>
    /// Renderer class that's responsible for drawing planet orbits for planets
    /// that are in range of the player's scanners.
    /// </summary>
    sealed class Orbits
    {
        #region Constants

        /// <summary>
        /// Thickness of the rendered orbit ellipses.
        /// </summary>
        private const int _orbitThickness = 6;

        /// <summary>
        /// Diffuse area of the dead zone (no immediate cutoff but fade to
        /// red).
        /// </summary>
        private const int _deadZoneDiffuseWidth = 100;

        /// <summary>
        /// Color to paint orbits in.
        /// </summary>
        private static readonly Color _orbitColor = Color.Turquoise * 0.3f;

        /// <summary>
        /// Color to paint the dead zone around gravitational attractors in.
        /// </summary>
        private static readonly Color _deadZoneColor = Color.DarkRed * 0.2f;

        #endregion

        #region Fields

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        private readonly GameClient _client;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Used to draw orbits.
        /// </summary>
        private Ellipse _orbitEllipse;

        /// <summary>
        /// Used to draw areas where gravitation force is stronger than ship's
        /// thrusters' force.
        /// </summary>
        private FilledEllipse _deadZoneEllipse;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private readonly List<Entity> _reusableNeighborList = new List<Entity>(64);

        #endregion
        
        #region Constructor

        public Orbits(GameClient client)
        {
            _client = client;
            _orbitEllipse = new Ellipse(client.Game);
            _orbitEllipse.SetThickness(_orbitThickness);
            _deadZoneEllipse = new FilledEllipse(_client.Game);
            _deadZoneEllipse.SetGradient(_deadZoneDiffuseWidth);
            _deadZoneEllipse.SetColor(_deadZoneColor);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent(SpriteBatch spriteBatch, ContentManager content)
        {
            _spriteBatch = spriteBatch;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Render our local radar system, with whatever detectables are close
        /// enough.
        /// </summary>
        public void Draw()
        {
            // Get local player's avatar.
            var info = _client.GetPlayerShipInfo();

            // Can't do anything without an avatar.
            if (info == null)
            {
                return;
            }

            // Fetch all the components we need.
            var position = _client.GetCameraPosition();
            var index = _client.GetSystem<IndexSystem>();

            // Bail if we're missing something.
            if (index == null)
            {
                return;
            }

            // Figure out the overall range of our radar system.
            float radarRange = info.RadarRange;

            // Our mass.
            float mass = info.Mass;

            // Get our viewport.
            var viewport = _spriteBatch.GraphicsDevice.Viewport;

            // Get the screen's center, used for diverse computations, and as
            // a center for relative computations (because the player's always
            // rendered in the center of the screen).
            Vector2 center;
            center.X = viewport.Width / 2f;
            center.Y = viewport.Height / 2f;

            // Get bounds in which to display the icon.
            var screenBounds = viewport.Bounds;

            // Precomputed for the loop.
            float radarRangeSquared = radarRange * radarRange;

            // Get the radius of the minimal bounding sphere of our viewport.
            float radius = (float)System.Math.Sqrt(center.X * center.X + center.Y * center.Y);

            // Begin drawing.
            _spriteBatch.Begin();

            // Loop through all our neighbors.
            foreach (var neighbor in index.
                GetNeighbors(ref position, radarRange, Detectable.IndexGroup, _reusableNeighborList))
            {
                // Get the components we need.
                var neighborTransform = neighbor.GetComponent<Transform>();
                var neighborDetectable = neighbor.GetComponent<Detectable>();
                var faction = neighbor.GetComponent<Faction>();

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
                var ellipse = neighbor.GetComponent<EllipsePath>();
                if (ellipse != null)
                {
                    // The entity we're orbiting around is at one of the two
                    // foci of the ellipse. We want the center, though.

                    // Get the current position of the entity we're orbiting.
                    var focusTransform = _client.Controller.Simulation.EntityManager.GetEntity(ellipse.CenterEntityId).GetComponent<Transform>().Translation;

                    // Compute the distance of the ellipse's foci to the center
                    // of the ellipse.
                    float ellipseFocusDistance = (float)System.Math.Sqrt(ellipse.MajorRadius * ellipse.MajorRadius - ellipse.MinorRadius * ellipse.MinorRadius);
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
                        _orbitEllipse.SetCenter(ellipseCenter - position + center);
                        _orbitEllipse.SetMajorRadius(ellipse.MajorRadius);
                        _orbitEllipse.SetMinorRadius(ellipse.MinorRadius);
                        _orbitEllipse.SetRotation(ellipse.Angle);

                        // Scale the opacity based on our distance to the
                        // actual object. Apply a exponential fall-off, and
                        // make it cap a little early to get the 100% alpha
                        // when nearby, not only when exactly on top of the
                        // object ;)
                        _orbitEllipse.SetColor(_orbitColor * ld);

                        // And draw it!
                        _orbitEllipse.Draw();
                    }
                }

                // If the neighbor does collision damage and is an attractor,
                // show the "dead zone" (i.e. the area beyond the point of no
                // return).
                var neighborGravitation = neighbor.GetComponent<Gravitation>();
                var neighborCollisionDamage = neighbor.GetComponent<CollisionDamage>();
                if (neighborCollisionDamage != null && neighborGravitation != null &&
                    (neighborGravitation.GravitationType & Gravitation.GravitationTypes.Attractor) != 0)
                {
                    // The point of no return is the distance at which the
                    // gravitation is stronger than our maximum thruster
                    // output.
                    float maxAcceleration = info.MaxAcceleration;
                    float neighborMass = neighborGravitation.Mass;
                    float pointOfNoReturn = (float)System.Math.Sqrt(mass * neighborMass / maxAcceleration);
                    _deadZoneEllipse.SetCenter(neighborTransform.Translation - position + center);
                    // Add the complete diffuse width, not just the half (which
                    // would be the exact point), because it's unlikely someone
                    // will exactly hit that point, so give them some fair
                    // warning.
                    _deadZoneEllipse.SetRadius(pointOfNoReturn + _deadZoneDiffuseWidth);
                    _deadZoneEllipse.Draw();
                }
            }

            // Clear the list for the next run.
            _reusableNeighborList.Clear();

            // Done drawing.
            _spriteBatch.End();
        }

        #endregion
    }
}
