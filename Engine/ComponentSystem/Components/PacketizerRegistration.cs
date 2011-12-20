using Engine.Serialization;

namespace Engine.ComponentSystem.Components
{
    public static class PacketizerRegistration
    {
        public static void RegisterEngineComponentsWithPacketizer()
        {
            Packetizer.Register<Acceleration>();
            Packetizer.Register<Avatar>();
            Packetizer.Register<CollidableBox>();
            Packetizer.Register<CollidableSphere>();
            Packetizer.Register<Friction>();
            Packetizer.Register<Physics>();
            Packetizer.Register<Sound>();
            Packetizer.Register<Spin>();
            Packetizer.Register<Transform>();
            Packetizer.Register<TransformedRenderer>();
            Packetizer.Register<Velocity>();
        }
    }
}
