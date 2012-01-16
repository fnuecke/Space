using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Engine.ComponentSystem.Components.Messages;
using Engine.Data;
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
        private Dictionary<TAttribute, float> _attributeCache = new Dictionary<TAttribute, float>();

        /// <summary>
        /// Cached lists of components by type.
        /// </summary>
        private Dictionary<Type, List<AbstractEntityModule<TAttribute>>> _moduleCache = new Dictionary<Type, List<AbstractEntityModule<TAttribute>>>();

        /// <summary>
        /// Manager for component ids.
        /// </summary>
        private IdManager _idManager = new IdManager();

        #endregion

        #region Constructor

        public EntityModules()
        {
        }

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
        public float GetValue(TAttribute attributeType)
        {
            if (_attributeCache.ContainsKey(attributeType))
            {
                return _attributeCache[attributeType];
            }
            var result = GetValue(attributeType, 0);
            _attributeCache[attributeType] = result;
            return result;
        }

        /// <summary>
        /// Get the accumulative value of all attributes in this component.
        /// 
        /// <para>
        /// This will <em>not</em> cache the result, as the result depends
        /// on the given base value.
        /// </para>
        /// </summary>
        /// <param name="attributeType">The type for which to compute the
        /// overall value.</param>
        /// <param name="baseValue">The base value to start from.</param>
        /// <returns>The accumulative value of the specified attribute type
        /// over all attributes tracked by this component.</returns>
        public float GetValue(TAttribute attributeType, float baseValue)
        {
            var attributes = new List<ModuleAttribute<TAttribute>>();
            foreach (var module in _modules)
            {
                attributes.AddRange(module.Attributes);
            }
            return attributes.Accumulate(attributeType, baseValue);
        }

        /// <summary>
        /// Get a list of all modules of the given type registered with this
        /// component.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetModules<T>()
            where T : AbstractEntityModule<TAttribute>
        {
            Type type = typeof(T);
            if (_moduleCache.ContainsKey(type))
            {
                return _moduleCache[type].Cast<T>();
            }
            var modules = new List<AbstractEntityModule<TAttribute>>();
            foreach (var module in _modules)
            {
                if (module.GetType() == type)
                {
                    modules.Add(module);
                }
            }
            _moduleCache[type] = modules;
            return modules.Cast<T>();
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
            foreach (var attributeType in module.AttributesToInvalidate)
            {
                _attributeCache.Remove(attributeType);
            }
            if (Entity != null)
            {
                ModuleAdded<TAttribute> message;
                message.Module = module;
                Entity.SendMessage(ref message);
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
                    ModuleRemoved<TAttribute> message;
                    message.Module = module;
                    Entity.SendMessage(ref message);
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

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .WriteWithTypeInfo(_modules)
                .Write(_idManager);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
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

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            
            foreach (var module in _modules)
            {
                module.Hash(hasher);
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (EntityModules<TAttribute>)base.DeepCopy(into);

            if (copy == into)
            {
                if (copy._modules.Count > _modules.Count)
                {
                    copy._modules.RemoveRange(_modules.Count, copy._modules.Count - _modules.Count);
                }

                int i = 0;
                for (; i < copy._modules.Count; ++i)
                {
                    copy._modules[i] = _modules[i].DeepCopy(copy._modules[i]);
                }
                for (; i < _modules.Count; ++i)
                {
                    copy._modules.Add(_modules[i].DeepCopy());
                }

                copy._attributeCache.Clear();
                copy._moduleCache.Clear();

                copy._idManager = _idManager.DeepCopy(copy._idManager);
            }
            else
            {
                // Create a new list and copy all modules.
                copy._modules = new List<AbstractEntityModule<TAttribute>>();
                foreach (var module in _modules)
                {
                    copy._modules.Add(module.DeepCopy());
                }

                // Copy the caches as well.
                copy._attributeCache = new Dictionary<TAttribute, float>(_attributeCache);
                copy._moduleCache = new Dictionary<Type, List<AbstractEntityModule<TAttribute>>>();

                // And the id manager.
                copy._idManager = _idManager.DeepCopy();
            }

            return copy;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Count = " + Modules.Count.ToString();
        }

        #endregion
    }
}
