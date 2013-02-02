using System;
using System.Collections.Generic;
using System.Linq;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Physics;
using Engine.ComponentSystem.Physics.Components;
using Engine.ComponentSystem.RPG.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using Space.Data;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// This system's sole purpose is to update a ship's physics representation (in Box2D terms: its fixture) when its equipment changes.
    /// </summary>
    public class EquipmentToFixtureSystem : AbstractSystem, IUpdatingSystem
    {
        /// <summary>List of entities for which the equipment changed since the last update.</summary>
        [CopyIgnore, PacketizeIgnore]
        private HashSet<int> _changedShape = new HashSet<int>();
        
        /// <summary>List of entities for which the mass changed since the last update, meaning we have to recompute our fixture's density.</summary>
        [CopyIgnore, PacketizeIgnore]
        private HashSet<int> _changedMass = new HashSet<int>(); 

        public void Update(long frame)
        {
            var shipShapeSystem = (ShipShapeSystem) Manager.GetSystem(ShipShapeSystem.TypeId);
            foreach (var entity in _changedShape)
            {
                var body = Manager.GetComponent(entity, Body.TypeId) as Body;
                if (body == null)
                {
                    continue;
                }

                var polygons = shipShapeSystem.GetShapes(entity);
                if (polygons == null)
                {
                    continue;
                }

                uint collisionCategory = 0, collisionMask = 0;
                foreach (Fixture fixture in Manager.GetComponents(entity, Fixture.TypeId).ToList())
                {
                    collisionCategory |= fixture.CollisionCategory;
                    collisionMask |= fixture.CollisionMask;

                    Manager.RemoveComponent(fixture);
                }

                // In case the shield is up we have to remove its group.
                collisionCategory &= ~Factions.Shields.ToCollisionGroup();

                foreach (var polygon in polygons)
                {
                    Manager.AttachPolygon(body, polygon, collisionCategory: collisionCategory, collisionMask: collisionMask);
                }

                // Enqueue for density recomputation.
                _changedMass.Add(entity);
            }
            _changedShape.Clear();

            foreach (var entity in _changedMass)
            {
                var body = Manager.GetComponent(entity, Body.TypeId) as Body;
                if (body == null || body.Type != Body.BodyType.Dynamic)
                {
                    continue;
                }

                var attributes = (Attributes<AttributeType>) Manager.GetComponent(entity, Attributes<AttributeType>.TypeId);
                if (attributes == null)
                {
                    continue;
                }

                var mass = Math.Max(1, attributes.GetValue(AttributeType.Mass));

                // Reset density to one and see what mass we'd get.
                foreach (Fixture fixture in Manager.GetComponents(entity, Fixture.TypeId))
                {
                    fixture.Density = 1f;
                }
                body.ResetMassData();

                var factor = mass / body.Mass;
                foreach (Fixture fixture in Manager.GetComponents(entity, Fixture.TypeId))
                {
                    fixture.Density *= factor;
                }
                body.ResetMassData();

                System.Diagnostics.Debug.Assert(Math.Abs(body.Mass - mass) < 0.1f);
            }
            _changedMass.Clear();
        }

        [MessageCallback]
        public void OnEntityRemoved(EntityRemoved message)
        {
            _changedShape.Remove(message.Entity);
            _changedMass.Remove(message.Entity);
        }

        [MessageCallback]
        public void OnItemEquipped(ItemEquipped message)
        {
            _changedShape.Add(message.Slot.Root.Entity);
        }

        [MessageCallback]
        public void OnItemUnequipped(ItemUnequipped message)
        {
            _changedShape.Add(message.Slot.Root.Entity);
        }
        
        [MessageCallback]
        public void OnCharacterStatsInvalidated(CharacterStatsInvalidated message)
        {
            _changedMass.Add(message.Entity);
        }

        public override AbstractSystem NewInstance()
        {
            var copy = (EquipmentToFixtureSystem) base.NewInstance();

            copy._changedShape = new HashSet<int>();
            copy._changedMass = new HashSet<int>();

            return copy;
        }

        public override void CopyInto(AbstractSystem into)
        {
            base.CopyInto(into);

            var copy = (EquipmentToFixtureSystem) into;
            copy._changedShape.Clear();
            copy._changedShape.UnionWith(_changedShape);
            copy._changedMass.Clear();
            copy._changedMass.UnionWith(_changedMass);
        }

        public override IWritablePacket Packetize(IWritablePacket packet)
        {
            base.Packetize(packet);

            packet.Write(_changedShape.Count);
            foreach (var entity in _changedShape)
            {
                packet.Write(entity);
            }
            
            packet.Write(_changedMass.Count);
            foreach (var entity in _changedMass)
            {
                packet.Write(entity);
            }

            return packet;
        }

        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);
            
            _changedShape.Clear();
            var changedShapeCount = packet.ReadInt32();
            for (var i = 0; i < changedShapeCount; i++)
            {
                _changedShape.Add(packet.ReadInt32());
            }

            _changedMass.Clear();
            var changedMassCount = packet.ReadInt32();
            for (var i = 0; i < changedMassCount; i++)
            {
                _changedMass.Add(packet.ReadInt32());
            }
        }
    }
}
