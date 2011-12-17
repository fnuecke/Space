using System.Text;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Components
{
    public class PhysicsDrawComponent : IComponent
    {
        /// <summary>
        /// The physics component this renderer draws.
        /// </summary>
        public PhysicsComponent PhysicsComponent { get; set; }

        /// <summary>
        /// The name of the texture to use for rendering the physics object.
        /// </summary>
        public string TextureName { get { return _textureName; } set { _textureName = value; _texture = null; } }

        private string _textureName;

        private Texture2D _texture;

        public PhysicsDrawComponent(PhysicsComponent physicsComponent)
        {
            this.PhysicsComponent = physicsComponent;
        }

        public PhysicsDrawComponent()
        {
        }

        public void Update(object parameterization)
        {
            if (parameterization is DrawParameterization)
            {
                var p = (DrawParameterization)parameterization;
                if (_texture == null)
                {
                    _texture = p.Content.Load<Texture2D>(TextureName);
                }

                p.SpriteBatch.Draw(_texture,
                new Rectangle(PhysicsComponent.Position.X + (int)p.Translation.X,
                              PhysicsComponent.Position.Y + (int)p.Translation.Y,
                              _texture.Width / 2, _texture.Height / 2),
                null,
                Color.White,
                PhysicsComponent.Rotation,
                new Vector2(_texture.Width / 2, _texture.Height / 2),
                SpriteEffects.None,
                0);
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Packetize(Serialization.Packet packet)
        {
            packet.Write(TextureName);
        }

        public void Depacketize(Serialization.Packet packet, Serialization.IPacketizerContext context)
        {
            TextureName = packet.ReadString();
        }

        public void Hash(Util.Hasher hasher)
        {
            hasher.Put(Encoding.UTF8.GetBytes(TextureName));
        }
    }
}
