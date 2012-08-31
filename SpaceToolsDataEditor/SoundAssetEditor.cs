using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Editor for texture assets, opening a form that show known textures.
    /// </summary>
    public sealed class SoundAssetEditor : UITypeEditor
    {
        private readonly SoundAssetDialog _dialog = new SoundAssetDialog();

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value)
        {
            var svc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (svc != null)
            {
                _dialog.SelectedAsset = value as string;
                if (svc.ShowDialog(_dialog) == DialogResult.OK)
                {
                    return _dialog.SelectedAsset;
                }
            }
            return base.EditValue(context, provider, value);
        }
    }
}
