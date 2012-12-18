using System;
using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    public sealed class DebugAIRenderSystem : AbstractComponentSystem<ArtificialIntelligence>, IDrawingSystem
    {
        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The spritebatch to use for rendering.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The font to use for rendering.
        /// </summary>
        private readonly SpriteFont _font;

        /// <summary>
        /// Arrow texture to render indication of where AI is headed.
        /// </summary>
        private readonly Texture2D _arrow;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugAIRenderSystem"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics device.</param>
        public DebugAIRenderSystem(ContentManager content, GraphicsDevice graphics)
        {
            _spriteBatch = new SpriteBatch(graphics);
            _font = content.Load<SpriteFont>("Fonts/ConsoleFont");
            _arrow = content.Load<Texture2D>("Textures/arrow");
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Get camera transform.
            var cameraTransform = camera.Transform;
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            // Iterate over all visible entities.
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, cameraTransform.Matrix);
            foreach (var entity in camera.VisibleEntities)
            {
                var ai = (ArtificialIntelligence)Manager.GetComponent(entity, ArtificialIntelligence.TypeId);
                if (ai != null)
                {
                    var transform = (Transform)Manager.GetComponent(entity, Transform.TypeId);
                    FarPosition position;
                    interpolation.GetInterpolatedPosition(transform.Entity, out position);
                    position += cameraTransform.Translation;

                    // Render vegetative influences.
                    DrawArrow((Vector2)position, ai.GetLastEscape(), Color.Red);

                    DrawArrow((Vector2)position, ai.GetLastSeparation(), Color.Yellow);

                    DrawArrow((Vector2)position, ai.GetLastCohesion(), Color.Blue);

                    DrawArrow((Vector2)position, ai.GetLastFormation(), Color.Teal);

                    // Render target.
                    DrawArrow((Vector2)position, ai.GetBehaviorTargetDirection(), Color.Green);

                    // Render current state.
                    position.Y += 20; // don't intersect with entity id if visible
                    _spriteBatch.DrawString(_font, "AI: " + ai.CurrentBehavior, (Vector2)position, Color.White, 0,
                        Vector2.Zero, 1f / camera.CameraZoom, SpriteEffects.None, 0);
                }
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
            _spriteBatch.Draw(_arrow, start, null, color,
                              (float)Math.Atan2(toEnd.Y, toEnd.X),
                              new Vector2(0, _arrow.Height / 2f),
                              new Vector2(toEnd.Length() / _arrow.Width, 1),
                              SpriteEffects.None, 0);
        }

        #endregion

        #region Copying

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override AbstractSystem NewInstance()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported by presentation types.
        /// </summary>
        /// <returns>Never.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public override void CopyInto(AbstractSystem into)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
