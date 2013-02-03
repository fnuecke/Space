using System;
using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Data;
using Space.Util;

namespace Space.ComponentSystem.Systems
{
    /// <summary>This system handles rendering whatever the local player's radar picks up.</summary>
    [Packetizable(false), PresentationOnlyAttribute]
    public sealed class RadarRenderSystem : AbstractSystem
    {
        #region Types

        /// <summary>Readable direction indexes for the radar images.</summary>
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

        /// <summary>Width of a single radar icon.</summary>
        private const int RadarIconWidth = 48;

        /// <summary>Height of a single radar icon.</summary>
        private const int RadarIconHeight = 48;

        /// <summary>Vertical offset of the distance number display relative to the center of the radar icons.</summary>
        private const int DistanceOffset = 5;
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should perform updates and react to events.</summary>
        [PublicAPI]
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The spritebatch to use for rendering.</summary>
        private SpriteBatch _spriteBatch;

        /// <summary>Background image for radar icons.</summary>
        private readonly Texture2D[] _radarDirection = new Texture2D[8];

        /// <summary>Background for rendering the distance to a target.</summary>
        private Texture2D _radarDistance;

        /// <summary>Font used to render the distance on radar icons.</summary>
        private SpriteFont _distanceFont;

        #endregion

        #region Single-Allocation

        /// <summary>
        ///     Reused for iterating components. This should be a sorted set, to avoid random order of elements, leading to
        ///     random flipping of render order. (looks pretty much like z-fighting then)
        /// </summary>
        private readonly ISet<int> _reusableNeighborList = new SortedSet<int>();

        #endregion

        #region Logic

        /// <summary>Render our local radar system, with whatever detectables are close enough.</summary>
        [MessageCallback]
        public void OnDraw(Draw message)
        {
            if (!Enabled)
            {
                return;
            }

            // Get local player's avatar.
            var avatar = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
            if (avatar <= 0)
            {
                return;
            }

            // Get info on the local player's ship.
            var info = ((ShipInfo) Manager.GetComponent(avatar, ShipInfo.TypeId));

            // Get the index we use for looking up nearby objects.
            var index = (IndexSystem) Manager.GetSystem(IndexSystem.TypeId);

            // Get camera information.
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Get the actual position we're rendering at. Note that this will
            // actually allow the player to "extend" his radar by the maximum
            // distance of the camera to his ship. That'll be a negligible
            // amount, however, in relation to the total radar range.
            var position = camera.CameraPosition;

            // Get zoom from camera.
            var zoom = camera.Zoom;

            // Figure out the overall range of our radar system.
            var radarRange = UnitConversion.ToSimulationUnits(info.RadarRange);

            // Get bounds in which to display the icon.
            var screenBounds = (RectangleF) _spriteBatch.GraphicsDevice.Viewport.Bounds;

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
            innerBounds.Inflate(-backgroundOrigin.X, -backgroundOrigin.Y);

            // Convert to simulation units for checking.
            screenBounds = UnitConversion.ToSimulationUnits(screenBounds);
            screenBounds.Offset(-center.X, -center.Y);

            // Now this is the tricky part: we take the minimal bounding sphere
            // (or rather, circle) that fits our screen space. For each
            // detectable entity we then pick the point on this circle that's
            // in the direction of that entity.
            // Because the only four points where this'll actually be in screen
            // space will be the four corners, we'll map them down to the edges
            // again. See below for that.
            var a = center.X - backgroundOrigin.X;
            var b = center.Y - backgroundOrigin.Y;
            var radius = (float) Math.Sqrt(a * a + b * b);

            // Loop through all our neighbors.
            index[Detectable.IndexId].Find(position, radarRange, _reusableNeighborList);

            // Begin drawing.
            _spriteBatch.Begin();
            foreach (IIndexable neighbor in _reusableNeighborList.Select(Manager.GetComponentById))
            {
                // Get the components we need.
                var neighborTransform = Manager.GetComponent(neighbor.Entity, TransformTypeId) as ITransform;
                var neighborDetectable = Manager.GetComponent(neighbor.Entity, Detectable.TypeId) as Detectable;
                var faction = (Faction) Manager.GetComponent(neighbor.Entity, Faction.TypeId);

                // Bail if we're missing something.
                if (neighborTransform == null || neighborDetectable == null || neighborDetectable.Texture == null)
                {
                    continue;
                }

                // We don't show the icons for anything that's inside our
                // viewport. Get the position of the detectable inside our
                // viewport. This will also serve as our direction vector.
                var direction = (Vector2) (neighborTransform.Position - position);
                var distance = direction.Length();

                // Check if the object's inside. If so, skip it. Take camera
                // zoom into account here.
                if (screenBounds.Contains(direction.X * zoom, direction.Y * zoom))
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
                    distance -= UnitConversion.ToSimulationUnits(iconPosition.Length()) / zoom;
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
                _spriteBatch.Draw(
                    _radarDirection[(int) GetRadarDirection(ref iconPosition, ref innerBounds)],
                    iconPosition,
                    null,
                    color,
                    0,
                    backgroundOrigin,
                    ld,
                    SpriteEffects.None,
                    0);

                // Get the texture origin (middle of the texture).
                Vector2 origin;
                origin.X = neighborDetectable.Texture.Width / 2.0f;
                origin.Y = neighborDetectable.Texture.Height / 2.0f;

                // And draw that, too.
                _spriteBatch.Draw(
                    neighborDetectable.Texture,
                    iconPosition,
                    null,
                    Color.White * ld,
                    neighborDetectable.RotateIcon ? neighborTransform.Angle : 0,
                    origin,
                    ld,
                    SpriteEffects.None,
                    0);

                // Draw the distance to the object.
                _spriteBatch.Draw(
                    _radarDistance,
                    iconPosition,
                    null,
                    Color.White * ld,
                    0,
                    backgroundOrigin,
                    ld,
                    SpriteEffects.None,
                    0);

                string formattedDistance = FormatDistance(distance);
                origin.X = _distanceFont.MeasureString(formattedDistance).X / 2f;
                origin.Y = -DistanceOffset;
                _spriteBatch.DrawString(
                    _distanceFont,
                    formattedDistance,
                    iconPosition,
                    Color.White * ld,
                    0,
                    origin,
                    ld,
                    SpriteEffects.None,
                    0);
            }
            // Done drawing.
            _spriteBatch.End();

            // Clear the list for the next run.
            _reusableNeighborList.Clear();
        }

        /// <summary>
        ///     Gets the actual direction background to display. This checks which borders the icon touches and returns the
        ///     according direction.
        /// </summary>
        /// <param name="position">The icon's position.</param>
        /// <param name="bounds">The bounds.</param>
        /// <returns>The direction of the border contact of the icon.</returns>
        private static RadarDirection GetRadarDirection(ref Vector2 position, ref RectangleF bounds)
        {
            if (Math.Abs(position.X - bounds.Left) < 0.001f)
            {
                if (Math.Abs(position.Y - bounds.Top) < 0.001f)
                {
                    return RadarDirection.TopLeft;
                }
                if (Math.Abs(position.Y - bounds.Bottom) < 0.001f)
                {
                    return RadarDirection.BottomLeft;
                }
                return RadarDirection.Left;
            }
            if (Math.Abs(position.X - bounds.Right) < 0.001f)
            {
                if (Math.Abs(position.Y - bounds.Top) < 0.001f)
                {
                    return RadarDirection.TopRight;
                }
                if (Math.Abs(position.Y - bounds.Bottom) < 0.001f)
                {
                    return RadarDirection.BottomRight;
                }
                return RadarDirection.Right;
            }
            if (Math.Abs(position.Y - bounds.Top) < 0.001f)
            {
                return RadarDirection.Top;
            }
            if (Math.Abs(position.Y - bounds.Bottom) < 0.001f)
            {
                return RadarDirection.Bottom;
            }
            // Should not be possible!
            throw new InvalidOperationException("Wait, wut?");
        }

        /// <summary>List of SI units, used for distance formatting.</summary>
        private static readonly string[] UnitNames = new[] {"", "k", "m", "g", "t", "p", "e", "z", "y"};

        /// <summary>Formats a distance to a string to be displayed in a radar icon.</summary>
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
        
        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            _spriteBatch = new SpriteBatch(message.Graphics.GraphicsDevice);

            var content = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content;

            _radarDirection[(int) RadarDirection.Top] = content.Load<Texture2D>("Textures/Radar/top");
            _radarDirection[(int) RadarDirection.Left] = content.Load<Texture2D>("Textures/Radar/left");
            _radarDirection[(int) RadarDirection.Right] = content.Load<Texture2D>("Textures/Radar/right");
            _radarDirection[(int) RadarDirection.Bottom] = content.Load<Texture2D>("Textures/Radar/bottom");
            _radarDirection[(int) RadarDirection.TopLeft] = content.Load<Texture2D>("Textures/Radar/top_left");
            _radarDirection[(int) RadarDirection.TopRight] = content.Load<Texture2D>("Textures/Radar/top_right");
            _radarDirection[(int) RadarDirection.BottomLeft] = content.Load<Texture2D>("Textures/Radar/bottom_left");
            _radarDirection[(int) RadarDirection.BottomRight] = content.Load<Texture2D>("Textures/Radar/bottom_right");
            _radarDistance = content.Load<Texture2D>("Textures/Radar/distance");
            _distanceFont = content.Load<SpriteFont>("Fonts/visitor");
        }

        [MessageCallback]
        public void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
        }

        #endregion
    }
}