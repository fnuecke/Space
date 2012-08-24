using System;
using System.Windows.Forms;

namespace Space.Tools.DataEditor
{
    /// <summary>
    /// Dialog to select textures from known pool.
    /// </summary>
    public sealed partial class EffectAssetDialog : Form
    {
        /// <summary>
        /// Gets or sets the selected asset.
        /// </summary>
        public string SelectedAsset { get; set; }

        /// <summary>
        /// The preview control used to render the particle effect preview.
        /// </summary>
        private readonly EffectPreviewControl _preview;

        public EffectAssetDialog()
        {
            InitializeComponent();

            gbPreview.Controls.Add(_preview = new EffectPreviewControl { Dock = DockStyle.Fill });
        }

        private void EffectAssetDialogLoad(object sender, EventArgs e)
        {
            tvEffects.BeginUpdate();
            tvEffects.Nodes.Clear();
            foreach (var assetName in ContentProjectManager.EffectAssetNames)
            {
                // Generate the path through the tree we need to take, to
                // insert this asset name as its own node.
                var hierarchy = assetName.Split(new[] { tvEffects.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
                var nodes = tvEffects.Nodes;
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
            tvEffects.EndUpdate();

            // Try re-selecting. We do this in an extra iteration to select as far down the
            // tree as possible.
            tvEffects.SelectedNode = null;
            if (!string.IsNullOrWhiteSpace(SelectedAsset))
            {
                var hierarchy = SelectedAsset.Replace('\\', '/').
                    Split(new[] {tvEffects.PathSeparator}, StringSplitOptions.RemoveEmptyEntries);
                var nodes = tvEffects.Nodes;
                for (var i = 0; i < hierarchy.Length; i++)
                {
                    if (!nodes.ContainsKey(hierarchy[i]))
                    {
                        // No such entry known in currently loaded content projects...
                        return;
                    }
                    tvEffects.SelectedNode = nodes[hierarchy[i]];
                    nodes = nodes[hierarchy[i]].Nodes;
                }
            }
            if (tvEffects.SelectedNode != null)
            {
                _preview.Effect = tvEffects.SelectedNode.FullPath;
            }
        }

        private void EffectsAfterSelect(object sender, TreeViewEventArgs e)
        {
            // Disable OK button until we have a valid image.
            btnOK.Enabled = false;
            SelectedAsset = null;

            // Clear preview.
            _preview.Effect = null;

            // Do we have something new?
            if (e.Node == null)
            {
                return;
            }

            // See if the asset is valid (full path).
            var assetPath = ContentProjectManager.GetEffectPath(e.Node.FullPath);
            if (assetPath == null)
            {
                return;
            }

            // OK, try to load it.
            _preview.Effect = e.Node.FullPath;
            btnOK.Enabled = true;
            SelectedAsset = e.Node.FullPath;
        }

        private void OkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void CancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void EffectsDoubleClick(object sender, EventArgs e)
        {
            if (btnOK.Enabled)
            {
                DialogResult = DialogResult.OK;
            }
        }
    }
}
