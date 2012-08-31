using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Dialog to select items valid for a slot.
    /// </summary>
    public sealed partial class ItemPoolDialog : Form
    {
        /// <summary>
        /// The currently selected item name.
        /// </summary>
        public string SelectedItemName { get; set; }

        public ItemPoolDialog()
        {
            InitializeComponent();

            pgPreview.PropertyValueChanged += PropertyValueChanged;
        }

        private void PropertyValueChanged(object o, PropertyValueChangedEventArgs args)
        {
            Debug.Assert(args.ChangedItem.PropertyDescriptor != null);
            var parent = args.ChangedItem.Parent;
            while (parent.GridItemType != GridItemType.Property && parent.Parent != null)
            {
                parent = parent.Parent;
            }
            Debug.Assert(parent != null);
            args.ChangedItem.PropertyDescriptor.SetValue(parent.Value, args.OldValue);
        }

        private void ItemInfoDialogLoad(object sender, EventArgs e)
        {
            tvItems.BeginUpdate();
            tvItems.Nodes.Clear();

            tvItems.Nodes.Add("", "None");

            foreach (var itemPool in ItemPoolManager.GetItemPools())
            {
                    
                var typeName = itemPool.GetType().Name;
                if (!tvItems.Nodes.ContainsKey(typeName))
                {
                    var cleanTypeName = typeName;
                    if (typeName.EndsWith("Factory"))
                    {
                        cleanTypeName = typeName.Substring(0, typeName.Length - "Factory".Length);
                    }
                    tvItems.Nodes.Add(typeName, cleanTypeName);
                }
                tvItems.Nodes[typeName].Nodes.Add(itemPool.Name, itemPool.Name);
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

            // Close directly if there are no items we can select.
            if (tvItems.Nodes.Count == 1)
            {
                MessageBox.Show("No slots remaining, or no items known that would fit the slots.", "Notice",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                DialogResult = DialogResult.Cancel;
            }
        }

        private void ItemsAfterSelect(object sender, TreeViewEventArgs e)
        {
            // Disable OK button until we have a valid image.
            btnOK.Enabled = false;
            SelectedItemName = null;
            pgPreview.SelectedObject = null;

            // Do we have something new?
            if (e.Node == null)
            {
                return;
            }

            // See if the "none" entry is selected.
            if (e.Node.Name.Equals(""))
            {
                btnOK.Enabled = true;
                return;
            }

            // See if the item is valid.
            var itemPool = ItemPoolManager.GetItemPool(e.Node.Name) ;
            if (itemPool == null)
            {
                return;
            }

            // OK, allow selecting it.
            btnOK.Enabled = true;
            SelectedItemName = e.Node.Name;
            pgPreview.SelectedObject = itemPool;
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
