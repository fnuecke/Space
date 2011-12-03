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
    class Ship : Sphere<GameState, IGameObject, GameCommandType, PlayerInfo, PacketizerContext>, IGameObject
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

        public int PlayerNumber { get; private set; }

        public ShipData Data { get; private set; }

        private Texture2D texture;

        private Directions accelerationDirection = Directions.None;

        private Fixed targetRotation;

        public Ship()
        {
        }

        public Ship(string name, int player, PacketizerContext context)
        {
            ShipData data = context.shipData[name];
            this.radius = data.Radius;
            this.Data = data;
            this.texture = context.shipTextures[name];
            this.PlayerNumber = player;
        }

        public void Accelerate(Directions direction)
        {
            accelerationDirection |= direction;
            acceleration = DirectionConversion.DirectionToFPoint(accelerationDirection) * Data.Acceleration;
        }

        public void StopAccelerate(Directions direction)
        {
            accelerationDirection &= ~direction;
            acceleration = DirectionConversion.DirectionToFPoint(accelerationDirection) * Data.Acceleration;
        }

        public void RotateTo(Fixed targetAngle)
        {
            targetRotation = targetAngle;
            Fixed deltaAngle = Angle.MinAngle(rotation, targetAngle);
            if (deltaAngle != Fixed.Zero)
            {
                if (deltaAngle > Fixed.Zero)
                {
                    // Rotate right.
                    speedRotation = DirectionConversion.DirectionToFixed(Directions.Right) * Data.RotationSpeed;
                }
                else
                {
                    // Rotate left.
                    speedRotation = DirectionConversion.DirectionToFixed(Directions.Left) * Data.RotationSpeed;
                }
            }
            else
            {
                rotation = targetRotation;
                speedRotation = Fixed.Zero;
            }
        }

        public override void Update()
        {
            if (IsAlive)
            {
                // If we're currently rotating, check if we want to step due to the
                // current update (because we'd overstep our target).
                if (RotationSpeed != Fixed.Zero)
                {
                    var currentDelta = Angle.MinAngle(Rotation, targetRotation);
                    base.Update();
                    var newDelta = Angle.MinAngle(Rotation, targetRotation);
                    if (currentDelta < Fixed.Zero && newDelta >= Fixed.Zero ||
                        currentDelta > Fixed.Zero && newDelta <= Fixed.Zero)
                    {
                        rotation = targetRotation;
                        speedRotation = Fixed.Zero;
                    }
                }
                else
                {
                    // Not rotating, just do a normal update.
                    base.Update();
                }
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

        #region Serialization

        public override void Packetize(Packet packet)
        {
            packet.Write(Data.Name);
            packet.Write(PlayerNumber);

            base.Packetize(packet);
        }

        public override void Depacketize(Packet packet, PacketizerContext context)
        {
            string name = packet.ReadString();
            Data = context.shipData[name];
            texture = context.game.Content.Load<Texture2D>(Data.Texture);

            PlayerNumber = packet.ReadInt32();

            base.Depacketize(packet, context);
        }

        #endregion
    }
}
