using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Space.Tools.DataEditor
{
    class AttributePoolChooserEditor : UITypeEditor
    {
        private readonly AttributePoolDialog _dialog = new AttributePoolDialog();

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
                _dialog.SelectedAtributeName = (string)value;
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
                    return _dialog.SelectedAtributeName;
                }
            }
            return base.EditValue(context, provider, value);
        }
    }
}
