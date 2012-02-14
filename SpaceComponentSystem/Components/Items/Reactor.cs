using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a reactor item, which is used to store and produce energy.
    /// </summary>
    public sealed class Reactor : Item<AttributeType>
    {
        public Reactor()
        {
            
        }
        public Reactor(string name)
        {
            _name = name;
        }
    }
}
