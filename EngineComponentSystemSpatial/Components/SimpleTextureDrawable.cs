using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.Math;
using Engine.Serialization;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>This component will render a texture at a fixed position, with a fixed rotation.</summary>
    public class SimpleTextureDrawable : Component, IDrawable
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId() { return TypeId; }

        #endregion

        #region Properties

        /// <summary>Gets or sets the scale to draw the texture with.</summary>
        /// <value>The scale.</value>
        [PublicAPI]
        public float Scale { get; set; }

        /// <summary>Gets or sets the color tint to draw the texture with.</summary>
        /// <value>The tint.</value>
        [PublicAPI]
        public Color Tint { get; set; }

        /// <summary>Gets or sets the name of the texture to draw.</summary>
        /// <value>The name of the texture.</value>
        [PublicAPI]
        public string TextureName
        {
            get { return _textureName; }
            set
            {
                _textureName = value;
                _texture = null;
            }
        }

        /// <summary>Gets the actual texture.</summary>
        [PublicAPI]
        public Texture2D Texture
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_textureName))
                {
                    LoadContent(((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content, null);
                }
                return _texture;
            }
        }

        /// <summary>The area this drawable needs to render itself.</summary>
        [PublicAPI]
        public RectangleF Bounds
        {
            get
            {
                var bounds = (RectangleF) _texture.Bounds;
                bounds.Offset(-_texture.Width / 2f, -_texture.Height / 2f);
                bounds.Inflate(
                    -bounds.Width / 2f * (1f - Scale),
                    -bounds.Height / 2f * (1f - Scale));
                return bounds;
            }
        }

        #endregion

        #region Fields

        /// <summary>The name of the texture we should draw.</summary>
        private string _textureName;

        /// <summary>The actual texture we draw. If this is null we will try to load it in the next Draw call.</summary>
        [PacketizeIgnore]
        private Texture2D _texture;

        #endregion

        #region Initialization

        /// <summary>Initializes the component.</summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="tint">The tint.</param>
        /// <returns></returns>
        public SimpleTextureDrawable Initialize(string textureName, Color tint, float scale = 1f)
        {
            TextureName = textureName;
            Scale = scale;
            Tint = tint;

            return this;
        }

        /// <summary>Initializes the component.</summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="scale">The scale.</param>
        /// <returns></returns>
        public SimpleTextureDrawable Initialize(string textureName, float scale = 1f) { return Initialize(textureName, Color.White, scale); }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Scale = 1f;
            Tint = Color.White;
            _textureName = null;
            _texture = null;
        }

        #endregion

        #region Logic

        /// <summary>Draws the texture with the specified parameters.</summary>
        /// <param name="batch">The sprite batch that may be used to render textures.</param>
        /// <param name="position">The position at which to draw. This already includes camera and object position.</param>
        /// <param name="angle">The angle at which to draw. This includes the object angle.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="effects">The effects.</param>
        /// <param name="layerDepth">The base layer depth to use, used for tie breaking.</param>
        public void Draw(
            SpriteBatch batch, Vector2 position, float angle, float scale, SpriteEffects effects, float layerDepth)
        {
            Vector2 origin;
            origin.X = Texture.Width / 2f;
            origin.Y = Texture.Height / 2f;

            batch.Draw(
                Texture,
                position,
                null,
                Tint,
                angle,
                origin,
                scale * Scale,
                effects,
                layerDepth);
        }

        public void LoadContent(ContentManager content, IGraphicsDeviceService graphics)
        {
            // Try to load our texture if we don't have it yet.
            if (!string.IsNullOrWhiteSpace(_textureName))
            {
                _texture = content.Load<Texture2D>(_textureName);
            }
            else
            {
                _texture = null;
            }
        }

        #endregion

        #region Serialization

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet) { _texture = null; }

        #endregion
    }
}