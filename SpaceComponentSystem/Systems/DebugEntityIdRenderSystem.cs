using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Renders entity ids at their position, if they have a position.</summary>
    [Packetizable(false)]
    public sealed class DebugEntityIdRenderSystem : AbstractSystem
    {
        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        [PublicAPI]
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
        [MessageCallback]
        public void OnDraw(Draw message)
        {
            if (!Enabled)
            {
                return;
            }

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
                    int x, y, subX, subY;
                    BitwiseMagic.Unpack(CellSystem.GetCellIdFromCoordinates(transform.Position), out x, out y);
                    BitwiseMagic.Unpack(CellSystem.GetSubCellIdFromCoordinates(transform.Position), out subX, out subY);
                    var text = string.Format(
                        "ID: {0} @ {1} / {2}\nCell: {3}:{4}, SubCell: {5}:{6}",
                        transform.Entity,
                        transform.Position,
                        transform.Angle,
                        x,
                        y,
                        subX,
                        subY);

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

        [MessageCallback]
        public void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            _spriteBatch = new SpriteBatch(message.Graphics.GraphicsDevice);
            _font = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content.Load<SpriteFont>("Fonts/ConsoleFont");
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