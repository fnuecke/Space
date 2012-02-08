using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a sensor item, which is used to detect stuff.
    /// </summary>
    public sealed class Sensor : Item<AttributeType>
    {
        public Sensor()
        {
            
        }
        public Sensor(string name)
        {
            _name = name;
        }
    }
}
