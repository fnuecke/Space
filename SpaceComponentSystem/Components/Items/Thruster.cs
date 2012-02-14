using Engine.ComponentSystem.RPG.Components;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Represents a single thruster item, which is responsible for providing
    /// a base speed for a certain energy drained.
    /// </summary>
    public sealed class Thruster : Item<AttributeType>
    {
        public Thruster()
        {
          
        }
        public Thruster(string name)
        {
            _name = name;
        }
        public override string Texture()
        {
            if (_itemTexture == null)
                _itemTexture = "Textures/Icons/Buffs/stabilisator";
            return _itemTexture;
        }
    }
}
