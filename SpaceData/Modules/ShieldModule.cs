using Engine.Data;

namespace Space.Data.Modules
{
    public class ShieldModule : AbstractEntityModule<EntityAttributeType>
    {
        #region Copying

        public override AbstractEntityModule<EntityAttributeType> DeepCopy(AbstractEntityModule<EntityAttributeType> into)
        {
            var copy = (ShieldModule)base.DeepCopy(into is ShieldModule ? into : null);

            if (copy == into)
            {
                // Copied into other instance, copy fields.
            }

            return copy;
        }

        #endregion
    }
}
