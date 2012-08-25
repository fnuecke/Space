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
        public string SelectedAsset { get; set; }

        public TextureAssetDialog()
        {
            InitializeComponent();
        }

        private void TextureAssetDialogLoad(object sender, EventArgs e)
        {
            tvTextures.BeginUpdate();
            tvTextures.Nodes.Clear();
            tvTextures.Nodes.Add("", "None");
            foreach (var assetName in ContentProjectManager.TextureAssetNames)
            {
                // Generate the path through the tree we need to take, to
                // insert this asset name as its own node.
                var hierarchy = assetName.Split(new[] { tvTextures.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
                var nodes = tvTextures.Nodes;
                for (var i = 0; i < hierarchy.Length; i++)
                {
                    // Create entry for this hierarchy level is necessary.
                    if (!nodes.ContainsKey(hierarchy[i]))
                    {
                        nodes.Add(hierarchy[i], hierarchy[i]);
                    }
                    // Continue with this node's child nodes as the next insertion point.
                    nodes = nodes[hierarchy[i]].Nodes;
                }
            }
            tvTextures.EndUpdate();

            // Try re-selecting. We do this in an extra iteration to select as far down the
            // tree as possible.
            tvTextures.SelectedNode = null;
            if (!string.IsNullOrWhiteSpace(SelectedAsset))
            {
                var hierarchy = SelectedAsset.Replace('\\', '/').
                    Split(new[] {tvTextures.PathSeparator}, StringSplitOptions.RemoveEmptyEntries);
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
            else
            {
                // Clear preview.
                var oldImage = pbPreview.Image;
                pbPreview.Image = null;
                if (oldImage != null)
                {
                    oldImage.Dispose();
                }
            }
        }

        private void TexturesAfterSelect(object sender, TreeViewEventArgs e)
        {
            // Disable OK button until we have a valid image.
            btnOK.Enabled = false;
            SelectedAsset = null;

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

            // See if the "none" entry is selected.
            if (e.Node.Name.Equals(""))
            {
                btnOK.Enabled = true;
                return;
            }

            // See if the asset is valid (full path).
            var assetPath = ContentProjectManager.GetTexturePath(e.Node.FullPath);
            if (assetPath == null)
            {
                return;
            }

            // OK, try to load it.
            try
            {
                pbPreview.Image = Image.FromFile(assetPath);
                btnOK.Enabled = true;
                SelectedAsset = e.Node.FullPath;
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
