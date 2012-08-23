using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// The currently selected item name.
        /// </summary>
        public string SelectedItemName { get; set; }

        /// <summary>
        /// Gets or sets the available slots. Based on these the available items will
        /// be filtered, to only contain items that can be put in one of these slots.
        /// </summary>
        public IEnumerable<ItemFactory.ItemSlotInfo> AvailableSlots { get; set; }

        public ItemInfoDialog()
        {
            InitializeComponent();

            pgPreview.PropertyValueChanged += PropertyValueChanged;
        }

        private void PropertyValueChanged(object o, PropertyValueChangedEventArgs args)
        {
            Debug.Assert(args.ChangedItem.PropertyDescriptor != null);
            var parent = args.ChangedItem.Parent;
            while (parent.Value == null && parent.Parent != null)
            {
                parent = parent.Parent;
            }
            args.ChangedItem.PropertyDescriptor.SetValue(parent.Value, args.OldValue);
        }

        private void ItemInfoDialogLoad(object sender, EventArgs e)
        {
            tvItems.BeginUpdate();
            tvItems.Nodes.Clear();

            tvItems.Nodes.Add("", "None");

            if (AvailableSlots != null)
            {
                foreach (var factory in FactoryManager.GetAllItems())
                {
                    if (!IsSlotAvailable(factory))
                    {
                        continue;
                    }
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

        /// <summary>
        /// Determines whether there is any slot available that can hold the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if a slot is available; otherwise, <c>false</c>.
        /// </returns>
        private bool IsSlotAvailable(ItemFactory item)
        {
            foreach (var slot in AvailableSlots)
            {
                if (slot.Type == item.GetType().ToItemType() &&
                    slot.Size >= item.RequiredSlotSize)
                {
                    return true;
                }
            }
            return false;
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

            // See if the item is valid.
            var factory = FactoryManager.GetFactory(e.Node.Name) as ItemFactory;
            if (factory == null && !e.Node.Name.Equals(""))
            {
                return;
            }

            // OK, allow selecting it.
            btnOK.Enabled = true;
            SelectedItemName = e.Node.Name;
            pgPreview.SelectedObject = factory;
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
