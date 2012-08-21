using System.Collections.Generic;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Space.ComponentSystem.Factories;
using Space.Data;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Item info editor that opens a dialog with known items, valid for the edited slot.
    /// </summary>
    public sealed class ItemInfoEditor : UITypeEditor
    {
        private readonly ItemInfoDialog _dialog = new ItemInfoDialog();

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value)
        {
            var svc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if ((value is string || value == null) && svc != null)
            {
                value = value ?? string.Empty;

                // Preselect old entry.
                _dialog.SelectedItemName = (string)value;

                // Restrict selection.
                var giContext = context as GridItem;
                if (giContext == null ||
                    giContext.Parent == null ||
                    giContext.Parent.Parent == null ||
                    giContext.Parent.Parent.Parent == null ||
                    giContext.Parent.Parent.Parent.Value == null)
                {
                    return value;
                }
                if (giContext.Parent.Parent.Parent.Value is ShipFactory)
                {
                    // Top level item, only allow fuselage.
                    _dialog.AvailableSlots = new[] { new ItemFactory.ItemSlotInfo{Size = ItemSlotSize.Huge, Type = ItemFactory.ItemSlotInfo.ItemType.Fuselage} };
                }
                else
                {
                    // Somewhere in the tree, get parent.
                    var containingItem = giContext.Parent.Parent.Parent.Value as ShipFactory.ItemInfo;
                    if (containingItem == null || string.IsNullOrWhiteSpace(containingItem.Name))
                    {
                        // Null parent, don't allow adding items.
                        return value;
                    }
                    var containingFactory = FactoryManager.GetFactory(containingItem.Name) as ItemFactory;
                    if (containingFactory == null)
                    {
                        // Invalid parent, don't allow adding items.
                        return value;
                    }
                    // Figure out available slots.
                    var availableSlots = new List<ItemFactory.ItemSlotInfo>();
                    if (containingFactory.Slots != null)
                    {
                        availableSlots.AddRange(containingFactory.Slots);
                    }
                    else
                    {
                        // Item has no slots, don't allow adding items.
                        return value;
                    }
                    // Remove used ones.
                    foreach (var slot in containingItem.Slots)
                    {
                        // Skip self.
                        if (slot == context.Instance)
                        {
                            continue;
                        }

                        // Skip empty ones.
                        if (string.IsNullOrWhiteSpace(slot.Name))
                        {
                            continue;
                        }

                        // Get the item of that type.
                        var slotItemFactory = FactoryManager.GetFactory(slot.Name) as ItemFactory;
                        if (slotItemFactory == null)
                        {
                            continue;
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
                        if (bestSlot != null)
                        {
                            availableSlots.Remove(bestSlot);
                        }
                    }
                    // Skip if no slots remain.
                    if (availableSlots.Count == 0)
                    {
                        return value;
                    }
                    // Set remaining slots.
                    _dialog.AvailableSlots = availableSlots;
                }

                if (svc.ShowDialog(_dialog) == DialogResult.OK)
                {
                    return _dialog.SelectedItemName;
                }
            }
            return base.EditValue(context, provider, value);
        }
    }
}
