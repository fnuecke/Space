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
        [PacketizeIgnore]
        public ShieldFactory Factory;

        /// <summary>The texture to use as a structure for the shield.</summary>
        [PacketizeIgnore]
        public Texture2D Structure;

        #endregion

        #region Initialization

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

        [OnPacketize]
        public IWritablePacket Packetize(IWritablePacket packet)
        {
            return packet.Write(Factory.Name);
        }

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            Factory = (ShieldFactory) FactoryLibrary.GetFactory(packet.ReadString());
            Structure = null;
        }

        [OnStringify]
        public StreamWriter Dump(StreamWriter w, int indent)
        {
            w.AppendIndent(indent).Write("Factory = ");
            w.Write(Factory.Name);

            return w;
        }

        #endregion
    }
}