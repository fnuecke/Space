using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Space.ComponentSystem.Factories;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Dialog to select items valid for a slot.
    /// </summary>
    public partial class ItemInfoDialog : Form
    {
        /// <summary>
        /// Gets or sets the types of the items allowed for selection.
        /// </summary>
        public IEnumerable<ItemFactory.ItemSlotInfo.ItemType> AllowedItemTypes { get; set; }

        /// <summary>
        /// The currently selected item name.
        /// </summary>
        public string SelectedItemName { get; set; }

        public ItemInfoDialog()
        {
            InitializeComponent();

            AllowedItemTypes = (ItemFactory.ItemSlotInfo.ItemType[])Enum.GetValues(typeof(ItemFactory.ItemSlotInfo.ItemType));
        }

        private void ItemInfoDialogLoad(object sender, EventArgs e)
        {
            tvItems.BeginUpdate();
            tvItems.Nodes.Clear();
            if (AllowedItemTypes != null)
            {
                foreach (var type in AllowedItemTypes)
                {
                    foreach (var factory in FactoryManager.GetAllItemsOfType(type))
                    {
                        var typeName = factory.GetType().Name;
                        if (!tvItems.Nodes.ContainsKey(typeName))
                        {
                            var cleanTypeName = typeName;
                            if (typeName.EndsWith("Factory"))
                            {
                                cleanTypeName = typeName.Substring(0, typeName.Length - "Factory".Length);
                            }
                            tvItems.Nodes.Add(typeName, cleanTypeName);
                        }
                        tvItems.Nodes[typeName].Nodes.Add(factory.Name, factory.Name);
                    }
                }
            }
            tvItems.EndUpdate();

            var select = tvItems.Nodes.Find(SelectedItemName, true);
            if (select.Length > 0)
            {
                tvItems.SelectedNode = select[0];
            }
            else
            {
                tvItems.SelectedNode = null;
            }
        }

        private void ItemsAfterSelect(object sender, TreeViewEventArgs e)
        {
            // Disable OK button until we have a valid image.
            btnOK.Enabled = false;
            SelectedItemName = null;

            // Do we have something new?
            if (e.Node == null)
            {
                return;
            }

            // See if the item is valid.
            if (FactoryManager.GetFactory(e.Node.Name) == null)
            {
                return;
            }

            // OK, allow selecting it.
            btnOK.Enabled = true;
            SelectedItemName = e.Node.Name;
        }

        private void OkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void CancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void ItemsDoubleClick(object sender, EventArgs e)
        {
            if (btnOK.Enabled)
            {
                DialogResult = DialogResult.OK;
            }
        }
    }
}
