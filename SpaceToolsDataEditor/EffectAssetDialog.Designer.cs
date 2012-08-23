namespace Space.Tools.DataEditor
{
    sealed partial class EffectAssetDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.tvEffects = new System.Windows.Forms.TreeView();
            this.scMain = new System.Windows.Forms.SplitContainer();
            this.gbEffects = new System.Windows.Forms.GroupBox();
            this.gbPreview = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).BeginInit();
            this.scMain.Panel1.SuspendLayout();
            this.scMain.Panel2.SuspendLayout();
            this.scMain.SuspendLayout();
            this.gbEffects.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(537, 407);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.CancelClick);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(456, 407);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "&OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.OkClick);
            // 
            // tvEffects
            // 
            this.tvEffects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvEffects.HideSelection = false;
            this.tvEffects.HotTracking = true;
            this.tvEffects.Location = new System.Drawing.Point(8, 21);
            this.tvEffects.Name = "tvEffects";
            this.tvEffects.PathSeparator = "/";
            this.tvEffects.Size = new System.Drawing.Size(174, 350);
            this.tvEffects.TabIndex = 2;
            this.tvEffects.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.EffectsAfterSelect);
            this.tvEffects.DoubleClick += new System.EventHandler(this.EffectsDoubleClick);
            // 
            // scMain
            // 
            this.scMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scMain.Location = new System.Drawing.Point(12, 12);
            this.scMain.Name = "scMain";
            // 
            // scMain.Panel1
            // 
            this.scMain.Panel1.Controls.Add(this.gbEffects);
            this.scMain.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // scMain.Panel2
            // 
            this.scMain.Panel2.Controls.Add(this.gbPreview);
            this.scMain.Panel2.Padding = new System.Windows.Forms.Padding(5);
            this.scMain.Size = new System.Drawing.Size(600, 389);
            this.scMain.SplitterDistance = 200;
            this.scMain.TabIndex = 4;
            // 
            // gbEffects
            // 
            this.gbEffects.Controls.Add(this.tvEffects);
            this.gbEffects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbEffects.Location = new System.Drawing.Point(5, 5);
            this.gbEffects.Name = "gbEffects";
            this.gbEffects.Padding = new System.Windows.Forms.Padding(8);
            this.gbEffects.Size = new System.Drawing.Size(190, 379);
            this.gbEffects.TabIndex = 0;
            this.gbEffects.TabStop = false;
            this.gbEffects.Text = "Effects";
            // 
            // gbPreview
            // 
            this.gbPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbPreview.Location = new System.Drawing.Point(5, 5);
            this.gbPreview.Name = "gbPreview";
            this.gbPreview.Padding = new System.Windows.Forms.Padding(8);
            this.gbPreview.Size = new System.Drawing.Size(386, 379);
            this.gbPreview.TabIndex = 0;
            this.gbPreview.TabStop = false;
            this.gbPreview.Text = "Preview";
            // 
            // EffectAssetDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(624, 442);
            this.Controls.Add(this.scMain);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "EffectAssetDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Image";
            this.Load += new System.EventHandler(this.EffectAssetDialogLoad);
            this.scMain.Panel1.ResumeLayout(false);
            this.scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).EndInit();
            this.scMain.ResumeLayout(false);
            this.gbEffects.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TreeView tvEffects;
        private System.Windows.Forms.SplitContainer scMain;
        private System.Windows.Forms.GroupBox gbEffects;
        private System.Windows.Forms.GroupBox gbPreview;
    }
}