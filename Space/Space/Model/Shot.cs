using Engine.Math;
using Engine.Physics;
using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;

namespace Space.Model
{
    class Shot : Sphere<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>, IGameObject
    {
        private Texture2D texture;

        public Shot()
        {
        }

        public Shot(string name,FPoint position, FPoint velocity, PacketizerContext context)
        {
            this.position = position;
            this.velocity = velocity;
            this.texture = context.weaponTextures[name];
            context.weaponsSounds[name].Play();
        }

        public override void NotifyOfCollision()
        {
        }

        public override object Clone()
        {
            return this.MemberwiseClone();
        }

        public void Draw(GameTime gameTime, Vector2 translation, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture,
                new Rectangle(position.X.IntValue + (int)translation.X, position.Y.IntValue + (int)translation.Y,
                              texture.Width / 2, texture.Height / 2),
                null,
                Color.Orange,
                (float)rotation.DoubleValue,
                new Vector2(texture.Width / 2, texture.Height / 2),
                SpriteEffects.None,
                0);
        }

        public override void Depacketize(Packet packet, PacketizerContext context)
        {
            texture = context.shipTextures["Sparrow"];

            base.Depacketize(packet, context);
        }
    }
}
