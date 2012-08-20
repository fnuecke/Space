namespace Space.Tools.DataEditor
{
    sealed partial class DataEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataEditor));
            this.pgProperties = new System.Windows.Forms.PropertyGrid();
            this.tvData = new System.Windows.Forms.TreeView();
            this.gbData = new System.Windows.Forms.GroupBox();
            this.msMain = new System.Windows.Forms.MenuStrip();
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSave = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiLoad = new System.Windows.Forms.ToolStripMenuItem();
            this.separator0 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMain = new System.Windows.Forms.ToolStrip();
            this.tsbSave = new System.Windows.Forms.ToolStripButton();
            this.tsbLoad = new System.Windows.Forms.ToolStripButton();
            this.separator1 = new System.Windows.Forms.ToolStripSeparator();
            this.gbProperties = new System.Windows.Forms.GroupBox();
            this.gbInformation = new System.Windows.Forms.GroupBox();
            this.scOuter = new System.Windows.Forms.SplitContainer();
            this.scInner = new System.Windows.Forms.SplitContainer();
            this.gbData.SuspendLayout();
            this.msMain.SuspendLayout();
            this.tsMain.SuspendLayout();
            this.gbProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scOuter)).BeginInit();
            this.scOuter.Panel1.SuspendLayout();
            this.scOuter.Panel2.SuspendLayout();
            this.scOuter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scInner)).BeginInit();
            this.scInner.Panel1.SuspendLayout();
            this.scInner.Panel2.SuspendLayout();
            this.scInner.SuspendLayout();
            this.SuspendLayout();
            // 
            // pgProperties
            // 
            this.pgProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgProperties.Location = new System.Drawing.Point(3, 16);
            this.pgProperties.Name = "pgProperties";
            this.pgProperties.Size = new System.Drawing.Size(462, 281);
            this.pgProperties.TabIndex = 0;
            // 
            // tvData
            // 
            this.tvData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvData.Location = new System.Drawing.Point(3, 16);
            this.tvData.Name = "tvData";
            this.tvData.Size = new System.Drawing.Size(229, 464);
            this.tvData.TabIndex = 1;
            this.tvData.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FactorySelected);
            // 
            // gbData
            // 
            this.gbData.Controls.Add(this.tvData);
            this.gbData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbData.Location = new System.Drawing.Point(0, 0);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(235, 483);
            this.gbData.TabIndex = 2;
            this.gbData.TabStop = false;
            this.gbData.Text = "Data";
            // 
            // msMain
            // 
            this.msMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile});
            this.msMain.Location = new System.Drawing.Point(0, 0);
            this.msMain.Name = "msMain";
            this.msMain.Size = new System.Drawing.Size(707, 24);
            this.msMain.TabIndex = 3;
            // 
            // tsmiFile
            // 
            this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiSave,
            this.tsmiLoad,
            this.separator0,
            this.tsmiExit});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(37, 20);
            this.tsmiFile.Text = "&File";
            // 
            // tsmiSave
            // 
            this.tsmiSave.Image = global::Space.Tools.Properties.Resources.saveHS;
            this.tsmiSave.Name = "tsmiSave";
            this.tsmiSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.tsmiSave.Size = new System.Drawing.Size(143, 22);
            this.tsmiSave.Text = "&Save";
            this.tsmiSave.Click += new System.EventHandler(this.SaveClick);
            // 
            // tsmiLoad
            // 
            this.tsmiLoad.Image = global::Space.Tools.Properties.Resources.OpenFile;
            this.tsmiLoad.Name = "tsmiLoad";
            this.tsmiLoad.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.tsmiLoad.Size = new System.Drawing.Size(143, 22);
            this.tsmiLoad.Text = "L&oad";
            this.tsmiLoad.Click += new System.EventHandler(this.LoadClick);
            // 
            // separator0
            // 
            this.separator0.Name = "separator0";
            this.separator0.Size = new System.Drawing.Size(140, 6);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.tsmiExit.Size = new System.Drawing.Size(143, 22);
            this.tsmiExit.Text = "E&xit";
            this.tsmiExit.Click += new System.EventHandler(this.ExitClick);
            // 
            // tsMain
            // 
            this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbSave,
            this.tsbLoad,
            this.separator1});
            this.tsMain.Location = new System.Drawing.Point(0, 24);
            this.tsMain.Name = "tsMain";
            this.tsMain.Size = new System.Drawing.Size(707, 25);
            this.tsMain.TabIndex = 4;
            // 
            // tsbSave
            // 
            this.tsbSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSave.Image = global::Space.Tools.Properties.Resources.saveHS;
            this.tsbSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSave.Name = "tsbSave";
            this.tsbSave.Size = new System.Drawing.Size(23, 22);
            this.tsbSave.Text = "Save";
            this.tsbSave.Click += new System.EventHandler(this.SaveClick);
            // 
            // tsbLoad
            // 
            this.tsbLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbLoad.Image = global::Space.Tools.Properties.Resources.OpenFile;
            this.tsbLoad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbLoad.Name = "tsbLoad";
            this.tsbLoad.Size = new System.Drawing.Size(23, 22);
            this.tsbLoad.Text = "Reload";
            this.tsbLoad.Click += new System.EventHandler(this.LoadClick);
            // 
            // separator1
            // 
            this.separator1.Name = "separator1";
            this.separator1.Size = new System.Drawing.Size(6, 25);
            // 
            // gbProperties
            // 
            this.gbProperties.Controls.Add(this.pgProperties);
            this.gbProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbProperties.Location = new System.Drawing.Point(0, 0);
            this.gbProperties.Name = "gbProperties";
            this.gbProperties.Size = new System.Drawing.Size(468, 300);
            this.gbProperties.TabIndex = 5;
            this.gbProperties.TabStop = false;
            this.gbProperties.Text = "Properties";
            // 
            // gbInformation
            // 
            this.gbInformation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbInformation.Location = new System.Drawing.Point(0, 0);
            this.gbInformation.Name = "gbInformation";
            this.gbInformation.Size = new System.Drawing.Size(468, 179);
            this.gbInformation.TabIndex = 6;
            this.gbInformation.TabStop = false;
            this.gbInformation.Text = "Information";
            // 
            // scOuter
            // 
            this.scOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scOuter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.scOuter.Location = new System.Drawing.Point(0, 49);
            this.scOuter.Name = "scOuter";
            // 
            // scOuter.Panel1
            // 
            this.scOuter.Panel1.Controls.Add(this.gbData);
            // 
            // scOuter.Panel2
            // 
            this.scOuter.Panel2.Controls.Add(this.scInner);
            this.scOuter.Size = new System.Drawing.Size(707, 483);
            this.scOuter.SplitterDistance = 235;
            this.scOuter.TabIndex = 7;
            // 
            // scInner
            // 
            this.scInner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scInner.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.scInner.Location = new System.Drawing.Point(0, 0);
            this.scInner.Name = "scInner";
            this.scInner.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scInner.Panel1
            // 
            this.scInner.Panel1.Controls.Add(this.gbProperties);
            // 
            // scInner.Panel2
            // 
            this.scInner.Panel2.Controls.Add(this.gbInformation);
            this.scInner.Size = new System.Drawing.Size(468, 483);
            this.scInner.SplitterDistance = 300;
            this.scInner.TabIndex = 0;
            // 
            // DataEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(707, 532);
            this.Controls.Add(this.scOuter);
            this.Controls.Add(this.tsMain);
            this.Controls.Add(this.msMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.msMain;
            this.Name = "DataEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Data Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DataEditorClosing);
            this.gbData.ResumeLayout(false);
            this.msMain.ResumeLayout(false);
            this.msMain.PerformLayout();
            this.tsMain.ResumeLayout(false);
            this.tsMain.PerformLayout();
            this.gbProperties.ResumeLayout(false);
            this.scOuter.Panel1.ResumeLayout(false);
            this.scOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scOuter)).EndInit();
            this.scOuter.ResumeLayout(false);
            this.scInner.Panel1.ResumeLayout(false);
            this.scInner.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scInner)).EndInit();
            this.scInner.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid pgProperties;
        private System.Windows.Forms.TreeView tvData;
        private System.Windows.Forms.GroupBox gbData;
        private System.Windows.Forms.MenuStrip msMain;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiSave;
        private System.Windows.Forms.ToolStripMenuItem tsmiLoad;
        private System.Windows.Forms.ToolStripSeparator separator0;
        private System.Windows.Forms.ToolStrip tsMain;
        private System.Windows.Forms.ToolStripButton tsbSave;
        private System.Windows.Forms.ToolStripButton tsbLoad;
        private System.Windows.Forms.ToolStripSeparator separator1;
        private System.Windows.Forms.GroupBox gbProperties;
        private System.Windows.Forms.GroupBox gbInformation;
        private System.Windows.Forms.SplitContainer scOuter;
        private System.Windows.Forms.SplitContainer scInner;
    }
}

