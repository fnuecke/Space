using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>This component will render a texture at a fixed position, with a fixed rotation.</summary>
    public class StaticTextureDrawable : Component, IDrawable
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

        /// <summary>Gets or sets the position to draw the texture at.</summary>
        /// <value>The position.</value>
        public WorldPoint Position { get; set; }

        /// <summary>Gets or sets the rotation to draw the texture with.</summary>
        /// <value>The rotation.</value>
        public float Rotation { get; set; }

        /// <summary>Gets or sets the scale to draw the texture with.</summary>
        /// <value>The scale.</value>
        public float Scale { get; set; }

        /// <summary>Gets or sets the color tint to draw the texture with.</summary>
        /// <value>The tint.</value>
        public Color Tint { get; set; }

        /// <summary>Gets or sets the name of the texture to draw.</summary>
        /// <value>The name of the texture.</value>
        public string TextureName
        {
            get { return _textureName; }
            set
            {
                _texture = null;
                _textureName = value;
            }
        }

        #endregion

        #region Fields

        /// <summary>The name of the texture we should draw.</summary>
        private string _textureName;

        /// <summary>The actual texture we draw. If this is null we will try to load it in the next Draw call.</summary>
        [PacketizerIgnore]
        private Texture2D _texture;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherDrawable = (StaticTextureDrawable) other;
            Position = otherDrawable.Position;
            Rotation = otherDrawable.Rotation;
            Scale = otherDrawable.Scale;
            Tint = otherDrawable.Tint;
            _textureName = otherDrawable._textureName;
            _texture = otherDrawable._texture;

            return this;
        }

        /// <summary>Initializes the component.</summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="tint">The tint.</param>
        /// <returns></returns>
        public StaticTextureDrawable Initialize(
            string textureName, WorldPoint position, float rotation, float scale, Color tint)
        {
            TextureName = textureName;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Tint = tint;

            return this;
        }

        /// <summary>Initializes the component.</summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="scale">The scale.</param>
        /// <returns></returns>
        public StaticTextureDrawable Initialize(
            string textureName, WorldPoint position, float rotation = 0, float scale = 1)
        {
            return Initialize(textureName, position, rotation, scale, Color.White);
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Position = WorldPoint.Zero;
            Rotation = 0;
            Scale = 1;
            Tint = Color.White;
            _textureName = null;
            _texture = null;
        }

        #endregion

        #region Logic

        /// <summary>Draws the texture at the set position with the set properties.</summary>
        /// <param name="batch">The sprite batch that may be used to render textures.</param>
        /// <param name="translation">The translation to apply when drawing.</param>
        public void Draw(SpriteBatch batch, WorldPoint translation)
        {
            batch.Draw(
                _texture,
                (Vector2) (Position + translation),
                null,
                Tint,
                Rotation,
                Vector2.Zero,
                Scale,
                SpriteEffects.None,
                0);
        }

        public void LoadContent(ContentManager content, IGraphicsDeviceService graphics)
        {
            // Try to load our texture if we don't have it yet.
            if (_texture == null)
            {
                if (string.IsNullOrWhiteSpace(_textureName))
                {
                    // Cannot render if we have no texture.
                    return;
                }
                _texture = content.Load<Texture2D>(_textureName);
            }
        }

        #endregion

        #region Serialization

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            _texture = null;
        }

        #endregion
    }
}