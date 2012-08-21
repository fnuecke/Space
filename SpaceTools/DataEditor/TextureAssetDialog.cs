using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Dialog to select textures from known pool.
    /// </summary>
    public sealed partial class TextureAssetDialog : Form
    {
        /// <summary>
        /// Gets or sets the selected asset.
        /// </summary>
        /// <value>
        /// The selected asset.
        /// </value>
        public string SelectedAsset
        {
            get { return _selectedAsset; }
            set
            {
                _selectedAsset = value;

                tvTextures.SelectedNode = null;
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }
                var hierarchy = value.Replace('/', '\\').Split(new[] {tvTextures.PathSeparator}, StringSplitOptions.RemoveEmptyEntries);
                var nodes = tvTextures.Nodes;
                for (var i = 0; i < hierarchy.Length; i++)
                {
                    if (!nodes.ContainsKey(hierarchy[i]))
                    {
                        // No such entry known in currently loaded content projects...
                        return;
                    }
                    tvTextures.SelectedNode = nodes[hierarchy[i]];
                    nodes = nodes[hierarchy[i]].Nodes;
                }
            }
        }

        private string _selectedAsset;

        public TextureAssetDialog()
        {
            InitializeComponent();
        }

        private void TextureAssetDialogLoad(object sender, EventArgs e)
        {
            tvTextures.BeginUpdate();
            tvTextures.Nodes.Clear();
            foreach (var assetName in ContentProjectManager.TextureAssetNames)
            {
                var hierarchy = assetName.Split(new[] {tvTextures.PathSeparator}, StringSplitOptions.RemoveEmptyEntries);
                var nodes = tvTextures.Nodes;
                for (var i = 0; i < hierarchy.Length; i++)
                {
                    if (!nodes.ContainsKey(hierarchy[i]))
                    {
                        nodes.Add(hierarchy[i], hierarchy[i]);
                    }
                    nodes = nodes[hierarchy[i]].Nodes;
                }
            }
            tvTextures.EndUpdate();

            // Try re-selecting.
            SelectedAsset = _selectedAsset;
        }

        private void TexturesAfterSelect(object sender, TreeViewEventArgs e)
        {
            // Disable OK button until we have a valid image.
            btnOK.Enabled = false;
            _selectedAsset = null;

            // Clear preview.
            var oldImage = pbPreview.Image;
            pbPreview.Image = null;
            if (oldImage != null)
            {
                oldImage.Dispose();
            }

            // Do we have something new?
            if (e.Node == null)
            {
                return;
            }

            // See if the asset is valid (full path).
            var assetPath = ContentProjectManager.GetFileForTextureAsset(e.Node.FullPath);
            if (assetPath == null)
            {
                return;
            }

            // OK, try to load it.
            try
            {
                pbPreview.Image = Image.FromFile(assetPath);
                btnOK.Enabled = true;
                _selectedAsset = e.Node.FullPath;
            }
            catch (FileNotFoundException)
            {
            }
        }

        private void OkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void CancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void TexturesDoubleClick(object sender, EventArgs e)
        {
            if (btnOK.Enabled)
            {
                DialogResult = DialogResult.OK;
            }
        }
    }
}
