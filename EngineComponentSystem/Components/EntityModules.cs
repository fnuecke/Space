using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.Data;
using Engine.Math;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Manages a list of modules currently active on the related entity.
    /// 
    /// <para>
    /// <b>Important</b>: the type parameter <em>must</em> be an <c>enum</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TAttribute">The enum that holds the possible types of
    /// attributes.</typeparam>
    public sealed class EntityModules<TModule, TAttribute> : AbstractComponent
        where TModule : AbstractEntityModule<TAttribute>, new()
        where TAttribute : struct
    {
        #region Properties

        /// <summary>
        /// A list of all known attributes.
        /// </summary>
        public ReadOnlyCollection<TModule> Modules { get { return _modules.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// Actual list of attributes registered.
        /// </summary>
        private List<TModule> _modules = new List<TModule>();

        /// <summary>
        /// Cached computation results for accumulative attribute values.
        /// </summary>
        private Dictionary<TAttribute, Fixed> _cached = new Dictionary<TAttribute, Fixed>();

        #endregion

        #region Attributes
        
        /// <summary>
        /// Get the accumulative value of all attributes in this component.
        /// 
        /// <para>
        /// This will result in the same value as calling <c>EntityAttribute.Accumulate</c>,
        /// but will cache the result, so repetitive calls will be faster.
        /// </para>
        /// </summary>
        /// <param name="attributeType">The type for which to compute the
        /// overall value.</param>
        /// <returns>The accumulative value of the specified attribute type
        /// over all attributes tracked by this component.</returns>
        public Fixed GetValue(TAttribute attributeType)
        {
            if (_cached.ContainsKey(attributeType))
            {
                return _cached[attributeType];
            }
            var attributes = new List<EntityAttribute<TAttribute>>();
            foreach (var module in _modules)
            {
                attributes.AddRange(module.Attributes);
            }
            var result = attributes.Accumulate(attributeType);
            _cached[attributeType] = result;
            return result;
        }

        /// <summary>
        /// Registers a new module with this component.
        /// 
        /// <para>
        /// Note that it is not a good idea to change any attributes type while
        /// it is tracked by this component, as this will break validated
        /// computed accumulative values. In the odd case this is required,
        /// remove the module first, then add it again.
        /// </para>
        /// </summary>
        /// <param name="module">The module to add.</param>
        public void AddModule(TModule module)
        {
            _modules.Add(module);
            // Invalidate cache.
            foreach (var attribute in module.Attributes)
            {
                _cached.Remove(attribute.Type);
            }
        }

        /// <summary>
        /// Registers a list of new modules with this component.
        /// 
        /// <para>
        /// Note that it is not a good idea to change any attributes type while
        /// it is tracked by this component, as this will break validated
        /// computed accumulative values. In the odd case this is required,
        /// remove the module first, then add it again.
        /// </para>
        /// </summary>
        /// <param name="modules">The modules to add.</param>
        public void AddModules(IEnumerable<TModule> modules)
        {
            _modules.AddRange(modules);
            // Invalidate cache.
            foreach (var module in modules)
            foreach (var attribute in module.Attributes)
            {
                _cached.Remove(attribute.Type);
            }
        }

        /// <summary>
        /// Removes a module from this component.
        /// </summary>
        /// <param name="module">The module to remove.</param>
        public void RemoveModule(TModule module)
        {
            if (_modules.Remove(module))
            {
                // Invalidate cache.
                foreach (var attribute in module.Attributes)
                {
                    _cached.Remove(attribute.Type);
                }
            }
        }

        /// <summary>
        /// Removes a module by its index from this component.
        /// </summary>
        /// <param name="index">The index of the module to remove.</param>
        /// <returns>The removed module.</returns>
        public TModule RemoveModuleAt(int index)
        {
            var module = _modules[index];
            RemoveModule(module);
            return module;
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            base.Packetize(packet)
                .Write(_modules.Count);
            foreach (var module in _modules)
            {
                packet.Write(module);
            }
            return packet;
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _modules.Clear();
            int numModules = packet.ReadInt32();
            for (int i = 0; i < numModules; i++)
            {
                _modules.Add(packet.ReadPacketizable(new TModule()));
            }

            // Invalidate caches.
            _cached.Clear();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            
            foreach (var module in _modules)
            {
                module.Hash(hasher);
            }
        }

        public override object Clone()
        {
            var copy = (EntityModules<TModule, TAttribute>)base.Clone();

            // Create a new list and copy all modules.
            copy._modules = new List<TModule>();
            foreach (var module in _modules)
            {
                copy._modules.Add((TModule)module.Clone());
            }

            // Copy the cache as well.
            copy._cached = new Dictionary<TAttribute, Fixed>(_cached);

            return copy;
        }

        #endregion
    }
}
