using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Control;
using Space.Data;
using Space.Util;

namespace Space.View
{
    /// <summary>
    /// Renderer class that's responsible for drawing a player's radar, i.e.
    /// the overlay that displays icons for nearby but out-of-screen objects
    /// of interest (ones with a <c>Detectable</c> component).
    /// </summary>
    sealed class Radar
    {
        #region Types

        /// <summary>
        /// Readable direction indexes for the radar images.
        /// </summary>
        private enum RadarDirection
        {
            Top,
            Left,
            Right,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        #endregion

        #region Constants

        /// <summary>
        /// Width of a single radar icon.
        /// </summary>
        private const int RadarIconWidth = 48;

        /// <summary>
        /// Height of a single radar icon.
        /// </summary>
        private const int RadarIconHeight = 48;

        /// <summary>
        /// Vertical offset of the distance number display relative to the
        /// center of the radar icons.
        /// </summary>
        private const int DistanceOffset = 5;

        /// <summary>
        /// Size of the radar border in pixel.
        /// </summary>
        private const int RadarBorderSize = 50;

        /// <summary>
        /// Percentage value when the health indicator within the radar frame
        /// should start displaying low health in red color.
        /// </summary>
        private const float HealthIndicatorThreshold = 0.5f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the game client for which to render the orbits.
        /// </summary>
        public GameClient Client { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The sprite batch to render the orbits into.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// Background image for radar icons.
        /// </summary>
        private readonly Texture2D[] _radarDirection = new Texture2D[8];

        /// <summary>
        /// Background for rendering the distance to a target.
        /// </summary>
        private Texture2D _radarDistance;

        /// <summary>
        /// Texture marking an icon as targeted.
        /// </summary>
        private Texture2D _radarTarget;

        /// <summary>
        /// Font used to render the distance on radar icons.
        /// </summary>
        private SpriteFont _distanceFont;

        #endregion

        #region Single-Allocation

        /// <summary>
        /// Reused for iterating components.
        /// </summary>
        private readonly HashSet<int> _reusableNeighborList = new HashSet<int>();

        #endregion

        #region Constructor

        public Radar(Game game, SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;

            _radarDirection[(int)RadarDirection.Top] = game.Content.Load<Texture2D>("Textures/Radar/top");
            _radarDirection[(int)RadarDirection.Left] = game.Content.Load<Texture2D>("Textures/Radar/left");
            _radarDirection[(int)RadarDirection.Right] = game.Content.Load<Texture2D>("Textures/Radar/right");
            _radarDirection[(int)RadarDirection.Bottom] = game.Content.Load<Texture2D>("Textures/Radar/bottom");
            _radarDirection[(int)RadarDirection.TopLeft] = game.Content.Load<Texture2D>("Textures/Radar/top_left");
            _radarDirection[(int)RadarDirection.TopRight] = game.Content.Load<Texture2D>("Textures/Radar/top_right");
            _radarDirection[(int)RadarDirection.BottomLeft] = game.Content.Load<Texture2D>("Textures/Radar/bottom_left");
            _radarDirection[(int)RadarDirection.BottomRight] = game.Content.Load<Texture2D>("Textures/Radar/bottom_right");
            _radarDistance = game.Content.Load<Texture2D>("Textures/Radar/distance");
            _radarTarget = game.Content.Load<Texture2D>("Textures/Radar/target");
            _distanceFont = game.Content.Load<SpriteFont>("Fonts/visitor");
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Render our local radar system, with whatever detectables are close
        /// enough.
        /// </summary>
        public void Draw()
        {
            if (Client == null || Client.Controller.Session.ConnectionState != ClientState.Connected)
            {
                return;
            }

            // Get local player's avatar.
            var info = Client.GetPlayerShipInfo();

            // Can't do anything without an avatar.
            if (info == null)
            {
                return;
            }

            // Fetch all the components we need.
            var index = Client.GetSystem<IndexSystem>();

            // Bail if we're missing something.
            if (index == null)
            {
                return;
            }

            // Get the actual position we're rendering at. Note that this will
            // actually allow the player to "extend" his radar by the maximum
            // distance of the camera to his ship. That'll be a negligible
            // amount, however, in relation to the total radar range.
            var position = Client.GetCameraPosition();

            // Figure out the overall range of our radar system.
            var radarRange = info.RadarRange;

            // Get bounds in which to display the icon.
            var screenBounds = _spriteBatch.GraphicsDevice.Viewport.Bounds;

            // Get the screen's center, used for diverse computations, and as
            // a center for relative computations (because the player's always
            // rendered in the center of the screen).
            Vector2 center;
            center.X = screenBounds.Width / 2f;
            center.Y = screenBounds.Height / 2f;

            // Get the texture origin (middle of the texture).
            Vector2 backgroundOrigin;
            backgroundOrigin.X = RadarIconWidth / 2.0f;
            backgroundOrigin.Y = RadarIconHeight / 2.0f;

            // Get the inner bounds in which to display the icon, i.e. minus
            // half the size of the icon, so deflate by that.
            var innerBounds = screenBounds;
            innerBounds.Inflate(-(int)backgroundOrigin.X, -(int)backgroundOrigin.Y);

            // Now this is the tricky part: we take the minimal bounding sphere
            // (or rather, circle) that fits our screen space. For each
            // detectable entity we then pick the point on this circle that's
            // in the direction of that entity.
            // Because the only four points where this'll actually be in screen
            // space will be the four corners, we'll map them down to the edges
            // again. See below for that.
            var a = center.X - backgroundOrigin.X;
            var b = center.Y - backgroundOrigin.Y;
            var radius = (float)Math.Sqrt(a * a + b * b);

            // Begin drawing.
            _spriteBatch.Begin();

            // Color the background of the radar red if health is low...
            float healthPercent = info.RelativeHealth;
            if (info.RelativeHealth < HealthIndicatorThreshold)
            {
                float redAlpha = (1 - healthPercent / HealthIndicatorThreshold) / 2;
                //_basicForms.FillRectangle(0, 0, RadarBorderSize, screenBounds.Height, Color.Red * redAlpha);
                //_basicForms.FillRectangle(screenBounds.Width - RadarBorderSize, 0, RadarBorderSize, screenBounds.Height, Color.Red * redAlpha);
                //_basicForms.FillRectangle(RadarBorderSize, 0, screenBounds.Width - 2 * RadarBorderSize, RadarBorderSize, Color.Red * redAlpha);
                //_basicForms.FillRectangle(RadarBorderSize, screenBounds.Height - RadarBorderSize, screenBounds.Width - 2 * RadarBorderSize, RadarBorderSize, Color.Red * redAlpha);
            }

            // Loop through all our neighbors.
            foreach (var neighbor in index.
                RangeQuery(ref position, radarRange, Detectable.IndexGroup, _reusableNeighborList))
            {
                // Get the components we need.
                var neighborTransform = Client.GetComponent<Transform>(neighbor);
                var neighborDetectable = Client.GetComponent<Detectable>(neighbor);
                var faction = Client.GetComponent<Faction>(neighbor);

                // Bail if we're missing something.
                if (neighborTransform == null || neighborDetectable.Texture == null)
                {
                    continue;
                }

                // We don't show the icons for anything that's inside our
                // viewport. Get the position of the detectable inside our
                // viewport. This will also serve as our direction vector.
                var direction = neighborTransform.Translation - position;
                float distance = direction.Length();

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

                // We'll make stuff far away a little less opaque. First get
                // the linear relative distance.
                var ld = distance / radarRange;
                // Then apply a exponential fall-off, and make it cap a little
                // early to get the 100% alpha when nearby, not only when
                // exactly on top of the object ;)
                ld = Math.Min(1, (1.1f - ld * ld * ld) * 1.1f);

                // Make stuff far away a little less opaque.
                color *= ld;

                // Get the direction to the detectable and normalize it.
                direction /= distance;

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
                    // for Y axis, though. Diameter down.
                    var scale = center.X / Math.Abs(iconPosition.X);
                    iconPosition.X *= scale;
                    iconPosition.Y *= scale;
                }
                else if (iconPosition.Y > center.Y || iconPosition.Y < -center.Y)
                {
                    // Out of screen on the Y axis. Guaranteed to be in bound
                    // for X axis, though. Diameter down.
                    var scale = center.Y / Math.Abs(iconPosition.Y);
                    iconPosition.X *= scale;
                    iconPosition.Y *= scale;
                }
                
                // Adjust the distance to an object such that it is the
                // distance to the screen edge, if so desired.
                if (Settings.Instance.RadarDistanceFromBorder)
                {
                    distance -= iconPosition.Length();
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
                _spriteBatch.Draw(_radarDirection[(int)GetRadarDirection(ref iconPosition, ref innerBounds)],
                    iconPosition, null,
                    color, 0, backgroundOrigin, ld, SpriteEffects.None, 0);

                // Get the texture origin (middle of the texture).
                Vector2 origin;
                origin.X = neighborDetectable.Texture.Width / 2.0f;
                origin.Y = neighborDetectable.Texture.Height / 2.0f;

                // And draw that, too.
                _spriteBatch.Draw(neighborDetectable.Texture, iconPosition, null,
                    Color.White * ld, 0, origin, ld, SpriteEffects.None, 0);

                // Draw the distance to the object.
                _spriteBatch.Draw(_radarDistance, iconPosition, null,
                    Color.White * ld, 0, backgroundOrigin, ld, SpriteEffects.None, 0);

                string formattedDistance = FormatDistance(distance);
                origin.X = _distanceFont.MeasureString(formattedDistance).X / 2f;
                origin.Y = -DistanceOffset;
                _spriteBatch.DrawString(_distanceFont, formattedDistance, iconPosition,
                    Color.White * ld, 0, origin, ld, SpriteEffects.None, 0);
            }

            // Clear the list for the next run.
            _reusableNeighborList.Clear();

            // Done drawing.
            _spriteBatch.End();
        }

        /// <summary>
        /// Gets the actual direction background to display. This checks which
        /// borders the icon touches and returns the according direction.
        /// </summary>
        /// <param name="position">The icon's position.</param>
        /// <param name="bounds">The bounds.</param>
        /// <returns>The direction of the border contact of the icon.</returns>
        private static RadarDirection GetRadarDirection(ref Vector2 position, ref Rectangle bounds)
        {
            if (Math.Abs(position.X - bounds.Left) < 0.001f)
            {
                if (Math.Abs(position.Y - bounds.Top) < 0.001f)
                {
                    return RadarDirection.TopLeft;
                }
                else if (Math.Abs(position.Y - bounds.Bottom) < 0.001f)
                {
                    return RadarDirection.BottomLeft;
                }
                else
                {
                    return RadarDirection.Left;
                }
            }
            else if (Math.Abs(position.X - bounds.Right) < 0.001f)
            {
                if (Math.Abs(position.Y - bounds.Top) < 0.001f)
                {
                    return RadarDirection.TopRight;
                }
                else if (Math.Abs(position.Y - bounds.Bottom) < 0.001f)
                {
                    return RadarDirection.BottomRight;
                }
                else
                {
                    return RadarDirection.Right;
                }
            }
            else
            {
                if (Math.Abs(position.Y - bounds.Top) < 0.001f)
                {
                    return RadarDirection.Top;
                }
                else if (Math.Abs(position.Y - bounds.Bottom) < 0.001f)
                {
                    return RadarDirection.Bottom;
                }
                else
                {
                    // Should not be possible!
                    throw new InvalidOperationException("Wait, wut?");
                }
            }
        }

        /// <summary>
        /// List of SI units, used for distance formatting.
        /// </summary>
        private static readonly string[] UnitNames = new[] { "", "k", "m", "g", "t", "p", "e", "z", "y" };

        /// <summary>
        /// Formats a distance to a string to be displayed in a radar icon.
        /// </summary>
        /// <param name="distance">The distance to format.</param>
        /// <returns>The formatted distance.</returns>
        private string FormatDistance(float distance)
        {
            // Divide by thousand until we're blow it, and count the number
            // of divisions to get the unit.
            var unit = 0;
            while (distance > 1000)
            {
                ++unit;
                distance /= 1000;
            }
            var unitName = "?";
            if (unit < UnitNames.Length)
            {
                unitName = UnitNames[unit];
            }
            // Don't show the post-comma digit if we're above 100 of the
            // current unit.
            return distance > 100
                       ? string.Format("{0:f0}{1}", distance, unitName)
                       : string.Format("{0:f1}{1}", distance, unitName);
        }

        #endregion

    }
}
