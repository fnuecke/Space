using Engine.Serialization;

namespace Engine.ComponentSystem
{
    public static class PacketizerRegistration
    {
        public static void RegisterEngineComponentsWithPacketizer()
        {
            Packetizer.Register<Entities.Entity>();

            Packetizer.Register<Components.Acceleration>();
            Packetizer.Register<Components.Avatar>();
            Packetizer.Register<Components.CollidableBox>();
            Packetizer.Register<Components.CollidableSphere>();
            Packetizer.Register<Components.Friction>();
            Packetizer.Register<Components.Physics>();
            Packetizer.Register<Components.Sound>();
            Packetizer.Register<Components.Spin>();
            Packetizer.Register<Components.Transform>();
            Packetizer.Register<Components.TransformedRenderer>();
            Packetizer.Register<Components.Velocity>();
        }
    }
}
