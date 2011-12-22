using Engine.Data;

namespace Space.Data
{
    public class ShipModule : AbstractEntityModule<EntityAttributeType>
    {
        public ShipModuleType Type { get; set; }
    }
}
