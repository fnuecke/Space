using Engine.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Space.Data
{
    public sealed class Projectile : IPacketizable
    {
        /// <summary>
        /// The texture to use to render the projectile type.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Texture = string.Empty;

        /// <summary>
        /// Name of the particle effect to use for this projectile type.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string Effect = string.Empty;

        /// <summary>
        /// The collision radius of the projectile.
        /// </summary>
        public float CollisionRadius;

        /// <summary>
        /// Whether this projectile type can be hit by other projectiles (e.g.
        /// missiles may be shot down, but normal projectiles should not
        /// interact).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public bool CanBeShot = false;

        /// <summary>
        /// The initial directed velocity of the projectile. This is rotated
        /// according to the emitters rotation. The set value applies directly
        /// if the emitter is facing to the right (i.e. is at zero rotation).
        /// </summary>
        [ContentSerializer(Optional = true)]
        public Vector2 InitialVelocity = Vector2.Zero;

        /// <summary>
        /// Initial orientation of the projectile. As with the initial
        /// velocity, this is rotated by the emitters rotation, and the
        /// rotation applies directly if the emitter is facing to the right,
        /// i.e. its own rotation is zero.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float InitialRotation = 0;

        /// <summary>
        /// Acceleration force applied to this projectile.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float AccelerationForce = 0;

        /// <summary>
        /// The friction used to slow the projectile down.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float Friction = 0;

        /// <summary>
        /// The time this projectile will stay alive before disappearing,
        /// in seconds.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public float TimeToLive = 5;

        #region Serialization
        
        public Packet Packetize(Packet packet)
        {
            return packet
                .Write(Texture)
                .Write(Effect)
                .Write(CollisionRadius)
                .Write(CanBeShot)
                .Write(InitialVelocity)
                .Write(InitialRotation)
                .Write(AccelerationForce)
                .Write(Friction)
                .Write(TimeToLive);
        }

        public void Depacketize(Packet packet)
        {
            Texture = packet.ReadString();
            Effect = packet.ReadString();
            CollisionRadius = packet.ReadSingle();
            CanBeShot = packet.ReadBoolean();
            InitialVelocity = packet.ReadVector2();
            InitialRotation = packet.ReadSingle();
            AccelerationForce = packet.ReadSingle();
            Friction = packet.ReadSingle();
            TimeToLive = packet.ReadSingle();
        }

        #endregion
    }
}
