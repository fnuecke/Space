using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Components;

namespace Space.ComponentSystem.Components
{
    public sealed class Shield : Item
    {
        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Shield)base.DeepCopy(into);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
            }

            return copy;
        }

        #endregion
    }
}
