using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
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
            tvData.BeginUpdate();
            foreach (var type in FactoryManager.GetFactoryTypes())
            {
                // Create node in tree.
                tvData.Nodes.Add(type.Name, CleanFactoryName(type));
            }
            tvData.EndUpdate();

            // Rescan for issues when a property changes.
            pgProperties.PropertyValueChanged += (o, args) => ScanForIssues((IFactory)pgProperties.SelectedObject);

            FactoryManager.FactoryAdded += factory => tvData.Nodes[factory.GetType().Name].Nodes.Add(factory.Name, factory.Name);
            FactoryManager.FactoriesCleared += () =>
            {
                tvData.BeginUpdate();
                foreach (TreeNode node in tvData.Nodes)
                {
                    node.Nodes.Clear();
                }
                tvData.EndUpdate();

                // No factories means less issues! Rescan anyway, because some settings might be bad.
                ScanForIssues();
            };
        }

        /// <summary>
        /// Cleans the name of the factory type by stripping the 'Factory' postfix
        /// if it exists.
        /// </summary>
        /// <param name="type">The type of the factory.</param>
        /// <returns>The cleaned factory name.</returns>
        private static string CleanFactoryName(Type type)
        {
            return type.Name.EndsWith("Factory", StringComparison.InvariantCulture) ? type.Name.Substring(0, type.Name.Length - "Factory".Length) : type.Name;
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
        private void SelectFactory(string name)
        {
            pgProperties.SelectedObject = null;
            var factory = FactoryManager.GetFactory(name);
            if (factory != null)
            {
                pgProperties.SelectedObject = factory;
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
            foreach (var factory in FactoryManager.GetFactories())
            {
                ScanForIssues(factory);
            }
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
                    AddIssue("Property marked as texture asset is not of type string.", factory.Name, property.Name);
                }
                else
                {
                    var path = ((string)property.GetValue(factory));
                    if (!string.IsNullOrWhiteSpace(path) && !ContentProjectManager.HasTextureAsset(path.Replace('\\', '/')))
                    {
                        AddIssue("Invalid texture asset name, no such texture asset.", factory.Name, property.Name, IssueType.Error);
                    }
                }
            }

            // Check ship loadouts for validity (used slots vs. available slots, item name).
            if (factory is ShipFactory)
            {
                var root = ((ShipFactory)factory).Items;
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
                outer: while (items.Count > 0)
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
                            AddIssue("Items equipped in a null item, will be ignored when generating the ship.", factory.Name, "Items");
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
                            AddIssue("Empty item declaration, will be skipped when generating the ship.", factory.Name, "Items", IssueType.Information);
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
                            AddIssue("Equipped item cannot be fit into any slot: " + slotItemFactory.Name + "(parent: " + itemFactory.Name + ")", factory.Name, "Items", IssueType.Error);
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

            lvIssues.EndUpdate();
        }
    }
}
