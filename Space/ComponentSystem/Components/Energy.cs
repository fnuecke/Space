using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Components.Messages;
using Space.Data;

namespace Space.ComponentSystem.Components
{
    public sealed class Energy : AbstractRegeneratingValue
    {
        public override void HandleMessage(System.ValueType message)
        {
            if (message is ModuleAdded<EntityAttributeType> || message is ModuleRemoved<EntityAttributeType>)
            {
                var modules = Entity.GetComponent<EntityModules<EntityAttributeType>>();
                MaxValue = modules.GetValue(EntityAttributeType.Energy);
                if (Value > MaxValue)
                {
                    Value = MaxValue;
                }
                Regeneration = modules.GetValue(EntityAttributeType.EnergyRegeneration);
            }
        }
    }
}
