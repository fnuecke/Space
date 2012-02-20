using System;
using System.Collections.Generic;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Control;
using Space.Data;
using Space.ScreenManagement.Screens.Helper;
using Space.ScreenManagement.Screens.Ingame.Interfaces;
using Space.Util;

namespace Space.ScreenManagement.Screens.Gameplay
{
    /// <summary>
    /// Renderer class that's responsible for drawing a player's radar, i.e.
    /// the overlay that displays icons for nearby but out-of-screen objects
    /// of interest (ones with a <c>Detectable</c> component).
    /// </summary>
    sealed class Radar : AbstractGuiElement
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
        private const int _radarIconWidth = 48;

        /// <summary>
        /// Height of a single radar icon.
        /// </summary>
        private const int _radarIconHeight = 48;

        /// <summary>
        /// Vertical offset of the distance number display relative to the
        /// center of the radar icons.
        /// </summary>
        private const int _distanceOffset = 5;

        /// <summary>
        /// Size of the radar border in pixel.
        /// </summary>
        private const int _radarBorderSize = 50;

        /// <summary>
        /// Percentage value when the health indicator within the radar frame
        /// should start displaying low health in red color.
        /// </summary>
        private const float _healthIndicatorThreshold = 0.5f;

        #endregion

        #region Fields

        /// <summary>
        /// Background image for radar icons.
        /// </summary>
        private Texture2D[] _radarDirection = new Texture2D[8];

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
        private readonly List<Entity> _reusableNeighborList = new List<Entity>(64);

        #endregion

        #region Constructor

        public Radar(GameClient client)
            : base(client)
        {
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent(IngameScreen ingame, ContentManager content)
        {
            _spriteBatch = ingame.SpriteBatch;

            _radarDirection[(int)RadarDirection.Top] = content.Load<Texture2D>("Textures/Radar/top");
            _radarDirection[(int)RadarDirection.Left] = content.Load<Texture2D>("Textures/Radar/left");
            _radarDirection[(int)RadarDirection.Right] = content.Load<Texture2D>("Textures/Radar/right");
            _radarDirection[(int)RadarDirection.Bottom] = content.Load<Texture2D>("Textures/Radar/bottom");
            _radarDirection[(int)RadarDirection.TopLeft] = content.Load<Texture2D>("Textures/Radar/top_left");
            _radarDirection[(int)RadarDirection.TopRight] = content.Load<Texture2D>("Textures/Radar/top_right");
            _radarDirection[(int)RadarDirection.BottomLeft] = content.Load<Texture2D>("Textures/Radar/bottom_left");
            _radarDirection[(int)RadarDirection.BottomRight] = content.Load<Texture2D>("Textures/Radar/bottom_right");
            _radarDistance = content.Load<Texture2D>("Textures/Radar/distance");
            _radarTarget = content.Load<Texture2D>("Textures/Radar/target");
            _distanceFont = content.Load<SpriteFont>("Fonts/visitor");

            _basicForms = new BasicForms(_spriteBatch, _client);
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Render our local radar system, with whatever detectables are close
        /// enough.
        /// </summary>
        public override void Draw()
        {
            // Get local player's avatar.
            var info = _client.GetPlayerShipInfo();

            // Can't do anything without an avatar.
            if (info == null)
            {
                return;
            }

            // Fetch all the components we need.
            var index = _client.GetSystem<IndexSystem>();

            // Bail if we're missing something.
            if (index == null)
            {
                return;
            }

            // Get the actual position we're rendering at. Note that this will
            // actually allow the player to "extend" his radar by the maximum
            // distance of the camera to his ship. That'll be a negligible
            // amount, however, in relation to the total radar range.
            var position = _client.GetCameraPosition();

            // Figure out the overall range of our radar system.
            float radarRange = info.RadarRange;

            // Our mass.
            float mass = info.Mass;

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
            backgroundOrigin.X = _radarIconWidth / 2.0f;
            backgroundOrigin.Y = _radarIconHeight / 2.0f;

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
            float a = center.X - backgroundOrigin.X;
            float b = center.Y - backgroundOrigin.Y;
            float radius = (float)System.Math.Sqrt(a * a + b * b);

            // Precomputed for the loop.
            float radarRangeSquared = radarRange * radarRange;

            // Begin drawing.
            _spriteBatch.Begin();

            // Make the background of the radar a bit darker...
            _basicForms.FillRectangle(0, 0, _radarBorderSize, screenBounds.Height, Color.Black * 0.15f);
            _basicForms.FillRectangle(screenBounds.Width - _radarBorderSize, 0, _radarBorderSize, screenBounds.Height, Color.Black * 0.15f);
            _basicForms.FillRectangle(_radarBorderSize, 0, screenBounds.Width - 2 * _radarBorderSize, _radarBorderSize, Color.Black * 0.15f);
            _basicForms.FillRectangle(_radarBorderSize, screenBounds.Height - _radarBorderSize, screenBounds.Width - 2 * _radarBorderSize, _radarBorderSize, Color.Black * 0.15f);

            // ... and the border of the radar a bit lighter.
            _basicForms.DrawRectangle(_radarBorderSize, _radarBorderSize, screenBounds.Width - 2 * _radarBorderSize, screenBounds.Height - 2 * _radarBorderSize, Color.White * 0.1f);

            // Color the background of the radar red if health is low...
            float healthPercent = info.RelativeHealth;
            if (info.RelativeHealth < _healthIndicatorThreshold)
            {
                float redAlpha = (1 - healthPercent / _healthIndicatorThreshold) / 2;
                _basicForms.FillRectangle(0, 0, _radarBorderSize, screenBounds.Height, Color.Red * redAlpha);
                _basicForms.FillRectangle(screenBounds.Width - _radarBorderSize, 0, _radarBorderSize, screenBounds.Height, Color.Red * redAlpha);
                _basicForms.FillRectangle(_radarBorderSize, 0, screenBounds.Width - 2 * _radarBorderSize, _radarBorderSize, Color.Red * redAlpha);
                _basicForms.FillRectangle(_radarBorderSize, screenBounds.Height - _radarBorderSize, screenBounds.Width - 2 * _radarBorderSize, _radarBorderSize, Color.Red * redAlpha);
            }

            // Loop through all our neighbors.
            foreach (var neighbor in index.
                RangeQuery(ref position, radarRange, Detectable.IndexGroup, _reusableNeighborList))
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
                float ld = distance / radarRange;
                // Then apply a exponential fall-off, and make it cap a little
                // early to get the 100% alpha when nearby, not only when
                // exactly on top of the object ;)
                ld = System.Math.Min(1, (1.1f - ld * ld * ld) * 1.1f);

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
                    var scale = center.X / System.Math.Abs(iconPosition.X);
                    iconPosition.X *= scale;
                    iconPosition.Y *= scale;
                }
                else if (iconPosition.Y > center.Y || iconPosition.Y < -center.Y)
                {
                    // Out of screen on the Y axis. Guaranteed to be in bound
                    // for X axis, though. Diameter down.
                    var scale = center.Y / System.Math.Abs(iconPosition.Y);
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
                origin.Y = -_distanceOffset;
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
        private RadarDirection GetRadarDirection(ref Vector2 position, ref Rectangle bounds)
        {
            if (position.X == bounds.Left)
            {
                if (position.Y == bounds.Top)
                {
                    return RadarDirection.TopLeft;
                }
                else if (position.Y == bounds.Bottom)
                {
                    return RadarDirection.BottomLeft;
                }
                else
                {
                    return RadarDirection.Left;
                }
            }
            else if (position.X == bounds.Right)
            {
                if (position.Y == bounds.Top)
                {
                    return RadarDirection.TopRight;
                }
                else if (position.Y == bounds.Bottom)
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
                if (position.Y == bounds.Top)
                {
                    return RadarDirection.Top;
                }
                else if (position.Y == bounds.Bottom)
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
        private static readonly string[] _unitNames = new[] { "", "k", "m", "g", "t", "p", "e", "z", "y" };

        /// <summary>
        /// Formats a distance to a string to be displayed in a radar icon.
        /// </summary>
        /// <param name="distance">The distance to format.</param>
        /// <returns>The formatted distance.</returns>
        private string FormatDistance(float distance)
        {
            // Divide by thousand until we're blow it, and count the number
            // of divisions to get the unit.
            int unit = 0;
            while (distance > 1000)
            {
                ++unit;
                distance /= 1000;
            }
            string unitName = "?";
            if (unit < _unitNames.Length)
            {
                unitName = _unitNames[unit];
            }
            // Don't show the post-comma digit if we're above 100 of the
            // current unit.
            if (distance > 100)
            {
                return string.Format("{0:f0}{1}", distance, unitName);
            }
            else
            {
                return string.Format("{0:f1}{1}", distance, unitName);
            }
        }

        #endregion

    }
}
