using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Special renderer for a planet or moon.
    /// 
    /// <para>
    /// Draws its atmosphere and shadow based on the sun it orbits.
    /// </para>
    /// </summary>
    public sealed class PlanetRenderer : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = Engine.ComponentSystem.Manager.GetComponentTypeId(typeof(PlanetRenderer));

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The name of the texture to use for rendering the physics object.
        /// </summary>
        public string TextureName
        {
            get { return _textureName; }
            set
            {
                _textureName = value;
                Texture = null;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The scale at which to render the texture.
        /// </summary>
        public float Radius;

        /// <summary>
        /// The actual texture with the set name.
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// The color to use for tinting when rendering.
        /// </summary>
        public Color PlanetTint;

        /// <summary>
        /// The color tint of this planet's atmosphere.
        /// </summary>
        public Color AtmosphereTint;

        /// <summary>
        /// The rotation direction of the planet's surface.
        /// </summary>
        public Vector2 SurfaceRotation;

        /// <summary>
        /// Actual texture name. Setter is used to invalidate the actual texture reference,
        /// so we need to store this ourselves.
        /// </summary>
        private string _textureName;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherPlanet = (PlanetRenderer)other;
            Radius = otherPlanet.Radius;
            Texture = otherPlanet.Texture;
            PlanetTint = otherPlanet.PlanetTint;
            AtmosphereTint = otherPlanet.AtmosphereTint;
            SurfaceRotation = otherPlanet.SurfaceRotation;
            TextureName = otherPlanet.TextureName;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="planetTexture">The planet texture.</param>
        /// <param name="planetTint">The planet tint.</param>
        /// <param name="planetRadius">The planet radius.</param>
        /// <param name="atmosphereTint">The atmosphere tint.</param>
        /// <param name="surfaceRotation">The rotation direction of the planet's surface</param>
        public PlanetRenderer Initialize(string planetTexture, Color planetTint,
            float planetRadius, Color atmosphereTint, Vector2 surfaceRotation)
        {
            Radius = planetRadius;
            PlanetTint = planetTint;
            AtmosphereTint = atmosphereTint;
            SurfaceRotation = surfaceRotation;
            TextureName = planetTexture;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Radius = 0;
            Texture = null;
            PlanetTint = Color.White;
            AtmosphereTint = Color.Transparent;
            SurfaceRotation = Vector2.Zero;
        }

        #endregion

        #region Serialization 

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Radius)
                .Write(PlanetTint.PackedValue)
                .Write(AtmosphereTint.PackedValue)
                .Write(SurfaceRotation)
                .Write(TextureName);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Radius = packet.ReadSingle();
            PlanetTint.PackedValue = packet.ReadUInt32();
            AtmosphereTint.PackedValue = packet.ReadUInt32();
            SurfaceRotation = packet.ReadVector2();
            TextureName = packet.ReadString();
        }

        /// <summary>
        /// Suppress hashing as this component has no influence on other
        /// components and actual simulation logic.
        /// </summary>
        /// <param name="hasher"></param>
        public override void Hash(Hasher hasher)
        {
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", TextureName=" + TextureName;
        }

        #endregion
    }
}
