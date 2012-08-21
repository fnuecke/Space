using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
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

        /// <summary>
        /// Loads all factories found at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
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

            ScanForIssues();
        }

        /// <summary>
        /// Adds a single factory to the list of known factories.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void AddFactory(IFactory factory)
        {
            _factories.Add(factory.Name, factory);
            var type = factory.GetType();
            _factoriesByType[type].Add(factory);
            tvData.Nodes[type.Name].Nodes.Add(factory.Name, factory.Name);
        }

        /// <summary>
        /// Clears the lists with known factories as well as the list with
        /// factories in the GUI.
        /// </summary>
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

            // No factories means less issues!
            ClearIssues();

            // Rescan anyway, because some settings might be bad.
            ScanForIssues();
        }

        /// <summary>
        /// Cleans the name of the factory type by stripping the 'Factory' postfix
        /// if it exists.
        /// </summary>
        /// <param name="type">The type of the factory.</param>
        /// <returns>The cleaned factory name.</returns>
        private static string CleanFactoryName(Type type)
        {
            var name = type.Name;
            if (name.EndsWith("Factory", StringComparison.InvariantCulture))
            {
                name = name.Substring(0, name.Length - "Factory".Length);
            }
            return name;
        }

        /// <summary>
        /// Selects the factory in our property grid if it exists, else selects
        /// nothing (clears property grid).
        /// </summary>
        /// <param name="name">The name of the factory.</param>
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

        /// <summary>
        /// Scans for issues.
        /// </summary>
        public void ScanForIssues()
        {
            // Clear old list.
            ClearIssues();

            // Check settings.
            foreach (string project in DataEditorSettingsProxy.Default.ContentProjects)
            {
                if (!File.Exists(project))
                {
                    AddIssue("Path to content project is invalid: " + project);
                }
            }

            // Check factories.
            foreach (var factory in _factories.Values)
            {
                // Check image asset properties.
                foreach (PropertyDescriptor property in TypeDescriptor
                    .GetProperties(factory,
                                   new Attribute[]
                                   {
                                       new EditorAttribute(
                                       typeof(TextureAssetEditor),
                                       typeof(UITypeEditor))
                                   }))
                {
                    if (property.PropertyType != typeof(string))
                    {
                        AddIssue("Property marked as texture asset is not of type string.", factory.Name, property.Name);
                    }
                    else
                    {
                        var path = ((string)property.GetValue(factory)).Replace('/', '\\');
                        if (!ContentProjectManager.HasTextureAsset(path))
                        {
                            AddIssue("Invalid texture asset name, no such texture asset.", factory.Name, property.Name, IssueType.Error);
                        }
                    }
                }
            }
        }
    }
}
