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
using Space.Data;

namespace Space.ScreenManagement.Screens.Gameplay
{
    /// <summary>
    /// Renderer class that's responsible for drawing a player's radar, i.e.
    /// the overlay that displays icons for nearby but out-of-screen objects
    /// of interest (ones with a <c>Detectable</c> component).
    /// </summary>
    public sealed class Radar
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
        /// Background image for radar icons.
        /// </summary>
        private Texture2D _radarBackground;

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
        private static readonly List<Entity> _reusableNeighborList = new List<Entity>(64);

        #endregion

        #region Constructor

        public Radar(GameClient client)
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

            _radarBackground = content.Load<Texture2D>("Textures/radar_background");
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
            var position = info.Position;
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

            // Get the texture origin (middle of the texture).
            Vector2 backgroundOrigin;
            backgroundOrigin.X = _radarBackground.Width / 2.0f;
            backgroundOrigin.Y = _radarBackground.Height / 2.0f;

            // Get bounds in which to display the icon.
            Rectangle screenBounds = viewport.Bounds;

            // Get the inner bounds in which to display the icon, i.e. minus
            // half the size of the icon, so deflate by that.
            Rectangle innerBounds = screenBounds;
            innerBounds.Inflate(-(int)backgroundOrigin.X, -(int)backgroundOrigin.Y);

            // Now this is the tricky part: we take the minimal bounding sphere
            // (or rather, circle) that fits our screen space. For each
            // detectable entity we then pick the point on this circle that's
            // in the direction of that entity.
            // Because the only four points where this'll actually be in screen
            // space will be the four corners, we'll map them down to the edges
            // again. See below for that.
            float a = center.X - backgroundOrigin.X;
            float b = center.Y - backgroundOrigin.Y;
            float radius = (float)System.Math.Sqrt(a * a + b * b);

            // Precomputed for the loop.
            float radarRangeSquared = radarRange * radarRange;

            // Begin drawing.
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

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
                        nearClipDistance < distanceToCenterSquared)
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
            
                // Check if the object's inside. If so, skip it.
                if (screenBounds.Contains((int)(direction.X + center.X), (int)(direction.Y + center.Y)))
                {
                    continue;
                }

                // Get the color of the faction faction the detectable belongs
                // to (it any).
                var color = Color.White;
                if (faction != null)
                {
                    color = faction.Value.ToColor();
                }

                // Make stuff far away a little less opaque.
                color *= ld;

                // Get the direction to the detectable and normalize it.
                direction.Normalize();

                // Figure out where we want to position our icon. As described
                // above, we first get the point on the surrounding circle,
                // by multiplying our normalized direction vector with that
                // circle's radius.
                Vector2 iconPosition;
                iconPosition.X = radius * direction.X;
                iconPosition.Y = radius * direction.Y;

                // But now it's almost certainly outside our screen. So let's
                // see in which sector we are (we can treat left/right and
                // up/down identically).
                if (iconPosition.X > center.X || iconPosition.X < -center.X)
                {
                    // Out of screen on the X axis. Guaranteed to be in bound
                    // for Y axis, though. Scale down.
                    var scale = center.X / System.Math.Abs(iconPosition.X);
                    iconPosition.X *= scale;
                    iconPosition.Y *= scale;
                }
                else if (iconPosition.Y > center.Y || iconPosition.Y < -center.Y)
                {
                    // Out of screen on the Y axis. Guaranteed to be in bound
                    // for X axis, though. Scale down.
                    var scale = center.Y / System.Math.Abs(iconPosition.Y);
                    iconPosition.X *= scale;
                    iconPosition.Y *= scale;
                }

                // Adjust to the center.
                iconPosition += center;

                // Finally, clamp the point to be far enough inside our
                // viewport for the complete texture of our icon to be
                // displayed, which is the original viewport minus half the
                // size of the icon.

                // Clamp our coordinates.
                iconPosition.X = MathHelper.Clamp(iconPosition.X, innerBounds.Left, innerBounds.Right);
                iconPosition.Y = MathHelper.Clamp(iconPosition.Y, innerBounds.Top, innerBounds.Bottom);

                // And, finally, draw it. First the background.
                _spriteBatch.Draw(_radarBackground, iconPosition, null, color, 0,
                    backgroundOrigin, 1, SpriteEffects.None, 0);

                // Get the texture origin (middle of the texture).
                Vector2 origin;
                origin.X = neighborDetectable.Texture.Width / 2.0f;
                origin.Y = neighborDetectable.Texture.Height / 2.0f;

                // And draw that, too.
                _spriteBatch.Draw(neighborDetectable.Texture, iconPosition, null, color, 0,
                    origin, 1, SpriteEffects.None, 0);
            }

            // Done drawing.
            _spriteBatch.End();

            // Clear the list for the next run.
            _reusableNeighborList.Clear();
        }

        #endregion
    }
}
