using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditor
    {
        private int _isValidationEnabled;

        #region Initialization / Loading
        
        /// <summary>
        /// Initializes the factory type dictionary (creates list for each known factory type).
        /// </summary>
        private void InitializeLogic()
        {
            tvData.Sorted = true;

            FactoryManager.FactoryAdded += HandleFactoryAdded;
            FactoryManager.FactoryRemoved += HandleFactoryRemoved;
            FactoryManager.FactoryNameChanged += HandleFactoryNameChanged;
            FactoryManager.FactoriesCleared += HandleCleared;

            ItemPoolManager.ItemPoolAdded += HandleItemPoolAdded;
            ItemPoolManager.ItemPoolRemoved += HandleItemPoolRemoved;
            ItemPoolManager.ItemPoolNameChanged += HandleItemPoolNameChanged;
            ItemPoolManager.ItemPoolCleared += HandleCleared;
        }

        /// <summary>
        /// Loads all factories and so on found at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        private void LoadData(string path)
        {
            // Disable validation and tree updates while loading, because
            // there will be a lot of changes (and redundant updates otherwise).
            ++_isValidationEnabled;
            tvData.BeginUpdate();

            // Load stuff.
            FactoryManager.Load(path);
            ItemPoolManager.Load(path);

            // Reenable tree and validation.
            tvData.EndUpdate();
            --_isValidationEnabled;

            // Done loading, see if there are any problem with what we got.
            ScanForIssues();
        }

        #endregion

        #region Factory handlers

        private void HandleFactoryAdded(IFactory factory)
        {
            // Find parent node (base type) and insert in it.
            var node = tvData.Nodes.Find(factory.GetType().AssemblyQualifiedName, true).FirstOrDefault(IsFactory);
            Debug.Assert(node != null);
            node.Nodes.Add(factory.Name, factory.Name).Tag = factory;

            // Validate that factory.
            ScanForIssues(factory);
        }

        private void HandleFactoryRemoved(IFactory factory)
        {
            // Find the corresponding node and remove it.
            var node = tvData.Nodes.Find(factory.Name, true).FirstOrDefault(n => IsFactory(n.Parent));
            Debug.Assert(node != null);
            node.Remove();

            // See if this causes us any trouble.
            ScanForIssues();
        }

        private void HandleFactoryNameChanged(string oldName, string newName)
        {
            // Find old entry, add new entry to old parent and remove old one.
            var node = tvData.Nodes.Find(oldName, true).FirstOrDefault(n => IsFactory(n.Parent));
            Debug.Assert(node != null);
            node.Parent.Nodes.Add(newName, newName).Tag = node.Tag;
            node.Remove();

            // See if this causes us any trouble.
            ScanForIssues();
        }

        #endregion

        #region Item pool handlers

        private void HandleItemPoolAdded(ItemPool pool)
        {
            // Find parent node (base type) and insert in it.
            var node = tvData.Nodes.Find(pool.GetType().AssemblyQualifiedName, true).FirstOrDefault(IsItemPool);
            Debug.Assert(node != null);
            node.Nodes.Add(pool.Name, pool.Name).Tag = pool;

            // Validate that item pool.
            ScanForIssues(pool);
        }

        private void HandleItemPoolRemoved(ItemPool itemPool)
        {
            // Find the corresponding node and remove it.
            var node = tvData.Nodes.Find(itemPool.Name, true).FirstOrDefault(n => IsItemPool(n.Parent));
            Debug.Assert(node != null);
            node.Remove();

            // See if this causes us any trouble.
            ScanForIssues();
        }

        private void HandleItemPoolNameChanged(string oldName, string newName)
        {
            // Find old entry, add new entry to old parent and remove old one.
            var node = tvData.Nodes.Find(oldName, true).FirstOrDefault(n => IsItemPool(n.Parent));
            Debug.Assert(node != null);
            node.Parent.Nodes.Add(newName, newName).Tag = node.Tag;
            node.Remove();

            // See if this causes us any trouble.
            ScanForIssues();
        }

        #endregion

        #region Common handlers

        private void HandleCleared()
        {
            // Disable validation and tree updates (performance).
            ++_isValidationEnabled;
            tvData.BeginUpdate();

            // Clear the nodes.
            tvData.Nodes.Clear();

            // Create factory base type nodes in tree.
            foreach (var type in FactoryManager.GetFactoryTypes())
            {
                var baseType = type.BaseType;
                if (baseType != null && baseType != typeof(object))
                {
                    if (!tvData.Nodes.ContainsKey(baseType.AssemblyQualifiedName))
                    {
                        tvData.Nodes.Add(baseType.AssemblyQualifiedName, CleanFactoryName(baseType));
                    }
                    tvData.Nodes[baseType.AssemblyQualifiedName].Nodes.Add(type.AssemblyQualifiedName, CleanFactoryName(type));
                }
                else
                {
                    tvData.Nodes.Add(type.AssemblyQualifiedName, CleanFactoryName(type));
                }
            }

            // Create entry for item pools.
            tvData.Nodes.Add(typeof(ItemPool).AssemblyQualifiedName, typeof(ItemPool).Name);

            // Re-add existing data.
            foreach (var factory in FactoryManager.GetFactories())
            {
                HandleFactoryAdded(factory);
            }
            foreach (var pool in ItemPoolManager.GetItemPools())
            {
                HandleItemPoolAdded(pool);
            }

            // Reenable tree updating and validation.
            tvData.EndUpdate();
            --_isValidationEnabled;

            // Rescan.
            ScanForIssues();
        }

        #endregion

        #region Selection

        /// <summary>
        /// Selects the factory in our property grid if it exists, else selects
        /// nothing (clears property grid).
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <returns></returns>
        private bool SelectFactory(IFactory factory)
        {
            // Deselect object first, to reset property selection in property grid.
            pgProperties.SelectedObject = null;

            // See if that factory is known and select it if possible.
            if (factory != null)
            {
                pgProperties.SelectedObject = factory;
                tvData.SelectedNode = tvData.Nodes.Find(factory.Name, true).FirstOrDefault(n => IsFactory(n.Parent));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects the Itempool in our property grid if it exists, else selects
        /// nothing (clears property grid).
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <returns></returns>
        private bool SelectItemPool(ItemPool pool)
        {
            // Deselect object first, to reset property selection in property grid.
            pgProperties.SelectedObject = null;

            // See if that item pool is known and select it if possible.
            if (pool != null)
            {
                pgProperties.SelectedObject = pool;
                tvData.SelectedNode = tvData.Nodes.Find(pool.Name, true).FirstOrDefault(n => IsItemPool(n.Parent));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects the property with the specified name.
        /// </summary>
        /// <param name="fullPath">The full path to the property, separated by points ('.').</param>
        /// <returns>Whether the property was successfully selected or not.</returns>
        private bool SelectProperty(string fullPath)
        {
            // Get the root node at which to start.
            var root = pgProperties.SelectedGridItem;
            while (root.Parent != null)
            {
                root = root.Parent;
            }

            // Split our path.
            var path = new Stack<string>(fullPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Reverse());

            // Create stack for grid traversal (we need this to skip
            // category entries, that do not represent actual properties).
            var branches = new Stack<Tuple<GridItem, string>>();
            branches.Push(Tuple.Create(root, path.Pop()));

            // Walk down our path.
            while (branches.Count > 0)
            {
                var currentLevel = branches.Pop();
                var node = currentLevel.Item1;
                var propertyName = currentLevel.Item2;

                // If it's an indexed property, get the entry in the array.
                GridItem property;
                if (propertyName.EndsWith("]"))
                {
                    var openingBracket = propertyName.LastIndexOf("[", StringComparison.Ordinal);
                    var index = int.Parse(propertyName.Substring(openingBracket + 1, propertyName.Length - openingBracket - 2));
                    property = node.GridItems[propertyName.Substring(0, openingBracket)];
                    if (property != null)
                    {
                        property = property.GridItems[index];
                    }
                }
                else
                {
                    property = node.GridItems[propertyName];
                }

                // See if we can directly get the property.
                if (property != null)
                {
                    // Got part of our path, see how to continue.
                    if (path.Count > 0)
                    {
                        // Still more to go.
                        branches.Clear();
                        branches.Push(Tuple.Create(property, path.Pop()));
                        continue;
                    }
                    else
                    {
                        // We're done, expand (otherwise we get an error) select and quit.
                        var parent = property.Parent;
                        while (parent != null)
                        {
                            if (parent.Expandable)
                            {
                                parent.Expanded = true;
                            }
                            parent = parent.Parent;
                        }
                        pgProperties.SelectedGridItem = property;
                        return true;
                    }
                }

                // Otherwise check for skippable entries.
                foreach (GridItem item in node.GridItems)
                {
                    if (item.GridItemType == GridItemType.Category)
                    {
                        branches.Push(Tuple.Create(item, propertyName));
                    }
                }
            }

            // See if we have a node.
            if (root != null)
            {
                // Yes, select it and report success.
                root.Select();
                return true;
            }

            return false;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Determines whether the specified tree node represents a factory.
        /// </summary>
        /// <param name="node">The tree node.</param>
        /// <returns>
        ///   <c>true</c> if the specified node represents a factory; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsFactory(TreeNode node)
        {
            return node != null && typeof(IFactory).IsAssignableFrom(Type.GetType(node.Name));
        }

        /// <summary>
        /// Determines whether the specified tree node represents an item pool.
        /// </summary>
        /// <param name="node">The tree node.</param>
        /// <returns>
        ///   <c>true</c> if the specified node represents an item pool; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsItemPool(TreeNode node)
        {
            return node != null && typeof(ItemPool).IsAssignableFrom(Type.GetType(node.Name));
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

        #endregion
    }
}
