using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Space.ComponentSystem.Factories;
using Space.Data;

namespace Space.Tools.DataEditor
{
    public sealed class ItemPoolEditor : UITypeEditor
    {
        private readonly ItemInfoDialog _dialog = new ItemInfoDialog();

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context,
                                         System.IServiceProvider provider, object value)
        {
            var svc = provider.GetService(typeof (IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if ((value is string || value == null) && svc != null)
            {
                value = value ?? string.Empty;

                // Preselect old entry.
                _dialog.SelectedItemName = (string) value;
                _dialog.AllItems = true;
                // Restrict selection.
                var giContext = context as GridItem;
                //if ((giContext == null ||
                //    giContext.Parent == null ||
                //    giContext.Parent.Parent == null ||
                //    giContext.Parent.Parent.Parent == null ||
                //    giContext.Parent.Parent.Parent.Value == null)&&
                //    (giContext.Parent.Parent.Parent.Parent == null
                //    ||!(giContext.Parent.Parent.Parent.Parent.Value is ItemPool)))
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
