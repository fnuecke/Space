using System.IO;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Factories;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    ///     Special renderer for a planet or moon.
    ///     <para>Draws its atmosphere and shadow based on the sun it orbits.</para>
    /// </summary>
    public sealed class PlanetRenderer : Component
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

        #region Fields

        /// <summary>The factory of this planet, it's "type".</summary>
        [PacketizeIgnore]
        public PlanetFactory Factory;

        /// <summary>The scale at which to render the texture.</summary>
        public float Radius;

        /// <summary>The rotation direction of the planet's surface.</summary>
        public float SurfaceRotation;

        /// <summary>The actual texture with the set name.</summary>
        [PacketizeIgnore]
        public Texture2D Albedo;

        /// <summary>The actual texture with the set name.</summary>
        [PacketizeIgnore]
        public Texture2D Normals;

        /// <summary>The actual texture with the set name.</summary>
        [PacketizeIgnore]
        public Texture2D Specular;

        /// <summary>The actual texture with the set name.</summary>
        [PacketizeIgnore]
        public Texture2D Lights;

        /// <summary>The actual texture with the set name.</summary>
        [PacketizeIgnore]
        public Texture2D Clouds;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified parameters.</summary>
        /// <param name="factory">The factory.</param>
        /// <param name="planetRadius">The planet radius.</param>
        /// <param name="surfaceRotation">The rotation direction of the planet's surface</param>
        /// <returns></returns>
        public PlanetRenderer Initialize(PlanetFactory factory, float planetRadius, float surfaceRotation)
        {
            Factory = factory;
            Radius = planetRadius;
            SurfaceRotation = surfaceRotation;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Factory = null;
            Radius = 0;
            SurfaceRotation = 0f;
            Albedo = null;
            Normals = null;
            Specular = null;
            Lights = null;
            Clouds = null;
        }

        #endregion

        #region Serialization 

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            return base.Packetize(packet)
                       .Write(Factory.Name);
        }

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            Factory = FactoryLibrary.GetFactory(packet.ReadString()) as PlanetFactory;

            Albedo = null;
            Normals = null;
            Specular = null;
            Lights = null;
            Clouds = null;
        }

        /// <summary>Writes a string representation of the object to a string builder.</summary>
        /// <param name="w"> </param>
        /// <param name="indent">The indentation level.</param>
        /// <returns>The string builder, for call chaining.</returns>
        public override StreamWriter Dump(StreamWriter w, int indent)
        {
            base.Dump(w, indent);

            w.AppendIndent(indent).Write("Factory = ");
            w.Write(Factory.Name);

            return w;
        }

        #endregion
    }
}