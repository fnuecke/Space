using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditor
    {
        /// <summary>
        /// The flat list of all factories, referenced via their name.
        /// </summary>
        private readonly Dictionary<string, IFactory> _factories = new Dictionary<string, IFactory>();

        /// <summary>
        /// Lists of all factories, categorized by their type.
        /// </summary>
        private readonly Dictionary<Type, List<IFactory>> _factoriesByType = new Dictionary<Type, List<IFactory>>();

        /// <summary>
        /// Initializes the factory type dictionary (creates list for each known factory type).
        /// </summary>
        private void InitializeLogic()
        {
            tvData.Sorted = true;
            tvData.BeginUpdate();
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(typeof(IFactory).IsAssignableFrom)
                .Where(p => p.IsClass && !p.IsAbstract))
            {
                // Add list for this type.
                _factoriesByType.Add(type, new List<IFactory>());

                // Create node in tree.
                tvData.Nodes.Add(type.Name, CleanFactoryName(type));
            }
            tvData.EndUpdate();
        }

        private void LoadFactories(string path)
        {
            ClearFactories();
            foreach (var file in Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories))
            {
                using (var reader = new XmlTextReader(file))
                {
                    tvData.BeginUpdate();
                    try
                    {
                        var factories = IntermediateSerializer.Deserialize<IFactory[]>(reader, null);
                        foreach (var factory in factories)
                        {
                            AddFactory(factory);
                        }
                    }
                    catch (InvalidContentException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                    finally
                    {
                        tvData.EndUpdate();
                    }
                }
            }
        }

        private void AddFactory(IFactory factory)
        {
            _factories.Add(factory.Name, factory);
            var type = factory.GetType();
            _factoriesByType[type].Add(factory);
            tvData.Nodes[type.Name].Nodes.Add(factory.Name, factory.Name);
        }

        private void ClearFactories()
        {
            tvData.BeginUpdate();
            _factories.Clear();
            foreach (var type in _factoriesByType)
            {
                type.Value.Clear();
                tvData.Nodes[type.Key.Name].Nodes.Clear();
            }
            tvData.EndUpdate();
        }

        private static string CleanFactoryName(Type type)
        {
            var name = type.Name;
            if (name.EndsWith("Factory", StringComparison.InvariantCulture))
            {
                name = name.Substring(0, name.Length - "Factory".Length);
            }
            return name;
        }

        private void SelectFactory(string name)
        {
            if (_factories.ContainsKey(name))
            {
                pgProperties.SelectedObject = _factories[name];
            }
            else
            {
                pgProperties.SelectedObject = null;
            }
        }
    }
}
