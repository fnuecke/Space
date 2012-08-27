namespace Space.Tools.DataEditor
{
    sealed partial class ItemPoolDialog
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
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbItems = new System.Windows.Forms.GroupBox();
            this.tvItems = new System.Windows.Forms.TreeView();
            this.gbInfo = new System.Windows.Forms.GroupBox();
            this.pgPreview = new System.Windows.Forms.PropertyGrid();
            this.scMain = new System.Windows.Forms.SplitContainer();
            this.gbItems.SuspendLayout();
            this.gbInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).BeginInit();
            this.scMain.Panel1.SuspendLayout();
            this.scMain.Panel2.SuspendLayout();
            this.scMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(456, 427);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "&OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.OkClick);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(537, 427);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.CancelClick);
            // 
            // gbItems
            // 
            this.gbItems.Controls.Add(this.tvItems);
            this.gbItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbItems.Location = new System.Drawing.Point(5, 5);
            this.gbItems.Name = "gbItems";
            this.gbItems.Padding = new System.Windows.Forms.Padding(8);
            this.gbItems.Size = new System.Drawing.Size(238, 399);
            this.gbItems.TabIndex = 4;
            this.gbItems.TabStop = false;
            this.gbItems.Text = "Items";
            // 
            // tvItems
            // 
            this.tvItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvItems.HideSelection = false;
            this.tvItems.HotTracking = true;
            this.tvItems.Location = new System.Drawing.Point(8, 21);
            this.tvItems.Name = "tvItems";
            this.tvItems.Size = new System.Drawing.Size(222, 370);
            this.tvItems.TabIndex = 2;
            this.tvItems.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ItemsAfterSelect);
            this.tvItems.DoubleClick += new System.EventHandler(this.ItemsDoubleClick);
            // 
            // gbInfo
            // 
            this.gbInfo.Controls.Add(this.pgPreview);
            this.gbInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbInfo.Location = new System.Drawing.Point(5, 5);
            this.gbInfo.Name = "gbInfo";
            this.gbInfo.Padding = new System.Windows.Forms.Padding(8);
            this.gbInfo.Size = new System.Drawing.Size(338, 399);
            this.gbInfo.TabIndex = 5;
            this.gbInfo.TabStop = false;
            this.gbInfo.Text = "Info";
            // 
            // pgPreview
            // 
            this.pgPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgPreview.Location = new System.Drawing.Point(8, 21);
            this.pgPreview.Name = "pgPreview";
            this.pgPreview.Size = new System.Drawing.Size(322, 370);
            this.pgPreview.TabIndex = 0;
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
            this.scMain.Panel1.Controls.Add(this.gbItems);
            this.scMain.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // scMain.Panel2
            // 
            this.scMain.Panel2.Controls.Add(this.gbInfo);
            this.scMain.Panel2.Padding = new System.Windows.Forms.Padding(5);
            this.scMain.Size = new System.Drawing.Size(600, 409);
            this.scMain.SplitterDistance = 248;
            this.scMain.TabIndex = 6;
            // 
            // ItemInfoDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(624, 462);
            this.Controls.Add(this.scMain);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(300, 400);
            this.Name = "ItemInfoDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Item";
            this.Load += new System.EventHandler(this.ItemInfoDialogLoad);
            this.gbItems.ResumeLayout(false);
            this.gbInfo.ResumeLayout(false);
            this.scMain.Panel1.ResumeLayout(false);
            this.scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).EndInit();
            this.scMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox gbItems;
        private System.Windows.Forms.TreeView tvItems;
        private System.Windows.Forms.GroupBox gbInfo;
        private System.Windows.Forms.SplitContainer scMain;
        private System.Windows.Forms.PropertyGrid pgPreview;
    }
}