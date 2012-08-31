using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
            InitialDirectory = Application.StartupPath,
            Title = "Select folder with factory XMLs"
        };

        private readonly DataEditorSettingsDialog _settingsDialog = new DataEditorSettingsDialog();

        private readonly AddFactoryDialog _factoryDialog = new AddFactoryDialog();
        private readonly AddItemPoolDialog _itemPoolDialog = new AddItemPoolDialog();
        private readonly AddAttributePoolDialog _attributePoolDialog = new AddAttributePoolDialog();

        private readonly Stack<Tuple<string, Func<bool>>> _undoCommands = new Stack<Tuple<string, Func<bool>>>();

        private int _changesSinceLastSave;

        public DataEditor()
        {
            InitializeComponent();
            InitializeLogic();

            pbPreview.Parent.Controls.Add(_ingamePreview);
            pbPreview.Parent.Controls.Add(_effectPreview);
            pbPreview.Parent.Controls.Add(_projectilePreview);

            lvIssues.ListViewItemSorter = new IssueComparer();

            pbPreview.Image = new Bitmap(2048, 2048, PixelFormat.Format32bppArgb);

            FactoryManager.FactoriesCleared += ClearUndo;
            ItemPoolManager.ItemPoolCleared += ClearUndo;

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

                if (MessageBox.Show(this,
                                    "It appears you are starting the program for the first time.\n" +
                                    "Do you wish to configure your data directories now?\n" +
                                    "(You can also do so later in the settings)",
                                    "First start", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _settingsDialog.ShowDialog(this);
                }
            }
        }

        private void ExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SaveClick(object sender, EventArgs e)
        {
            FactoryManager.Save();
            ItemPoolManager.Save();
            AttributePoolManager.Save();

            _changesSinceLastSave = 0;
            UpdateUndoMenu();
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

            _changesSinceLastSave = 0;
            LoadData(_openDialog.FileName);
        }

        private void DataSelected(object sender, TreeViewEventArgs e)
        {
            if (SelectFactory(e.Node.Tag as IFactory) ||
                SelectItemPool(e.Node.Tag as ItemPool) ||
                SelectAttributePool(e.Node.Tag as AttributePool))
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
            if (_changesSinceLastSave != 0)
            {
                switch (MessageBox.Show(this,
                                        "You have unsaved changes, do you want to save them now?",
                                        "Question", MessageBoxButtons.YesNoCancel,
                                        MessageBoxIcon.Exclamation))
                {
                    case DialogResult.Yes:
                        SaveClick(null, null);
                        break;
                    case DialogResult.Cancel:
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
                    SelectFactory(instance);

                    // Add undo command.
                    PushUndo("add factory", () =>
                    {
                        if (MessageBox.Show(this,
                                            "Are you sure you wish to delete the factory '" + instance.Name + "'?",
                                            "Confirmation", MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            FactoryManager.Remove(instance);
                            return true;
                        }
                        return false;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed creating new factory:\n" + ex, "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddItemPoolClick(object sender, EventArgs e)
        {
            if (_itemPoolDialog.ShowDialog(this) == DialogResult.OK)
            {
                var name = _itemPoolDialog.ItemPoolName;

                try
                {
                    // Create a new instance.
                    var instance = new ItemPool {Name = name};

                    // Register it.
                    ItemPoolManager.Add(instance);

                    // And select it.
                    SelectItemPool(instance);

                    // Add undo command.
                    PushUndo("add item pool", () =>
                    {
                        if (MessageBox.Show(this,
                                            "Are you sure you wish to delete the item pool '" + instance.Name + "'?",
                                            "Confirmation", MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            ItemPoolManager.Remove(instance);
                            return true;
                        }
                        return false;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed creating new item pool:\n" + ex, "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddAttributePoolClick(object sender, EventArgs e)
        {
            if (_attributePoolDialog.ShowDialog(this) == DialogResult.OK)
            {
                var name = _attributePoolDialog.AttributePoolName;

                try
                {
                    // Create a new instance.
                    var instance = new AttributePool {Name = name};

                    // Register it.
                    AttributePoolManager.Add(instance);

                    // And select it.
                    SelectAttributePool(instance);

                    // Add undo command.
                    PushUndo("add attribute pool", () =>
                    {
                        if (MessageBox.Show(this,
                                            "Are you sure you wish to delete the attribute pool '" + instance.Name + "'?",
                                            "Confirmation", MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            AttributePoolManager.Remove(instance);
                            return true;
                        }
                        return false;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed creating new attribute pool:\n" + ex, "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    if ((ModifierKeys & Keys.Shift) != 0 ||
                        MessageBox.Show(this,
                                        "Are you sure you wish to delete the factory '" + factory.Name + "'?",
                                        "Confirmation", MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        FactoryManager.Remove(factory);

                        // Add undo command.
                        PushUndo("remove factory", () =>
                        {
                            FactoryManager.Add(factory);
                            return true;
                        });
                    }
                }
                else if (pgProperties.SelectedObject is ItemPool)
                {
                    var itemPool = (ItemPool)pgProperties.SelectedObject;
                    if ((ModifierKeys & Keys.Shift) != 0 ||
                        MessageBox.Show(this,
                                        "Are you sure you wish to delete the item pool '" + itemPool.Name + "'?",
                                        "Confirmation", MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        ItemPoolManager.Remove(itemPool);

                        // Add undo command.
                        PushUndo("remove item pool", () =>
                        {
                            ItemPoolManager.Add(itemPool);
                            return true;
                        });
                    }
                }
                else if (pgProperties.SelectedObject is AttributePool)
                {
                    var attributePool = (AttributePool)pgProperties.SelectedObject;
                    if ((ModifierKeys & Keys.Shift) != 0 ||
                        MessageBox.Show(this,
                                        "Are you sure you wish to delete the attribute pool '" + attributePool.Name + "'?",
                                        "Confirmation", MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        AttributePoolManager.Remove(attributePool);

                        // Add undo command.
                        PushUndo("remove attribute pool", () =>
                        {
                            AttributePoolManager.Add(attributePool);
                            return true;
                        });
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
            // Prepare for undo command. Remember currently selected object.
            var selectedObject = pgProperties.SelectedObject;
            Debug.Assert(selectedObject != null);

            // Get path to changed property, bottom up. We have to do this before
            // name change handling, as that can trigger removal of tree nodes, and
            // thus a rebuilding of the property grid, leading to object disposed
            // exceptions when trying to build the path.
            string fullPath = null;
            object instance = null;

            // Move up until we hit the root.
            {
                var gridItem = e.ChangedItem;
                while (gridItem != null)
                {
                    // Ignore categories as they're purely display related.
                    if (gridItem.GridItemType != GridItemType.Category)
                    {
                        // If this is the first property that's not the selected one we
                        // want to mark it as the instance of which the changed property
                        // is a member.
                        if (instance == null && !string.IsNullOrWhiteSpace(fullPath))
                        {
                            instance = gridItem.Value;
                        }

                        if (gridItem.PropertyDescriptor != null)
                        {
                            // Expand the path, in a compatible format to our SelectProperty function.
                            fullPath = gridItem.PropertyDescriptor.Name +
                                       (string.IsNullOrWhiteSpace(fullPath) ? "" : ("." + fullPath));
                        }
                    }

                    // Continue with parent.
                    gridItem = gridItem.Parent;
                }
                Debug.Assert(fullPath != null);
                fullPath = fullPath.Replace(".[", "[");
            }

            // Handle possible object name changes.
            if (!HandleObjectNameChanged(e.OldValue, e.ChangedItem.Value, e.ChangedItem))
            {
                // Rename failed, bail.
                return;
            }

            // Add the undo command.
            PushUndo("edit property", () =>
            {
                // Select the changed object again.
                if (selectedObject is IFactory)
                {
                    SelectFactory(selectedObject as IFactory);
                }
                else if (selectedObject is ItemPool)
                {
                    SelectItemPool(selectedObject as ItemPool);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported type.");
                }

                // Select our property.
                SelectProperty(fullPath);

                Debug.Assert(pgProperties.SelectedGridItem != null);
                Debug.Assert(pgProperties.SelectedGridItem.PropertyDescriptor != null);

                // Remember for name change handler.
                var newValue = pgProperties.SelectedGridItem.Value;

                // And change back the value.
                pgProperties.SelectedGridItem.PropertyDescriptor.SetValue(instance, e.OldValue);

                // Handle possible object name changes.
                HandleObjectNameChanged(newValue, e.OldValue, pgProperties.SelectedGridItem);

                // Refresh the complete grid (sometimes parent cells would not update
                // properly, otherwise... probably some wrong editor implementation)
                pgProperties.Refresh();

                // Update our preview.
                UpdatePreview(true);

                return true;
            });

            // Refresh the complete grid (sometimes parent cells would not update
            // properly, otherwise... probably some wrong editor implementation)
            pgProperties.Refresh();

            // Update our preview.
            UpdatePreview(true);
        }

        private bool HandleObjectNameChanged(object oldValue, object newValue, GridItem changedItem)
        {
            // See if what we changed is the name of the selected object.
            if (ReferenceEquals(changedItem.PropertyDescriptor, TypeDescriptor.GetProperties(pgProperties.SelectedObject)["Name"]))
            {
                // Yes, get old and new value.
                var oldName = oldValue as string;
                var newName = newValue as string;

                // Adjust manager layout, this will throw as necessary.
                tvData.BeginUpdate();
                try
                {
                    if (pgProperties.SelectedObject is IFactory)
                    {
                        FactoryManager.Rename(oldName, newName);
                    }
                    else if (pgProperties.SelectedObject is ItemPool)
                    {
                        ItemPoolManager.Rename(oldName, newName);
                    }
                    else if (pgProperties.SelectedObject is AttributePool)
                    {
                        AttributePoolManager.Rename(oldName, newName);
                    }
                }
                catch (ArgumentException ex)
                {
                    // Revert to old name.
                    if (pgProperties.SelectedObject is IFactory)
                    {
                        ((IFactory)pgProperties.SelectedObject).Name = oldName;
                    }
                    else if (pgProperties.SelectedObject is ItemPool)
                    {
                        ((ItemPool)pgProperties.SelectedObject).Name = oldName;
                    }
                    else if (pgProperties.SelectedObject is AttributePool)
                    {
                        ((AttributePool)pgProperties.SelectedObject).Name = oldName;
                    }

                    // Tell the user why.
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                finally
                {
                    // Stop updating the tree.
                    tvData.EndUpdate();
                }

                if (pgProperties.SelectedObject is IFactory)
                {
                    SelectFactory((IFactory)pgProperties.SelectedObject);
                }
                else if (pgProperties.SelectedObject is ItemPool)
                {
                    SelectItemPool((ItemPool)pgProperties.SelectedObject);
                }
                else if (pgProperties.SelectedObject is AttributePool)
                {
                    SelectAttributePool((AttributePool)pgProperties.SelectedObject);
                }

                SelectProperty("Name");
            }
            else
            {
                // Rescan for issues related to this property.
                if (changedItem.PropertyDescriptor != null &&
                    changedItem.PropertyDescriptor.Attributes[typeof(TriggersFullValidationAttribute)] == null)
                {
                    if (pgProperties.SelectedObject is IFactory)
                    {
                        ScanForIssues((IFactory)pgProperties.SelectedObject);
                    }
                    else if (pgProperties.SelectedObject is ItemPool)
                    {
                        ScanForIssues((ItemPool)pgProperties.SelectedObject);
                    }

                    // Done, avoid the full rescan.
                    return true;
                }
            }

            // Do a full scan when we come here.
            ScanForIssues();
            return true;
        }

        private void IssuesDoubleClick(object sender, EventArgs e)
        {
            if (lvIssues.SelectedItems.Count == 0)
            {
                return;
            }
            // Try to select the object.
            var target = lvIssues.SelectedItems[0].Tag;
            if (!SelectFactory(target as IFactory) && !SelectItemPool(target as ItemPool))
            {
                return;
            }

            // Navigate to that property.
            if (SelectProperty(lvIssues.SelectedItems[0].SubItems[3].Text))
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
        /// Sorter for issue list.
        /// </summary>
        private sealed class IssueComparer : IComparer<ListViewItem>, IComparer
        {
            public int Compare(ListViewItem x, ListViewItem y)
            {
                var xMessage = x.SubItems.Count > 1 ? x.SubItems[1].Text : null;
                var xFactory = x.SubItems.Count > 2 ? x.SubItems[2].Text : null;
                var xProperty = x.SubItems.Count > 3 ? x.SubItems[3].Text : null;

                var yMessage = y.SubItems.Count > 1 ? y.SubItems[1].Text : null;
                var yFactory = y.SubItems.Count > 2 ? y.SubItems[2].Text : null;
                var yProperty = y.SubItems.Count > 3 ? y.SubItems[3].Text : null;

                if (!string.IsNullOrWhiteSpace(xFactory) && !string.IsNullOrWhiteSpace(yFactory) && !string.Equals(xFactory, yFactory))
                {
                    // Sort by factory.
                    return string.CompareOrdinal(xFactory, yFactory);
                }
                if (!string.IsNullOrWhiteSpace(xProperty) && !string.IsNullOrWhiteSpace(yProperty) && !string.Equals(xProperty, yProperty))
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

        private void UndoClick(object sender, EventArgs e)
        {
            PopUndo();
        }

        public void PushUndo(string menuTitle, Func<bool> action)
        {
            _undoCommands.Push(Tuple.Create(menuTitle, action));
            ++_changesSinceLastSave;
            UpdateUndoMenu();
        }

        public void PopUndo()
        {
            if (_undoCommands.Count < 1)
            {
                return;
            }
            var command = _undoCommands.Pop();
            if (command.Item2())
            {
                // Undo was successful.
                --_changesSinceLastSave;
                UpdateUndoMenu();
            }
            else
            {
                // Undo was canceled (e.g. confirmation declined by user).
                _undoCommands.Push(command);
            }
        }

        private void ClearUndo()
        {
            _undoCommands.Clear();
            UpdateUndoMenu();
        }

        private void UpdateUndoMenu()
        {
            if (_undoCommands.Count > 0)
            {
                undoToolStripMenuItem.Text = "&Undo " + _undoCommands.Peek().Item1;
                undoToolStripMenuItem.Enabled = true;
            }
            else
            {
                undoToolStripMenuItem.Text = "&Undo...";
                undoToolStripMenuItem.Enabled = false;
            }

            if (_changesSinceLastSave != 0)
            {
                Text = @"Space - Data Editor (*)";
            } else {
                Text = @"Space - Data Editor";
            }
        }
    }
}
