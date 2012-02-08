using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a shield item, which blocks damage.
    /// </summary>
    public sealed class Shield : Item<AttributeType>
    {
        public Shield()
        {
           
        }
        public Shield(string name)
        {
            _name = name;
        }
    }
}
