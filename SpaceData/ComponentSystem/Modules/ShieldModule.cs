using Engine.ComponentSystem.Modules;
using Space.Data;

namespace Space.ComponentSystem.Modules
{
    public class ShieldModule : AbstractModule<SpaceModifier>
    {
        #region Copying

        public override AbstractModule<SpaceModifier> DeepCopy(AbstractModule<SpaceModifier> into)
        {
            var copy = (ShieldModule)base.DeepCopy(into);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
            }

            return copy;
        }

        #endregion
    }
}
