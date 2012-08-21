using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
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
            InitialDirectory = Application.StartupPath,
            Title = "Select folder with factory XMLs"
        };

        private readonly DataEditorSettingsDialog _settingsDialog = new DataEditorSettingsDialog();

        public DataEditor()
        {
            InitializeComponent();
            InitializeLogic();

            lvIssues.ListViewItemSorter = new IssueComparer();
            pgProperties.PropertyValueChanged += (o, args) => pgProperties.Refresh();
            pbPreview.Image = new Bitmap(1024, 1024);

            var settings = DataEditorSettings.Default;

            if (!string.IsNullOrWhiteSpace(settings.LastOpenedFolder))
            {
                _openDialog.InitialDirectory = settings.LastOpenedFolder;
                if (settings.AutoLoad)
                {
                    LoadFactories(_openDialog.InitialDirectory);
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

        /// <summary>
        /// Adds a new issue to the issues list.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="factory">The factory, if any.</param>
        /// <param name="property">The property, if any.</param>
        /// <param name="type">The type.</param>
        public void AddIssue(string message, string factory = "", string property = "", IssueType type = IssueType.Warning)
        {
            if ((int)type < 1)
            {
                type = IssueType.Success;
            }
            lvIssues.Items.Add(new ListViewItem(new[] { "", message, factory, property }, (int)type - 1));
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
            Application.Exit();
        }

        private void SaveClick(object sender, EventArgs e)
        {

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
        }

        private void FactorySelected(object sender, TreeViewEventArgs e)
        {
            SelectFactory(e.Node.Name);
            UpdatePreview();
        }

        private void DataEditorClosing(object sender, FormClosingEventArgs e)
        {
            var settings = DataEditorSettings.Default;

            // Save layout.
            settings.WindowBounds = DesktopBounds;

            settings.MainSplit = scMain.SplitterDistance;
            settings.OuterSplit = scOuter.SplitterDistance;
            settings.InnerSplit = scInner.SplitterDistance;

            settings.IssuesDescriptionWidth = lvIssues.Columns[1].Width;
            settings.IssuesFactoryWidth = lvIssues.Columns[2].Width;
            settings.IssuesPropertyWidth = lvIssues.Columns[3].Width;

            settings.Save();
        }

        private void SettingsClick(object sender, EventArgs e)
        {
            _settingsDialog.ShowDialog(this);
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
            // Clear preview.
            var oldBackground = pbPreview.BackgroundImage;
            pbPreview.BackgroundImage = null;
            using (var g = System.Drawing.Graphics.FromImage(pbPreview.Image))
            {
                g.Clear(System.Drawing.Color.Transparent);
            }
            if (oldBackground != null)
            {
                oldBackground.Dispose();
            }

            // Stop if nothing is selected.
            if (pgProperties.SelectedObject == null ||
                pgProperties.SelectedGridItem == null)
            {
                return;
            }

            // Figure out what to show. If an image asset is selected we simply show that.
            if (pgProperties.SelectedGridItem.PropertyDescriptor != null &&
                pgProperties.SelectedGridItem.PropertyDescriptor.Attributes[typeof(EditorAttribute)] != null &&
                ((EditorAttribute)pgProperties.SelectedGridItem.PropertyDescriptor.Attributes[typeof(EditorAttribute)])
                    .EditorTypeName.Equals(typeof(TextureAssetEditor).AssemblyQualifiedName))
            {
                RenderTextureAssetPreview();
                return;
            }

            // We're not rendering based on property grid item selection at this point, so
            // we just try to render the selected object.

            // Try rendering the selected object as an item.
            RenderItemPreview(pgProperties.SelectedObject as ItemFactory);

            // Try rendering a ship.
            RenderShipPreview(pgProperties.SelectedObject as ShipFactory);
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
            fullPath = fullPath.Replace('/', '\\');

            var filePath = ContentProjectManager.GetFileForTextureAsset(fullPath);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                // We got it. Set as the new image.
                try
                {
                    pbPreview.BackgroundImage = Image.FromFile(filePath);
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
                var bmp = new Bitmap(modelFile);
                var size = factory.RequiredSlotSize.ToPixelSize();
                var scale = size / (float)Math.Max(bmp.Width, bmp.Height);
                var newWidth = (int)(bmp.Width * scale);
                var newHeight = (int)(bmp.Height * scale);
                if (newWidth != bmp.Width || newHeight != bmp.Height)
                {
                    // Need resizing.
                    var newBmp = new Bitmap(newWidth, newHeight);
                    using (var g = System.Drawing.Graphics.FromImage(newBmp))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(bmp, 0, 0, newWidth, newHeight);
                    }
                    // Kill old bmp, use new.
                    bmp.Dispose();
                    bmp = newBmp;
                }
                pbPreview.BackgroundImage = bmp;
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
                    var size = slot.Size.ToPixelSize();
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
                        g.DrawImage(mountpointImage, new Rectangle((int)x, (int)y, size, size), 0, 0, mountpointImage.Width, mountpointImage.Height, GraphicsUnit.Pixel, ia);
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
