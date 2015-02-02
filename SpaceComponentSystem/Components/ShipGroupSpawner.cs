using System.IO;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Space.ComponentSystem.Systems;

namespace Space.ComponentSystem.Components
{
    public sealed class ShipGroupSpawner : Component
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

        /// <summary>The list of ships this group is made up of.</summary>
        private string[] ships;

        private ArtificialIntelligence.AIConfiguration[] configurations;

        private SquadSystem.AbstractFormation formation;

        #endregion

        #region Initialization

        public override Component Initialize(Component other)
        {
            return base.Initialize(other);
        }

        public override void Reset()
        {
            base.Reset();
        }
        
        #endregion

        #region Serialization / Hashing

        [OnPacketize]
        public IWritablePacket Packetize(IWritablePacket packet)
        {
            //packet.Write(Targets.Count);
            //foreach (var item in Targets)
            //{
            //    packet.Write(item);
            //}

            return packet;
        }

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            //Targets.Clear();
            //var targetCount = packet.ReadInt32();
            //for (var i = 0; i < targetCount; i++)
            //{
            //    Targets.Add(packet.ReadInt32());
            //}
        }

        [OnStringify]
        public StreamWriter Dump(StreamWriter w, int indent)
        {
            //w.AppendIndent(indent).Write("Targets = {");
            //var first = true;
            //foreach (var target in Targets)
            //{
            //    if (!first)
            //    {
            //        w.Write(", ");
            //    }
            //    first = false;
            //    w.Write(target);
            //}
            //w.Write("}");

            return w;
        }

        #endregion
    }
}
