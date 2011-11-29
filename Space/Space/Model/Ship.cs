using Engine.Math;
using Engine.Physics;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Commands;
using SpaceData;

namespace Space.Model
{
    class Ship : Sphere<GameState, IGameObject, GameCommandType, PlayerInfo>, IGameObject
    {
        /// <summary>
        /// Time in ticks it takes before a ship may respawn.
        /// </summary>
        public const long RespawnTime = 1000;

        /// <summary>
        /// The last frame this ship was destroyed in.
        /// </summary>
        protected long lastDestroyed;

        public bool IsAlive { get { return true /* State.CurrentFrame - lastDestroyed > RespawnTime */; } }

        public int Player { get; private set; }

        public FPoint Position { get { return position; } }

        private ShipData data;

        private Texture2D texture;

        public Ship()
        {
        }

        public Ship(ShipData data, Texture2D texture, int player)
            : base(data.Radius)
        {
            this.data = data;
            this.texture = texture;
            this.Player = player;
        }

        public void Accelerate(Direction direction)
        {
            acceleration = DirectionConversion.DirectionToFPoint(direction) * data.Acceleration;
        }

        public void StopMovement()
        {
            acceleration = FPoint.Zero;
        }

        public void TurnLeft()
        {
            speedRotation = -data.RotationSpeed;
        }

        public void TurnRight()
        {
            speedRotation = data.RotationSpeed;
        }

        public void StopRotating()
        {
            speedRotation = (Fixed)0;
        }

        public override void Update()
        {
            if (IsAlive)
            {
                base.Update();
            }
        }

        public override void PostUpdate()
        {
            if (IsAlive)
            {
                base.PostUpdate();

                foreach (var collideable in State.Collideables)
                {
                    if (collideable.Intersects(this.radius, ref this.previousPosition, ref this.position))
                    {
                        collideable.NotifyOfCollision();
                        this.NotifyOfCollision();
                    }
                }
            }
        }

        public override void NotifyOfCollision()
        {
            lastDestroyed = State.CurrentFrame;
        }

        public override object Clone()
        {
            return this.MemberwiseClone();
        }

        public void Draw(GameTime gameTime, Vector2 translation, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture,
                new Rectangle(position.X.IntValue + (int)translation.X, position.Y.IntValue + (int)translation.Y,
                              texture.Width, texture.Height),
                null,
                Color.White,
                (float)rotation.DoubleValue,
                new Vector2(texture.Width / 2, texture.Height / 2),
                SpriteEffects.None,
                0);
        }

        public override void Packetize(Packet packet)
        {
            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet)
        {
            base.Packetize(packet);
        }
    }
}
