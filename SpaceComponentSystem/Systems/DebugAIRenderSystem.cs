using System;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    [Packetizable(false)]
    public sealed class DebugAIRenderSystem : AbstractComponentSystem<ArtificialIntelligence>, IDrawingSystem
    {
        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>The spritebatch to use for rendering.</summary>
        private SpriteBatch _spriteBatch;

        /// <summary>The font to use for rendering.</summary>
        private SpriteFont _font;

        /// <summary>Arrow texture to render indication of where AI is headed.</summary>
        private Texture2D _arrow;

        #endregion

        #region Logic

        /// <summary>Draws the system.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem) Manager.GetSystem(CameraSystem.TypeId);

            // Get camera transform.
            var cameraTransform = camera.Transform;
            var cameraTranslation = camera.Translation;
            var interpolation = (InterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);

            // Iterate over all visible entities.
            _spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, cameraTransform);
            foreach (var entity in camera.VisibleEntities)
            {
                var ai = (ArtificialIntelligence) Manager.GetComponent(entity, ArtificialIntelligence.TypeId);
                if (ai != null)
                {
                    FarPosition position;
                    float angle;
                    interpolation.GetInterpolatedTransform(entity, out position, out angle);
                    position = position + cameraTranslation;

                    // Render vegetative influences.
                    DrawArrow((Vector2) position, ai.GetLastEscape(), Color.Red);

                    DrawArrow((Vector2) position, ai.GetLastSeparation(), Color.Yellow);

                    DrawArrow((Vector2) position, ai.GetLastCohesion(), Color.Blue);

                    DrawArrow((Vector2) position, ai.GetLastFormation(), Color.Teal);

                    // Render target.
                    DrawArrow((Vector2) position, ai.GetBehaviorTargetDirection(), Color.Green);

                    // Render current state.
                    position = FarUnitConversion.ToScreenUnits(position);
                    position.Y += 20; // don't intersect with entity id if visible
                    _spriteBatch.DrawString(
                        _font,
                        "AI: " + ai.CurrentBehavior,
                        (Vector2) position,
                        Color.White,
                        0,
                        Vector2.Zero,
                        1f / camera.CameraZoom,
                        SpriteEffects.None,
                        0);
                }
            }
            _spriteBatch.End();
        }

        private void DrawArrow(Vector2 start, Vector2 toEnd, Color color)
        {
            start = XnaUnitConversion.ToScreenUnits(start);
            toEnd = XnaUnitConversion.ToScreenUnits(toEnd);
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

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<GraphicsDeviceCreated>(OnGraphicsDeviceCreated);
            Manager.AddMessageListener<GraphicsDeviceDisposing>(OnGraphicsDeviceDisposing);
        }

        private void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            _spriteBatch = new SpriteBatch(message.Graphics.GraphicsDevice);
            var content = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content;
            _font = content.Load<SpriteFont>("Fonts/ConsoleFont");
            _arrow = content.Load<Texture2D>("Textures/arrow");
        }

        private void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
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