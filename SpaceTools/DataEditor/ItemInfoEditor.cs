using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

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
                _dialog.SelectedItemName = (string)value ?? string.Empty;
                if (svc.ShowDialog(_dialog) == DialogResult.OK)
                {
                    return _dialog.SelectedItemName;
                }
            }
            return base.EditValue(context, provider, value);
        }
    }
}
