using System.Linq;
using Engine.ComponentSystem;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using NSubstitute;

namespace Engine.Tests.ComponentSystem
{
    using NUnit.Framework;

    [TestFixture]
    public class ManagerTest
    {
        [Test]
        public void AddRemoveSystem()
        {
            var manager = new Manager();
            var system = Substitute.For<AbstractSystem>();

            manager.AddSystem(system);

            manager.RemoveSystem(system);
        }

        [Test]
        public void UpdateAndDraw()
        {
            var manager = new Manager();
            var system = Substitute.For<AbstractSystem>();

            manager.AddSystem(system);

            manager.Update(0);
            
            //system.Received().Update(Arg.Any<long>());

            manager.Draw(0, 0);

            //system.Received().Draw(Arg.Any<long>());
        }

        [Test]
        public void AddGetRemoveEntity()
        {
            var manager = new Manager();

            var entity1 = manager.AddEntity();
            var entity2 = manager.AddEntity();

            Assert.True(manager.HasEntity(entity1));
            Assert.True(manager.HasEntity(entity2));

            manager.RemoveEntity(entity1);

            Assert.False(manager.HasEntity(entity1));

            manager.RemoveEntity(entity2);

            Assert.False(manager.HasEntity(entity2));
        }

        [Test]
        public void AddGetRemoveComponent()
        {
            var manager = new Manager();

            var entity = manager.AddEntity();

            var component1 = manager.AddComponent<TestComponent>(entity);
            var component2 = manager.AddComponent<TestComponent>(entity);

            var component1Id = component1.Id;
            var component2Id = component2.Id;

            Assert.True(manager.HasComponent(component1Id));
            Assert.True(manager.HasComponent(component2Id));

            Assert.AreEqual(component1, manager.GetComponentById(component1Id));
            Assert.AreEqual(component2, manager.GetComponentById(component2Id));

            Assert.AreEqual(component1, manager.GetComponent(entity, TestComponent.TypeId));
            
            Assert.Contains(component1, manager.GetComponents(entity, TestComponent.TypeId).ToArray());
            Assert.Contains(component2, manager.GetComponents(entity, TestComponent.TypeId).ToArray());

            manager.RemoveComponent(component1);

            Assert.False(manager.HasComponent(component1Id));

            Assert.AreEqual(component2, manager.GetComponent(entity, TestComponent.TypeId));

            manager.RemoveEntity(entity);

            Assert.False(manager.HasComponent(component2Id));
        }

        [Test]
        public void SendMessage()
        {
            var manager = new Manager();
            var system = Substitute.For<AbstractSystem>();
            manager.AddSystem(system);

            var message = 0;
            manager.SendMessage(message);

            //system.ReceivedWithAnyArgs().Receive(ref message);
        }

        [Test]
        public void Serialization()
        {
            var manager1 = new Manager();
            var manager2 = new Manager();

            var entity = manager1.AddEntity();
            var component = manager1.AddComponent<TestComponent>(entity);

            var packet = new Packet();
            manager1.Packetize(packet);

            packet.Reset();

            packet.ReadPacketizableInto(manager2);

            Assert.True(manager2.HasEntity(entity));
            Assert.True(manager2.HasComponent(component.Id));
            Assert.AreEqual(manager2.GetComponentById(component.Id).Entity, entity);
        }

        private sealed class TestComponent : Component
        {
            public static readonly int TypeId = Engine.ComponentSystem.Manager.GetComponentTypeId(typeof(TestComponent));
            public override int GetTypeId()
            {
                return TypeId;
            }
        }
    }
}
