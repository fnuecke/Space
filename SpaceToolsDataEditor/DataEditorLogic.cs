using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditor
    {
        /// <summary>
        /// Initializes the factory type dictionary (creates list for each known factory type).
        /// </summary>
        private void InitializeLogic()
        {
            tvData.Sorted = true;

            // Rescan for issues when a property changes.
            pgProperties.PropertyValueChanged += HandlePropertyValueChanged;

            FactoryManager.FactoryAdded += HandleFactoryAdded;
            FactoryManager.FactoryRemoved += HandleFactoryRemoved;
            FactoryManager.FactoriesCleared += HandleFactoriesCleared;
            FactoryManager.FactoryNameChanged += HandleFactoryNameChanged;
        }

        private void HandleFactoryAdded(IFactory factory)
        {
            var insertionNode = tvData.Nodes.Find(factory.GetType().Name, true);
            if (insertionNode.Length > 0)
            {
                insertionNode[0].Nodes.Add(factory.Name, factory.Name);
            }

            // Validate that factory.
            ScanForIssues(factory);
        }

        private void HandleFactoryRemoved(IFactory factory)
        {
            var node = tvData.Nodes.Find(factory.Name, true);
            if (node.Length > 0)
            {
                node[0].Remove();
            }

            // See if this causes us any trouble.
            ScanForIssues();
        }

        private void HandleFactoriesCleared()
        {
            tvData.BeginUpdate();
            tvData.Nodes.Clear();
            // Create base type nodes in tree.
            foreach (var type in FactoryManager.GetFactoryTypes())
            {
                if (type.BaseType != null && type.BaseType != typeof(object))
                {
                    if (!tvData.Nodes.ContainsKey(type.BaseType.Name))
                    {
                        tvData.Nodes.Add(type.BaseType.Name, CleanFactoryName(type.BaseType));
                    }
                    tvData.Nodes[type.BaseType.Name].Nodes.Add(type.Name, CleanFactoryName(type));
                }
                else
                {
                    tvData.Nodes.Add(type.Name, CleanFactoryName(type));
                }
            }
            tvData.EndUpdate();

            // No factories means less issues! Rescan anyway, because some settings might be bad.
            ScanForIssues();
        }

        private void HandleFactoryNameChanged(string oldName, string newName)
        {
            var oldNode = tvData.Nodes.Find(oldName, true);
            if (oldNode.Length > 0)
            {
                oldNode[0].Parent.Nodes.Add(newName, newName);
                oldNode[0].Remove();
            }
        }

        private void HandlePropertyValueChanged(object o, PropertyValueChangedEventArgs args)
        {
            if (pgProperties.SelectedObject is IFactory)
            {
                var factory = (IFactory)pgProperties.SelectedObject;
                // See if what we changed is the name of the factory.
                if (ReferenceEquals(args.ChangedItem.PropertyDescriptor, TypeDescriptor.GetProperties(factory)["Name"]))
                {
                    // Yes, get old and ned value.
                    var oldName = args.OldValue as string;
                    var newName = args.ChangedItem.Value as string;
                    // Adjust factory manager layout, this will throw as necessary.
                    tvData.BeginUpdate();
                    try
                    {
                        FactoryManager.Rename(oldName, newName);
                        tvData.EndUpdate();

                        SelectFactory(newName);
                        SelectProperty("Name");

                        // Do a full scan as this factory may have been referenced somewhere.
                        ScanForIssues();
                    }
                    catch (ArgumentException ex)
                    {
                        tvData.EndUpdate();

                        // Failed renaming, revert to old name.
                        factory.Name = oldName;

                        // Tell the user why.
                        System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Rescan for issues related to this property.
                    if (args.ChangedItem.PropertyDescriptor == null || args.ChangedItem.PropertyDescriptor.Attributes[typeof(TriggersFullValidationAttribute)] != null)
                    {
                        ScanForIssues();
                    }
                    else
                    {
                        ScanForIssues(factory);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans the name of the factory type by stripping the 'Factory' postfix
        /// if it exists.
        /// </summary>
        /// <param name="type">The type of the factory.</param>
        /// <returns>The cleaned factory name.</returns>
        private static string CleanFactoryName(Type type)
        {
            return type.Name.EndsWith("Factory", StringComparison.InvariantCulture)
                       ? type.Name.Substring(0, type.Name.Length - "Factory".Length)
                       : type.Name;
        }

        /// <summary>
        /// Loads all factories found at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        private void LoadFactories(string path)
        {
            tvData.BeginUpdate();
            FactoryManager.Load(path);
            tvData.EndUpdate();

            ScanForIssues();
        }

        /// <summary>
        /// Selects the factory in our property grid if it exists, else selects
        /// nothing (clears property grid).
        /// </summary>
        /// <param name="name">The name of the factory.</param>
        private bool SelectFactory(string name)
        {
            pgProperties.SelectedObject = null;
            //tvData.SelectedNode = null;
            var factory = FactoryManager.GetFactory(name);
            if (factory != null)
            {
                pgProperties.SelectedObject = factory;
                var node = tvData.Nodes.Find(name, true);
                if (node.Length > 0)
                {
                    tvData.SelectedNode = node[0];
                }
                return true;
            }
            return false;
        }
    }
}
