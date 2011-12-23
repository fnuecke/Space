using Engine.Data;

namespace Engine.ComponentSystem.Components.Messages
{
    /// <summary>
    /// Sent by <c>EntityModules</c> when a module was removed.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute types.</typeparam>
    public struct ModuleRemoved<TAttribute>
        where TAttribute : struct
    {
        /// <summary>
        /// The module that was removed.
        /// </summary>
        public AbstractEntityModule<TAttribute> Module;

        internal static ModuleRemoved<TAttribute> Create(AbstractEntityModule<TAttribute> module)
        {
            ModuleRemoved<TAttribute> result;
            result.Module = module;
            return result;
        }
    }
}
