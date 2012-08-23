using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Factories;
using Space.Data;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditor : Form
    {
        /// <summary>
        /// Possible issue types.
        /// </summary>
        public enum IssueType
        {
            None,
            Success,
            Information,
            Warning,
            Error
        }

        private readonly CommonOpenFileDialog _openDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            EnsurePathExists = true,
            EnsureValidNames = true,
            InitialDirectory = System.Windows.Forms.Application.StartupPath,
            Title = "Select folder with factory XMLs"
        };

        private readonly DataEditorSettingsDialog _settingsDialog = new DataEditorSettingsDialog();

        private readonly AddFactoryDialog _factoryDialog = new AddFactoryDialog();

        public DataEditor()
        {
            InitializeComponent();
            InitializeLogic();

            lvIssues.ListViewItemSorter = new IssueComparer();
            pgProperties.PropertyValueChanged += (o, args) =>
            {
                pgProperties.Refresh();
                SavingEnabled = true;
            };
            FactoryManager.FactoryAdded += factory => SavingEnabled = true;
            FactoryManager.FactoryRemoved += factory => SavingEnabled = true;
            FactoryManager.FactoryNameChanged += (a,b) => SavingEnabled = true;
            pbPreview.Image = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb);

            tvData.KeyDown += (sender, args) =>
            {
                if (args.KeyCode == Keys.Delete && args.Shift)
                {
                    args.Handled = true;
                    RemoveClick(sender, args);
                }
            };

            var settings = DataEditorSettings.Default;

            if (!string.IsNullOrWhiteSpace(settings.LastOpenedFolder))
            {
                _openDialog.InitialDirectory = settings.LastOpenedFolder;
                if (settings.AutoLoad)
                {
                    LoadFactories(_openDialog.InitialDirectory);
                    if (!string.IsNullOrWhiteSpace(settings.LastSelectedFactory))
                    {
                        var result = tvData.Nodes.Find(settings.LastSelectedFactory, true);
                        if (result.Length > 0)
                        {
                            tvData.SelectedNode = result[0];
                        }
                    }
                    SavingEnabled = false;
                }
            }

            // Restore layout.
            DesktopBounds = settings.WindowBounds;

            if (settings.MainSplit > 0)
            {
                scMain.SplitterDistance = settings.MainSplit;
            }
            if (settings.OuterSplit > 0)
            {
                scOuter.SplitterDistance = settings.OuterSplit;
            }
            if (settings.InnerSplit > 0)
            {
                scInner.SplitterDistance = settings.InnerSplit;
            }
            if (settings.IssuesDescriptionWidth > 0)
            {
                lvIssues.Columns[1].Width = settings.IssuesDescriptionWidth;
            }
            if (settings.IssuesFactoryWidth > 0)
            {
                lvIssues.Columns[2].Width = settings.IssuesFactoryWidth;
            }
            if (settings.IssuesPropertyWidth > 0)
            {
                lvIssues.Columns[3].Width = settings.IssuesPropertyWidth;
            }

            // IntermediateSerializer won't recognize these otherwise... -.-
            new Serialization.SpaceAttributeModifierConstraintSerializer();
            new Serialization.SpaceAttributeModifierSerializer();
        }

        private void DataEditorLoad(object sender, EventArgs e)
        {
            if (DataEditorSettings.Default.FirstStart)
            {
                DataEditorSettings.Default.FirstStart = false;
                DataEditorSettings.Default.Save();

                if (System.Windows.MessageBox.Show("It appears you are starting the program for the first time.\n" +
                                                   "Do you wish to configure your data directories now?\n" +
                                                   "(You can also do so later in the settings)", "First start",
                                                   MessageBoxButton.YesNo, MessageBoxImage.Question,
                                                   MessageBoxResult.Yes) == MessageBoxResult.Yes)
                {
                    _settingsDialog.ShowDialog(this);
                }
            }
        }

        public bool SavingEnabled
        {
            get { return tsmiSave.Enabled; }
            set
            {
                Text = "Space - Data Editor" + (value ? " (*)" : "");
                tsmiSave.Enabled = value;
            }
        }

        /// <summary>
        /// Adds a new issue to the issues list.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="factory">The factory, if any.</param>
        /// <param name="property">The property, if any.</param>
        /// <param name="type">The type.</param>
        public void AddIssue(string message, string factory = "", string property = "", IssueType type = IssueType.Success)
        {
            if ((int)type < 1)
            {
                type = IssueType.Success;
            }
            lvIssues.Items.Add(new ListViewItem(new[] { "", message, factory, property }, (int)type - 1));
        }

        /// <summary>
        /// Adds a new issue to the issues list.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        public void AddIssue(string message, IssueType type = IssueType.Success)
        {
            AddIssue(message, "", "", type);
        }

        /// <summary>
        /// Removes all issues from the issue list.
        /// </summary>
        public void ClearIssues()
        {
            lvIssues.Items.Clear();
        }

        /// <summary>
        /// Removes all known issues for the factory with the specified name.
        /// </summary>
        /// <param name="factory">The factory.</param>
        private void RemoveIssuesForFactory(string factory)
        {
            lvIssues.BeginUpdate();

            for (var i = lvIssues.Items.Count - 1; i >= 0; i--)
            {
                if (lvIssues.Items[i].SubItems[2].Text.Equals(factory))
                {
                    lvIssues.Items.RemoveAt(i);
                }
            }

            lvIssues.EndUpdate();
        }

        private void ExitClick(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void SaveClick(object sender, EventArgs e)
        {
            FactoryManager.Save();
            SavingEnabled = false;
        }

        private void LoadClick(object sender, EventArgs e)
        {
            if (_openDialog.ShowDialog(Handle) != CommonFileDialogResult.Ok)
            {
                return;
            }
            if (!Directory.Exists(_openDialog.FileName))
            {
                return;
            }

            DataEditorSettings.Default.LastOpenedFolder = _openDialog.FileName;
            DataEditorSettings.Default.Save();

            LoadFactories(_openDialog.FileName);

            SavingEnabled = false;
        }

        private void FactorySelected(object sender, TreeViewEventArgs e)
        {
            if (SelectFactory(e.Node.Name)||SelectItemPool(e.Node.Name))
            {
                tsmiRemove.Enabled = true;
                miDelete.Enabled = true;
            }
            else
            {
                tsmiRemove.Enabled = false;
                miDelete.Enabled = false;
            }
            UpdatePreview();
        }

        private void DataEditorClosing(object sender, FormClosingEventArgs e)
        {
            if (SavingEnabled)
            {
                switch (System.Windows.MessageBox.Show("You have unsaved changes, do you want to save them now?", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel))
                {
                    case MessageBoxResult.Yes:
                        SaveClick(null, null);
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }

            var settings = DataEditorSettings.Default;

            // Save layout.
            settings.WindowBounds = DesktopBounds;

            settings.MainSplit = scMain.SplitterDistance;
            settings.OuterSplit = scOuter.SplitterDistance;
            settings.InnerSplit = scInner.SplitterDistance;

            settings.IssuesDescriptionWidth = lvIssues.Columns[1].Width;
            settings.IssuesFactoryWidth = lvIssues.Columns[2].Width;
            settings.IssuesPropertyWidth = lvIssues.Columns[3].Width;

            if (tvData.SelectedNode != null)
            {
                settings.LastSelectedFactory = tvData.SelectedNode.Name;
            }

            settings.Save();
        }

        private void SettingsClick(object sender, EventArgs e)
        {
            _settingsDialog.ShowDialog(this);
        }

        private void AddFactoryClick(object sender, EventArgs e)
        {
            if (_factoryDialog.ShowDialog(this) == DialogResult.OK)
            {
                var type = _factoryDialog.FactoryType;
                var name = _factoryDialog.FactoryName;

                try
                {
                    // Create a new instance of this factory type.
                    var instance = Activator.CreateInstance(type) as IFactory;
                    if (instance == null)
                    {
                        // This should not happen. Ever.
                        throw new ArgumentException("Resulting object was not a factory.");
                    }
                    instance.Name = name;

                    // Register it.
                    FactoryManager.Add(instance);

                    // And select it.
                    SelectFactory(name);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Failed creating new factory instance:\n" + ex, "Error",
                                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveClick(object sender, EventArgs e)
        {
            if (pgProperties.SelectedObject == null)
            {
                return;
            }

            if (tvData.Focused || sender == miDelete)
            {
                if (pgProperties.SelectedObject is IFactory)
                {
                    var factory = (IFactory)pgProperties.SelectedObject;
                    if (ModifierKeys == Keys.Shift ||
                        System.Windows.MessageBox.Show("Are you sure you wish to delete '" + factory.Name + "'?",
                                                       "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question,
                                                       MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        FactoryManager.Remove(factory);
                    }
                }
            }
        }

        private void PropertiesSelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void PropertiesPropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            // Clear image.
            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.FillRectangle(Brushes.Transparent, 0, 0, pbPreview.Image.Width, pbPreview.Image.Height);
            }

            // Stop if nothing is selected.
            if (pgProperties.SelectedObject != null &&
                pgProperties.SelectedGridItem != null)
            {
                // Figure out what to show. If an image asset is selected we simply show that.
                if (pgProperties.SelectedGridItem.PropertyDescriptor != null &&
                    pgProperties.SelectedGridItem.PropertyDescriptor.Attributes[typeof(EditorAttribute)] != null &&
                    ((EditorAttribute)pgProperties.SelectedGridItem.PropertyDescriptor.Attributes[typeof(EditorAttribute)])
                        .EditorTypeName.Equals(typeof(TextureAssetEditor).AssemblyQualifiedName))
                {
                    RenderTextureAssetPreview();
                }
                else
                {
                    // We're not rendering based on property grid item selection at this point, so
                    // we just try to render the selected object.

                    // Try rendering the selected object as an item.
                    RenderItemPreview(pgProperties.SelectedObject as ItemFactory);

                    // Try rendering a ship.
                    RenderShipPreview(pgProperties.SelectedObject as ShipFactory);
                }
            }

            // Update the picture box.
            pbPreview.Invalidate();
        }

        private void RenderTextureAssetPreview()
        {
            // OK, render that image (or try to).
            var fullPath = (string)pgProperties.SelectedGridItem.Value;
            if (fullPath == null)
            {
                return;
            }

            // Make all forward slashes backslashes for the following split.
            fullPath = fullPath.Replace('\\', '/');

            var filePath = ContentProjectManager.GetFileForTextureAsset(fullPath);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                // We got it. Set as the new image.
                try
                {
                    using (var img = Image.FromFile(filePath))
                    {
                        using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                        {
                            g.DrawImage(img, (pbPreview.Image.Width - img.Width) / 2f, (pbPreview.Image.Height - img.Height) / 2f, img.Width, img.Height);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                }
            }
        }

        private void RenderItemPreview(ItemFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // Draw base image.
            var modelFile = ContentProjectManager.GetFileForTextureAsset(factory.Model);
            if (modelFile != null)
            {
                using (var bmp = new Bitmap(modelFile))
                {
                    var newWidth = factory.RequiredSlotSize.Scale(bmp.Width);
                    var newHeight = factory.RequiredSlotSize.Scale(bmp.Height);
                    using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(bmp, (pbPreview.Image.Width - newWidth) / 2f, (pbPreview.Image.Height - newHeight) / 2f, newWidth, newHeight);
                    }
                }
            }

            // Draw slot mounting positions.
            if (factory.Slots != null)
            {
                // Set up required stuff for alpha blended drawing.
                var ia = new ImageAttributes();
                ia.SetColorMatrix(new ColorMatrix { Matrix00 = 1f, Matrix11 = 1f, Matrix22 = 1f, Matrix44 = 1f, Matrix33 = 0.7f });

                foreach (var slot in factory.Slots)
                {
                    Image mountpointImage;
                    MountpointImages.TryGetValue(slot.Type, out mountpointImage);
                    if (mountpointImage == null)
                    {
                        continue;
                    }
                    var size = slot.Size.Scale(16);
                    var x = (pbPreview.Image.Width - size - 1) / 2f;
                    var y = (pbPreview.Image.Height - size - 1) / 2f;
                    if (slot.Offset.HasValue)
                    {
                        x += slot.Offset.Value.X;
                        y += slot.Offset.Value.Y;
                    }
                    using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(mountpointImage, new System.Drawing.Rectangle((int)x, (int)y, (int)size, (int)size), 0, 0, mountpointImage.Width, mountpointImage.Height, GraphicsUnit.Pixel, ia);
                    }
                }
            }
        }

        private void RenderShipPreview(ShipFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            // Draw base image.
            var modelFile = ContentProjectManager.GetFileForTextureAsset(factory.Texture);
            if (modelFile != null)
            {
                using (var bmp = new Bitmap(modelFile))
                {
                    using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(bmp, (pbPreview.Image.Width - bmp.Width) / 2f, (pbPreview.Image.Height - bmp.Height) / 2f, bmp.Width, bmp.Height);
                    }
                }
            }

            // Draw equipped items.
            var items = new Stack<Tuple<ShipFactory.ItemInfo, List<ItemFactory.ItemSlotInfo>, Vector2, float>>();
            if (factory.Items != null)
            {
                items.Push(Tuple.Create(factory.Items,
                                        new List<ItemFactory.ItemSlotInfo>
                                        {
                                            new ItemFactory.ItemSlotInfo
                                            {
                                                Size = ItemSlotSize.Small,
                                                Type = ItemFactory.ItemSlotInfo.ItemType.Fuselage
                                            }
                                        }, Vector2.Zero, 0f));
            }
            while (items.Count > 0)
            {
                var info = items.Pop();
                var itemInfo = info.Item1;
                var slots = info.Item2;
                var offset = info.Item3;
                var multiplier = info.Item4;

                // Get info on item.
                var itemFactory = FactoryManager.GetFactory(itemInfo.Name) as ItemFactory;
                if (itemFactory == null)
                {
                    continue;
                }

                // Find smallest slot we fit into.
                ItemFactory.ItemSlotInfo bestSlot = null;
                foreach (var slot in slots)
                {
                    if (slot.Type == itemFactory.GetType().ToItemType() &&
                        slot.Size >= itemFactory.RequiredSlotSize)
                    {
                        if (bestSlot == null || slot.Size < bestSlot.Size)
                        {
                            bestSlot = slot;
                        }
                    }
                }
                // Could not find a slot.
                if (bestSlot == null)
                {
                    continue;
                }

                // Consume the slot.
                slots.Remove(bestSlot);

                // Render.
                if (bestSlot.Offset.HasValue)
                {
                    if (multiplier == 0f && bestSlot.Offset.Value.Y != 0f)
                    {
                        multiplier = Math.Sign(bestSlot.Offset.Value.Y);
                        offset += bestSlot.Offset.Value;
                    }
                    else
                    {
                        offset.X += bestSlot.Offset.Value.X;
                        offset.Y += bestSlot.Offset.Value.Y * multiplier;
                    }
                }

                var modelPath = ContentProjectManager.GetFileForTextureAsset(itemFactory.Model);
                if (modelPath != null)
                {
                    try
                    {
                        using (var img = Image.FromFile(modelPath))
                        {
                            var width = bestSlot.Size.Scale(img.Width);
                            var height = bestSlot.Size.Scale(img.Height);
                            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.DrawImage(img, (pbPreview.Image.Width - width) / 2f + offset.X,
                                            (pbPreview.Image.Height - height) / 2f + offset.Y, width, height);
                            }
                        }
                    }
                    catch(FileNotFoundException)
                    {
                    }
                }

                // Queue child items (if we have potential slots for them).
                if (itemInfo.Slots != null && itemFactory.Slots != null && itemFactory.Slots.Length > 0)
                {
                    // Use same list for all children to make sure we consume from the same list.
                    var availableSlots = new List<ItemFactory.ItemSlotInfo>(itemFactory.Slots);
                    foreach (var slot in itemInfo.Slots)
                    {
                        items.Push(Tuple.Create(slot, availableSlots, offset, multiplier));
                    }
                }
            }
        }

        private static readonly Dictionary<ItemFactory.ItemSlotInfo.ItemType, Image> MountpointImages = new Dictionary<ItemFactory.ItemSlotInfo.ItemType, Image>
        {
            {ItemFactory.ItemSlotInfo.ItemType.Armor, Images.mountpoint_armor},
            {ItemFactory.ItemSlotInfo.ItemType.Fuselage, Images.mountpoint_fuselage},
            {ItemFactory.ItemSlotInfo.ItemType.Reactor, Images.mountpoint_reactor},
            {ItemFactory.ItemSlotInfo.ItemType.Sensor, Images.mountpoint_sensor},
            {ItemFactory.ItemSlotInfo.ItemType.Shield, Images.mountpoint_shield},
            {ItemFactory.ItemSlotInfo.ItemType.Thruster, Images.mountpoint_thruster},
            {ItemFactory.ItemSlotInfo.ItemType.Weapon, Images.mountpoint_weapon},
            {ItemFactory.ItemSlotInfo.ItemType.Wing, Images.mountpoint_wing},
        };

        private void IssuesDoubleClick(object sender, EventArgs e)
        {
            if (lvIssues.SelectedItems.Count == 0)
            {
                return;
            }
            // Figure out factory and property names.
            var factory = lvIssues.SelectedItems[0].SubItems[2].Text;
            var property = lvIssues.SelectedItems[0].SubItems[3].Text;

            // Navigate to that factory.
            var nodes = tvData.Nodes.Find(factory, true);
            if (nodes.Length == 0)
            {
                return;
            }
            tvData.SelectedNode = nodes[0];

            // Navigate to that property.
            if (SelectProperty(property))
            {
                // Got the property, focus the property grid.
                pgProperties.Focus();   
            }
            else
            {
                // No such property, focus tree.
                tvData.Focus();
            }
        }

        /// <summary>
        /// Selects the property with the specified name.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        private bool SelectProperty(string name)
        {
            var root = pgProperties.SelectedGridItem;
            while (root.Parent != null)
            {
                root = root.Parent;
            }

            var nodes = new Stack<GridItem>();
            nodes.Push(root);
            while (nodes.Count > 0)
            {
                var node = nodes.Pop();
                if (name.Equals(node.Label))
                {
                    node.Select();
                    return true;
                }
                foreach (GridItem child in node.GridItems)
                {
                    nodes.Push(child);
                }
            }
            return false;
        }

        /// <summary>
        /// Sorter for issue list.
        /// </summary>
        private sealed class IssueComparer : IComparer<ListViewItem>, IComparer
        {
            public int Compare(ListViewItem x, ListViewItem y)
            {
                var xMessage = x.SubItems[1].Text;
                var xFactory = x.SubItems[2].Text;
                var xProperty = x.SubItems[3].Text;

                var yMessage = y.SubItems[1].Text;
                var yFactory = y.SubItems[2].Text;
                var yProperty = y.SubItems[3].Text;

                if (!string.IsNullOrWhiteSpace(xFactory) && !string.IsNullOrWhiteSpace(yFactory) && !xFactory.Equals(yFactory))
                {
                    // Sort by factory.
                    return string.CompareOrdinal(xFactory, yFactory);
                }
                else if (!string.IsNullOrWhiteSpace(xProperty) && !string.IsNullOrWhiteSpace(yProperty) && !xProperty.Equals(yProperty))
                {
                    // Sort by property.
                    return string.CompareOrdinal(xProperty, yProperty);
                }
                else
                {
                    // Sort by message.
                    return string.CompareOrdinal(xMessage, yMessage);   
                }
            }

            public int Compare(object x, object y)
            {
                if (x is ListViewItem && y is ListViewItem)
                {
                    return Compare((ListViewItem)x, (ListViewItem)y);
                }
                throw new ArgumentException("Invalid item type.");
            }
        }
    }
}
