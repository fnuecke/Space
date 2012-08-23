using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Space.ComponentSystem.Factories;
using Space.ComponentSystem.Factories.SunSystemFactoryTypes;

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

            ItemPoolManager.ItemPoolAdded += HandleItemPoolAdded;
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

        private void HandleItemPoolAdded(ItemPool pool)
        {
            var insertionNode = tvData.Nodes.Find("ItemPool", true);
            if (insertionNode.Length > 0)
            {
                insertionNode[0].Nodes.Add(pool.Name, pool.Name);
            }

            // Validate that factory.
            ScanForIssues(pool);
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
            tvData.Nodes.Add("ItemPool", "Item Pool");
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
            ItemPoolManager.Load(path);
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
        /// <summary>
        /// Selects the factory in our property grid if it exists, else selects
        /// nothing (clears property grid).
        /// </summary>
        /// <param name="name">The name of the factory.</param>
        private bool SelectItemPool(string name)
        {
            pgProperties.SelectedObject = null;
            //tvData.SelectedNode = null;
            var factory = ItemPoolManager.GetItemPool(name);
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
                    AddIssue("Path to content project is invalid: " + project, IssueType.Warning);
                }
            }

            // Check factories.
            foreach (var factory in FactoryManager.GetFactories())
            {
                ScanForIssues(factory);
            }

            //check itempools
            foreach (var itemPool in ItemPoolManager.GetItemPool())
            {
                ScanForIssues(itemPool);
            }
        }

        /// <summary>
        /// Scans for issues for a specific itempool.
        /// </summary>
        /// <param name="itemPool">The itempool.</param>
        private void ScanForIssues(ItemPool itemPool)
        {
            
        }
        /// <summary>
        /// Scans for issues for a specific factory.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void ScanForIssues(IFactory factory)
        {
            // Remove issues involving this factory.
            RemoveIssuesForFactory(factory.Name);

            lvIssues.BeginUpdate();

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
                    AddIssue("Property marked as texture asset is not of type string.", factory.Name, property.Name, IssueType.Warning);
                }
                else
                {
                    var path = ((string)property.GetValue(factory));
                    if (!string.IsNullOrWhiteSpace(path) &&
                        !ContentProjectManager.HasTextureAsset(path.Replace('\\', '/')))
                    {
                        AddIssue("Invalid texture asset name, no such texture asset.", factory.Name, property.Name,
                                 IssueType.Error);
                    }
                }
            }

            // Check ship loadouts for validity (used slots vs. available slots, item name).
            ScanShipFactory(factory as ShipFactory);

            // Validate values for planets.
            ScanPlanetFactory(factory as PlanetFactory);

            // Validate values for suns.
            ScanSunFactory(factory as SunFactory);

            // Validate planet/sun names.
            ScanSunSystemFactory(factory as SunSystemFactory);

            lvIssues.EndUpdate();
        }

        private void ScanShipFactory(ShipFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            var root = factory.Items;
            if (root != null)
            {
                // We only check if the root item is a fuselage here, other checks will
                // we done in the loop below.
                var rootItem = FactoryManager.GetFactory(root.Name) as ItemFactory;
                if (rootItem != null && !(rootItem is FuselageFactory))
                {
                    AddIssue("Root item must always be a fuselage.", factory.Name, "Items", IssueType.Error);
                }
            }
            var items = new Stack<ShipFactory.ItemInfo>();
            items.Push(root);
            outer:
            while (items.Count > 0)
            {
                var item = items.Pop();
                if (item == null)
                {
                    continue;
                }

                // Check item name.
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    // Empty, make sure there are no child items.
                    if (item.Slots.Length > 0)
                    {
                        AddIssue("Items equipped in a null item, will be ignored when generating the ship.",
                                 factory.Name, "Items", IssueType.Warning);
                    }
                    continue;
                }

                // We have an item name, make sure it's a proper item.
                var itemFactory = FactoryManager.GetFactory(item.Name) as ItemFactory;
                if (itemFactory == null)
                {
                    AddIssue("Invalid item name or type: " + item.Name, factory.Name, "Items", IssueType.Error);
                    continue;
                }

                // Check used vs. available slots.
                var availableSlots = new List<ItemFactory.ItemSlotInfo>();
                if (itemFactory.Slots != null)
                {
                    availableSlots.AddRange(itemFactory.Slots);
                }
                foreach (var slot in item.Slots)
                {
                    // Skip empty ones.
                    if (string.IsNullOrWhiteSpace(slot.Name))
                    {
                        AddIssue("Empty item declaration, will be skipped when generating the ship.", factory.Name,
                                 "Items", IssueType.Information);
                        goto outer;
                    }

                    // Get the item of that type.
                    var slotItemFactory = FactoryManager.GetFactory(slot.Name) as ItemFactory;
                    if (slotItemFactory == null)
                    {
                        AddIssue("Invalid item name or type: " + slot.Name, factory.Name, "Items", IssueType.Error);
                        goto outer;
                    }

                    // OK, try to consume a slot (the smallest one possible).
                    var type = slotItemFactory.GetType().ToItemType();
                    var size = slotItemFactory.RequiredSlotSize;
                    ItemFactory.ItemSlotInfo bestSlot = null;
                    foreach (var availableSlot in availableSlots)
                    {
                        if (availableSlot.Type != type || availableSlot.Size < size)
                        {
                            continue;
                        }
                        if (bestSlot == null || availableSlot.Size < bestSlot.Size)
                        {
                            bestSlot = availableSlot;
                        }
                    }
                    if (bestSlot == null)
                    {
                        AddIssue(
                            "Equipped item cannot be fit into any slot: " + slotItemFactory.Name + " (parent: " +
                            itemFactory.Name + ")", factory.Name, "Items", IssueType.Error);
                        goto outer;
                    }
                    availableSlots.Remove(bestSlot);
                }

                // All well on this level, check children.
                foreach (var slot in item.Slots)
                {
                    items.Push(slot);
                }
            }
        }

        private void ScanPlanetFactory(PlanetFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            if (factory.Mass != null &&
                (factory.Mass.Low < 0 || factory.Mass.High < 0))
            {
                AddIssue("Mass is negative and will be ignored.", factory.Name, "Mass", IssueType.Information);
            }
            if (factory.Radius != null &&
                (factory.Radius.Low <= 0 || factory.Radius.High <= 0))
            {
                AddIssue("Planet radius should be larger then zero.", factory.Name, "Radius", IssueType.Warning);
            }
            if (factory.Eccentricity != null &&
                (factory.Eccentricity.Low < 0 || factory.Eccentricity.High < 0 ||
                 factory.Eccentricity.Low > 1 || factory.Eccentricity.High > 1))
            {
                AddIssue("Planet orbit ellipse eccentricity is in invalid value-range (should be in [0, 1]).",
                         factory.Name, "Eccentricity", IssueType.Warning);
            }
        }

        private void ScanSunFactory(SunFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            if (factory.Mass != null &&
                (factory.Mass.Low < 0 || factory.Mass.High < 0))
            {
                AddIssue("Mass is negative and will be ignored.", factory.Name, "Mass", IssueType.Information);
            }
            if (factory.Radius != null &&
                (factory.Radius.Low <= 0 || factory.Radius.High <= 0))
            {
                AddIssue("Sun radius should be larger then zero.", factory.Name, "Radius", IssueType.Warning);
            }
        }

        private void ScanSunSystemFactory(SunSystemFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // See if the selected sun is valid.
            var sun = factory.Sun;
            var sunFactory = FactoryManager.GetFactory(sun) as SunFactory;
            if (sunFactory == null && !string.IsNullOrWhiteSpace(sun))
            {
                AddIssue("Invalid sun name, no such sun type.", factory.Name, "Sun", IssueType.Error);
            }

            // See if the planets are valid.
            var orbits = new Stack<Orbit>();
            orbits.Push(factory.Planets);
            while (orbits.Count > 0)
            {
                var orbit = orbits.Pop();
                if (orbit == null)
                {
                    continue;
                }

                foreach (var orbiter in orbit.Orbiters)
                {
                    var orbiterFactory = FactoryManager.GetFactory(orbiter.Name) as PlanetFactory;
                    if (orbiterFactory == null && !string.IsNullOrWhiteSpace(orbiter.Name))
                    {
                        AddIssue("Invalid planet name, no such planet type: " + orbiter.Name, factory.Name,
                                 "Planets", IssueType.Error);
                    }
                    if (orbiter.OrbitRadius == null)
                    {
                        AddIssue("Nor orbit radius set for planet '" + orbiter.Name + "'.", factory.Name, "Planets",
                                 IssueType.Error);
                    }
                    else if (orbiter.OrbitRadius.Low <= 0 || orbiter.OrbitRadius.High <= 0)
                    {
                        AddIssue("Orbit radius should be larger than zero for planet '" + orbiter.Name + "'.",
                                 factory.Name, "Planets", IssueType.Warning);
                    }
                    if (orbiter.ChanceToExist <= 0)
                    {
                        AddIssue("Planet '" + orbiter.Name + "' will never be generated (probability <= 0).",
                                 factory.Name, "Planets", IssueType.Information);
                    }

                    orbits.Push(orbiter.Moons);
                }
            }
        }
    }
}
