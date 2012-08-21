using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Space.ComponentSystem.Factories;

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
            var oldImage = pbPreview.Image;
            pbPreview.BackgroundImage = null;
            pbPreview.Image = null;
            if (oldBackground != null)
            {
                oldBackground.Dispose();
            }
            if (oldImage != null)
            {
                oldImage.Dispose();
            }

            // Stop if nothing is selected.
            if (pgProperties.SelectedObject == null ||
                pgProperties.SelectedGridItem == null ||
                pgProperties.SelectedGridItem.PropertyDescriptor == null)
            {
                return;
            }

            // Figure out what to show. If an image asset is selected we
            // simply show that.
            if (pgProperties.SelectedGridItem.PropertyDescriptor.Attributes[typeof(EditorAttribute)] != null &&
                ((EditorAttribute)pgProperties.SelectedGridItem.PropertyDescriptor.Attributes[typeof(EditorAttribute)])
                    .EditorTypeName.Equals(typeof(TextureAssetEditor).AssemblyQualifiedName))
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
                return;
            }

            var objectType = pgProperties.SelectedObject.GetType();
            if (objectType == typeof(ItemFactory))
            {

            }
        }

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
