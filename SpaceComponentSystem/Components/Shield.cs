using System.IO;
using Engine.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Factories;

namespace Space.ComponentSystem.Components
{
    /// <summary>Represents a shield item, which blocks damage.</summary>
    public sealed class Shield : SpaceItem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>The factory that created this shield.</summary>
        [PacketizerIgnore]
        public ShieldFactory Factory;

        /// <summary>The texture to use as a structure for the shield.</summary>
        [PacketizerIgnore]
        public Texture2D Structure;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Engine.ComponentSystem.Components.Component Initialize(
            Engine.ComponentSystem.Components.Component other)
        {
            base.Initialize(other);

            var otherShield = (Shield) other;
            Factory = otherShield.Factory;
            Structure = otherShield.Structure;

            return this;
        }

        /// <summary>Initializes the shield with the specified factory.</summary>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        public Shield Initialize(ShieldFactory factory)
        {
            Factory = factory;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Factory = null;
            Structure = null;
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

            Factory = (ShieldFactory) FactoryLibrary.GetFactory(packet.ReadString());
            Structure = null;
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