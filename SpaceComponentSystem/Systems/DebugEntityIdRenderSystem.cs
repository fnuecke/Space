using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Renders entity ids at their position, if they have a position.</summary>
    public sealed class DebugEntityIdRenderSystem : AbstractSystem, IDrawingSystem
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

        #endregion

        #region Logic
        
        /// <summary>Store for performance.</summary>
        private static readonly int TransformTypeId = Engine.ComponentSystem.Manager.GetComponentTypeId<ITransform>();

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
                var transform = (ITransform) Manager.GetComponent(entity, TransformTypeId);
                if (transform != null)
                {
                    int cx, cy, scx, scy;
                    BitwiseMagic.Unpack(CellSystem.GetCellIdFromCoordinates(transform.Position), out cx, out cy);
                    BitwiseMagic.Unpack(CellSystem.GetSubCellIdFromCoordinates(transform.Position), out scx, out scy);
                    var text = string.Format(
                        "ID: {0} @ {1} / {2}\nCell: {3}:{4}, SubCell: {5}:{6}",
                        transform.Entity,
                        transform.Position,
                        transform.Angle,
                        cx,
                        cy,
                        scx,
                        scy);

                    FarPosition position;
                    float angle;
                    interpolation.GetInterpolatedTransform(transform.Entity, out position, out angle);
                    position = FarUnitConversion.ToScreenUnits(position + cameraTranslation);
                    _spriteBatch.DrawString(
                        _font,
                        text,
                        (Vector2) position,
                        Color.White,
                        0,
                        Vector2.Zero,
                        1f / camera.CameraZoom,
                        SpriteEffects.None,
                        1);
                }
            }
            _spriteBatch.End();
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
            _font = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content.Load<SpriteFont>("Fonts/ConsoleFont");
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