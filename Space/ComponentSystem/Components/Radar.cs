using System;
using System.Linq;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Data;
using Space.Data.Modules;

namespace Space.ComponentSystem.Components
{
    class Radar : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// Background image for radar icons.
        /// </summary>
        private Texture2D _background;

        #endregion

        #region Constructor

        public Radar()
        {
            DrawOrder = 100;
        }

        #endregion

        #region Logic

        private static readonly float _sqrt2 = (float)System.Math.Sqrt(2);

        /// <summary>
        /// Draws the 
        /// </summary>
        /// <param name="parameterization">The parameterization to use for this update.</param>
        public override void Draw(object parameterization)
        {
            // Get parameterization in proper type.
            var args = (RendererParameterization)parameterization;

            if (_background == null)
            {
                _background = args.Content.Load<Texture2D>("Textures/radar_background");
            }

            // Fetch all the components we need.
            var transform = Entity.GetComponent<Transform>();
            var index = Entity.Manager.SystemManager.GetSystem<IndexSystem>();
            var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();

            // Bail if we're missing something.
            if (transform == null && index == null && modules == null)
            {
                return;
            }

            // Figure out the overall range of our radar system.
            float radarRange = 0;

            // Get equipped sensor modules.
            var sensors = modules.GetModules<SensorModule>();
            if (sensors != null)
            {
                // TODO in case we're adding sensor types (anti-cloaking, ...) check this one's actually a radar.
                radarRange += sensors.Sum(module => module.Range);
            }

            // Apply any modifiers from equipment.
            radarRange = modules.GetValue(EntityAttributeType.SensorRange, radarRange);
            
            // Get our viewport. Needed to compute the overall positioning of
            // our icons on the screen.
            var viewport = args.SpriteBatch.GraphicsDevice.Viewport;

            // Get the screen's center, used for diverse computations, and as
            // a center for relative computations (because the player's always
            // rendered in the center of the screen).
            Vector2 center;
            center.X = viewport.Width / 2f;
            center.Y = viewport.Height / 2f;

            // Get the texture origin (middle of the texture).
            Vector2 backgroundOrigin;
            backgroundOrigin.X = _background.Width / 2.0f;
            backgroundOrigin.Y = _background.Height / 2.0f;

            // Now this is the tricky part: we take the minimal bounding sphere
            // (or rather, circle) that fits our screen space. For each
            // detectable entity we then pick the point on this circle that's
            // in the direction of that entity.
            // Because the only four points where this'll actually be in screen
            // space will be the four corners, we'll map them down to the edges
            // again. See below for that.
            float radius = (float)System.Math.Sqrt(center.X * center.X + center.Y * center.Y);

            // Loop through all our neighbors.
            foreach (var neighbor in index.GetNeighbors(Entity, radarRange, Detectable.IndexGroup))
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
                var direction = neighborTransform.Translation - transform.Translation;

                // Get bounds in which to display the icon.
                Rectangle screenBounds = viewport.Bounds;

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
                // size of the icon, so deflate it by that.
                screenBounds.Inflate(-(int)backgroundOrigin.X, -(int)backgroundOrigin.Y);

                // Clamp our coordinates.
                iconPosition.X = MathHelper.Clamp(iconPosition.X, screenBounds.Left, screenBounds.Right);
                iconPosition.Y = MathHelper.Clamp(iconPosition.Y, screenBounds.Top, screenBounds.Bottom);

                // And, finally, draw it. First the background.
                args.SpriteBatch.Draw(_background, iconPosition, null, color, 0,
                    backgroundOrigin, 1, SpriteEffects.None, 0);

                // Get the texture origin (middle of the texture).
                Vector2 origin;
                origin.X = neighborDetectable.Texture.Width / 2.0f;
                origin.Y = neighborDetectable.Texture.Height / 2.0f;

                // And draw that, too.
                args.SpriteBatch.Draw(neighborDetectable.Texture, iconPosition, null, color, 0,
                    origin, 1, SpriteEffects.None, 0);
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsDrawParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(RendererParameterization) ||
                parameterizationType.IsSubclassOf(typeof(RendererParameterization));
        }
        #endregion
    }
}
