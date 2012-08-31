using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Space.Tools.DataEditor
{
    class ItemPoolChooserEditor : UITypeEditor
    {
        private readonly ItemPoolDialog _dialog = new ItemPoolDialog();

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context,
                                         System.IServiceProvider provider, object value)
        {
            var svc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if ((value is string || value == null) && svc != null)
            {
                value = value ?? string.Empty;

                // Preselect old entry.
                _dialog.SelectedItemName = (string)value;
                // Restrict selection.
                //var giContext = context as GridItem; //TODO check if we DO need this
                //if (giContext == null ||
                //    giContext.Parent == null ||
                //    giContext.Parent.Parent == null ||
                //    giContext.Parent.Parent.Parent == null ||
                //    giContext.Parent.Parent.Parent.Value == null) 
                //{
                //    MessageBox.Show("Cannot edit heres.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //    return value;
                //}


                if (svc.ShowDialog(_dialog) == DialogResult.OK)
                {
                    return _dialog.SelectedItemName;
                }
            }
            return base.EditValue(context, provider, value);
        }
    }
}
