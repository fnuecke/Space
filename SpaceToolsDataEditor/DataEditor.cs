using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    public sealed partial class DataEditor : Form
    {
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
        private readonly AddItemPoolDialog _itemPoolDialog = new AddItemPoolDialog();

        public DataEditor()
        {
            InitializeComponent();
            InitializeLogic();

            pbPreview.Parent.Controls.Add(_ingamePreview);
            pbPreview.Parent.Controls.Add(_effectPreview);
            pbPreview.Parent.Controls.Add(_planetPreview);
            pbPreview.Parent.Controls.Add(_sunPreview);
            pbPreview.Parent.Controls.Add(_projectilePreview);

            lvIssues.ListViewItemSorter = new IssueComparer();
            pgProperties.PropertyValueChanged += (o, args) =>
            {
                pgProperties.Refresh();
                SavingEnabled = true;
            };
            FactoryManager.FactoryAdded += factory => SavingEnabled = true;
            FactoryManager.FactoryRemoved += factory => SavingEnabled = true;
            FactoryManager.FactoryNameChanged += (a,b) => SavingEnabled = true;

            ItemPoolManager.ItemPoolAdded += itemPool => SavingEnabled = true;
            ItemPoolManager.ItemPoolRemoved += itemPool => SavingEnabled = true;
            ItemPoolManager.ItemPoolNameChanged += (a, b) => SavingEnabled = true;

            pbPreview.Image = new Bitmap(2048, 2048, PixelFormat.Format32bppArgb);

            tvData.KeyDown += (sender, args) =>
            {
                if (args.KeyCode == Keys.Delete && args.Shift)
                {
                    args.Handled = true;
                    RemoveClick(sender, args);
                }
            };
            tvData.NodeMouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    tvData.SelectedNode = args.Node;
                }
            };

            var settings = DataEditorSettings.Default;

            if (!string.IsNullOrWhiteSpace(settings.LastOpenedFolder))
            {
                _openDialog.InitialDirectory = settings.LastOpenedFolder;
                if (settings.AutoLoad)
                {
                    LoadData(_openDialog.InitialDirectory);
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

            if (settings.WindowMaximized)
            {
                WindowState = FormWindowState.Maximized;
            }

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

        private void ExitClick(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void SaveClick(object sender, EventArgs e)
        {
            FactoryManager.Save();
            ItemPoolManager.Save();
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

            LoadData(_openDialog.FileName);

            SavingEnabled = false;
        }

        private void FactorySelected(object sender, TreeViewEventArgs e)
        {
            if (SelectFactory(e.Node.Name) ||
                SelectItemPool(e.Node.Name))
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
            if (WindowState == FormWindowState.Maximized)
            {
                settings.WindowBounds = RestoreBounds;
                settings.WindowMaximized = true;
            }
            else
            {
                settings.WindowBounds = DesktopBounds;
                settings.WindowMaximized = false;
            }

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
        private void AddItemPoolClick(object sender, EventArgs e)
        {
            if (_itemPoolDialog.ShowDialog(this) == DialogResult.OK)
            {
                var type = _itemPoolDialog.ItemPoolType;
                var name = _itemPoolDialog.FactoryName;

                try
                {
                    // Create a new instance of this factory type.
                    var instance = Activator.CreateInstance(type) as ItemPool;
                    if (instance == null)
                    {
                        // This should not happen. Ever.
                        throw new ArgumentException("Resulting object was not a factory.");
                    }
                    instance.Name = name;

                    // Register it.
                    ItemPoolManager.Add(instance);

                    // And select it.
                    SelectItemPool(name);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Failed creating new Item Pool instance:\n" + ex, "Error",
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

            if (tvData.Focused || (sender == miDelete && miDelete.Visible))
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
                else if (pgProperties.SelectedObject is ItemPool)
                {
                    var itemPool = (ItemPool)pgProperties.SelectedObject;
                    if (ModifierKeys == Keys.Shift ||
                        System.Windows.MessageBox.Show("Are you sure you wish to delete '" + itemPool.Name + "'?",
                                                       "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question,
                                                       MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        ItemPoolManager.Remove(itemPool);
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
            UpdatePreview(true);
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
                if (!string.IsNullOrWhiteSpace(xProperty) && !string.IsNullOrWhiteSpace(yProperty) && !xProperty.Equals(yProperty))
                {
                    // Sort by property.
                    return string.CompareOrdinal(xProperty, yProperty);
                }
                // Sort by message.
                return string.CompareOrdinal(xMessage, yMessage);
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
