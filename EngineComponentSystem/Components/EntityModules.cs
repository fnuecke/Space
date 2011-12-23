using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Engine.ComponentSystem.Components.Messages;
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
    public sealed class EntityModules<TAttribute> : AbstractComponent
        where TAttribute : struct
    {
        #region Properties

        /// <summary>
        /// A list of all known attributes.
        /// </summary>
        public ReadOnlyCollection<AbstractEntityModule<TAttribute>> Modules { get { return _modules.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// Actual list of attributes registered.
        /// </summary>
        private List<AbstractEntityModule<TAttribute>> _modules = new List<AbstractEntityModule<TAttribute>>();

        /// <summary>
        /// Cached computation results for accumulative attribute values.
        /// </summary>
        private Dictionary<TAttribute, Fixed> _attributeCache = new Dictionary<TAttribute, Fixed>();

        /// <summary>
        /// Cached lists of components by type.
        /// </summary>
        private Dictionary<Type, object[]> _moduleCache = new Dictionary<Type, object[]>();

        /// <summary>
        /// Manager for component ids.
        /// </summary>
        private IdManager _idManager = new IdManager();

        #endregion

        #region Attributes / Modules
        
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
            if (_attributeCache.ContainsKey(attributeType))
            {
                return _attributeCache[attributeType];
            }
            var attributes = new List<ModuleAttribute<TAttribute>>();
            foreach (var module in _modules)
            {
                attributes.AddRange(module.Attributes);
            }
            var result = attributes.Accumulate(attributeType);
            _attributeCache[attributeType] = result;
            return result;
        }

        /// <summary>
        /// Get a list of all modules of the given type registered with this
        /// component.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <returns></returns>
        public T[] GetModules<T>()
            where T : AbstractEntityModule<TAttribute>
        {
            Type type = typeof(T);
            if (_moduleCache.ContainsKey(type))
            {
                return (T[])_moduleCache[type];
            }
            var modules = new List<AbstractEntityModule<TAttribute>>();
            foreach (var module in _modules)
            {
                if (module.GetType() == type)
                {
                    modules.Add(module);
                }
            }
            _moduleCache[type] = modules.Cast<T>().ToArray();
            return (T[])_moduleCache[type];
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
        public void AddModule(AbstractEntityModule<TAttribute> module)
        {
            if (module == null || _modules.Contains(module))
            {
                return;
            }
            if (module.UID > 0)
            {
                throw new ArgumentException("Module is already part of another component.", "module");
            }
            _modules.Add(module);
            module.UID = _idManager.GetId();
            // Invalidate caches.
            _moduleCache.Remove(module.GetType());
            foreach (var attribute in module.Attributes)
            {
                _attributeCache.Remove(attribute.Type);
            }
            if (Entity != null)
            {
                Entity.SendMessage(ModuleAdded<TAttribute>.Create(module));
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
        public void AddModules(IEnumerable<AbstractEntityModule<TAttribute>> modules)
        {
            foreach (var module in modules)
            {
                AddModule(module);
            }
        }

        /// <summary>
        /// Removes a module from this component.
        /// </summary>
        /// <param name="module">The module to remove.</param>
        public AbstractEntityModule<TAttribute> RemoveModule(AbstractEntityModule<TAttribute> module)
        {
            if (_modules.Remove(module))
            {
                // Invalidate caches.
                _moduleCache.Remove(module.GetType());
                foreach (var attribute in module.Attributes)
                {
                    _attributeCache.Remove(attribute.Type);
                }
                // Notify others *before* resetting the id.
                if (Entity != null)
                {
                    Entity.SendMessage(ModuleRemoved<TAttribute>.Create(module));
                }
                _idManager.ReleaseId(module.UID);
                module.UID = -1;
            }
            return module;
        }

        /// <summary>
        /// Removes a module by its index from this component.
        /// </summary>
        /// <param name="index">The index of the module to remove.</param>
        /// <returns>The removed module.</returns>
        public AbstractEntityModule<TAttribute> RemoveModuleAt(int index)
        {
            return RemoveModule(_modules[index]);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .WriteWithTypeInfo(_modules)
                .Write(_idManager);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _modules.Clear();
            foreach (var module in packet.ReadPacketizablesWithTypeInfo<AbstractEntityModule<TAttribute>>())
            {
                _modules.Add(module);
            }

            packet.ReadPacketizableInto(_idManager);

            // Invalidate caches.
            _attributeCache.Clear();
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
            var copy = (EntityModules<TAttribute>)base.Clone();

            // Create a new list and copy all modules.
            copy._modules = new List<AbstractEntityModule<TAttribute>>();
            foreach (var module in _modules)
            {
                copy._modules.Add((AbstractEntityModule<TAttribute>)module.Clone());
            }

            // Copy the caches as well.
            copy._attributeCache = new Dictionary<TAttribute, Fixed>(_attributeCache);
            copy._moduleCache = new Dictionary<Type, object[]>();

            // And the id manager.
            copy._idManager = (IdManager)_idManager.Clone();

            return copy;
        }

        #endregion
    }
}
