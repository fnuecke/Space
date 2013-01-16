using System;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Systems;
using IDrawable = Engine.ComponentSystem.Spatial.Components.IDrawable;

namespace Space.ComponentSystem.Components
{
    /// <summary>Takes care of rendering ships.</summary>
    public class ShipDrawable : Component, IDrawable
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>The area this drawable needs to render itself.</summary>
        public RectangleF Bounds
        {
            get
            {
                // We could compute the size, but as this would trigger creation of the cache entry,
                // this is generally a bad idea (TM), so let's throw until a use case arises where
                // it's absolutely necessary to query the render size of this.
                throw new NotSupportedException();
                /*
                var shapeSystem = (ShipShapeSystem) Manager.GetSystem(ShipShapeSystem.TypeId);
                Texture2D texture;
                Vector2 offset;
                shapeSystem.GetTexture(Entity, out texture, out offset);
                var bounds = (RectangleF) texture.Bounds;
                bounds.Offset(offset);
                return bounds;
                */
            }
        }

        /// <summary>Gets the fallback texture if the ship has no equipment.</summary>
        public Texture2D FallbackTexture
        {
            get
            {
                if (_fallbackTextureName == null && !string.IsNullOrWhiteSpace(_fallbackTextureName))
                {
                    var graphics = (GraphicsDeviceSystem) Manager.GetSystem(GraphicsDeviceSystem.TypeId);
                    _fallbackTexture = graphics.Content.Load<Texture2D>(_fallbackTextureName);
                }
                return _fallbackTexture;
            }
        }

        #endregion

        #region Fields
        
        private Color _tint;

        private string _fallbackTextureName;

        [PacketizerIgnore]
        private Texture2D _fallbackTexture;

        #endregion

        #region Initialization

        public void Initialize(string texture, Color tint)
        {
            _fallbackTextureName = texture;
            _tint = tint;
        }

        public override void Reset()
        {
            base.Reset();

            _tint = Color.White;
            _fallbackTextureName = null;
            _fallbackTexture = null;
        }

        #endregion

        /// <summary>
        ///     Draws the entity of the component. The implementation will generally depend on the type of entity (via the type of
        ///     <see cref="IDrawable"/> implementation).
        /// </summary>
        /// <param name="batch">The sprite batch that may be used to render textures.</param>
        /// <param name="position">The position at which to draw. This already includes camera and object position.</param>
        /// <param name="angle">The angle at which to draw. This includes the object angle.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="effects">The effects.</param>
        /// <param name="layerDepth">The base layer depth to use, used for tie breaking.</param>
        public void Draw(
            SpriteBatch batch, Vector2 position, float angle, float scale, SpriteEffects effects, float layerDepth)
        {
            var shapeSystem = (ShipShapeSystem) Manager.GetSystem(ShipShapeSystem.TypeId);
            Texture2D texture;
            Vector2 offset;
            shapeSystem.GetTexture(Entity, out texture, out offset);
            batch.Draw(
                texture,
                position,
                null,
                _tint,
                angle,
                offset,
                scale,
                effects,
                layerDepth);
        }

        public void LoadContent(ContentManager content, IGraphicsDeviceService graphics)
        {
            if (!string.IsNullOrWhiteSpace(_fallbackTextureName))
            {
                _fallbackTexture = content.Load<Texture2D>(_fallbackTextureName);
            }
            else
            {
                _fallbackTexture = null;
            }
        }

        public override void Depacketize(Engine.Serialization.IReadablePacket packet)
        {
            base.Depacketize(packet);

            _fallbackTexture = null;
        }
    }
}