using Engine.Data;
using Engine.Serialization;

namespace Engine.ComponentSystem.Components
{
    public static class PacketizerRegistration
    {
        public static void Initialize<TModule, TAttribute>()
            where TModule : AbstractEntityModule<TAttribute>, new()
            where TAttribute : struct
        {
            Packetizer.Register<Acceleration>();
            Packetizer.Register<Avatar>();
            Packetizer.Register<EntityModules<TModule, TAttribute>>();
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
