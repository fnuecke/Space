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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataEditor));
            this.pgProperties = new System.Windows.Forms.PropertyGrid();
            this.tvData = new System.Windows.Forms.TreeView();
            this.cmsFactories = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiAddFactory = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRemove = new System.Windows.Forms.ToolStripMenuItem();
            this.gbData = new System.Windows.Forms.GroupBox();
            this.msMain = new System.Windows.Forms.MenuStrip();
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSave = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiLoad = new System.Windows.Forms.ToolStripMenuItem();
            this.separator0 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.separator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.miNewFactory = new System.Windows.Forms.ToolStripMenuItem();
            this.miDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.gbProperties = new System.Windows.Forms.GroupBox();
            this.gbPreview = new System.Windows.Forms.GroupBox();
            this.pbPreview = new System.Windows.Forms.PictureBox();
            this.scOuter = new System.Windows.Forms.SplitContainer();
            this.scInner = new System.Windows.Forms.SplitContainer();
            this.scMain = new System.Windows.Forms.SplitContainer();
            this.gbIssues = new System.Windows.Forms.GroupBox();
            this.lvIssues = new System.Windows.Forms.ListView();
            this.chIcon = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chText = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chFactory = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chProperty = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ilIssueTypes = new System.Windows.Forms.ImageList(this.components);
            this.cmsFactories.SuspendLayout();
            this.gbData.SuspendLayout();
            this.msMain.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.gbPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scOuter)).BeginInit();
            this.scOuter.Panel1.SuspendLayout();
            this.scOuter.Panel2.SuspendLayout();
            this.scOuter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scInner)).BeginInit();
            this.scInner.Panel1.SuspendLayout();
            this.scInner.Panel2.SuspendLayout();
            this.scInner.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).BeginInit();
            this.scMain.Panel1.SuspendLayout();
            this.scMain.Panel2.SuspendLayout();
            this.scMain.SuspendLayout();
            this.gbIssues.SuspendLayout();
            this.SuspendLayout();
            // 
            // pgProperties
            // 
            this.pgProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgProperties.Location = new System.Drawing.Point(8, 21);
            this.pgProperties.Name = "pgProperties";
            this.pgProperties.Size = new System.Drawing.Size(437, 133);
            this.pgProperties.TabIndex = 0;
            this.pgProperties.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.PropertiesPropertyValueChanged);
            this.pgProperties.SelectedGridItemChanged += new System.Windows.Forms.SelectedGridItemChangedEventHandler(this.PropertiesSelectedGridItemChanged);
            // 
            // tvData
            // 
            this.tvData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tvData.ContextMenuStrip = this.cmsFactories;
            this.tvData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvData.HideSelection = false;
            this.tvData.HotTracking = true;
            this.tvData.Location = new System.Drawing.Point(8, 21);
            this.tvData.Name = "tvData";
            this.tvData.PathSeparator = "/";
            this.tvData.Size = new System.Drawing.Size(214, 286);
            this.tvData.TabIndex = 1;
            this.tvData.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FactorySelected);
            // 
            // cmsFactories
            // 
            this.cmsFactories.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiAddFactory,
            this.tsmiRemove});
            this.cmsFactories.Name = "cmsFactories";
            this.cmsFactories.Size = new System.Drawing.Size(141, 48);
            // 
            // tsmiAddFactory
            // 
            this.tsmiAddFactory.Image = global::Space.Tools.DataEditor.Properties.Resources._077_AddFile_16x16_72;
            this.tsmiAddFactory.Name = "tsmiAddFactory";
            this.tsmiAddFactory.Size = new System.Drawing.Size(140, 22);
            this.tsmiAddFactory.Text = "New &Factory";
            this.tsmiAddFactory.Click += new System.EventHandler(this.AddFactoryClick);
            // 
            // tsmiRemove
            // 
            this.tsmiRemove.Enabled = false;
            this.tsmiRemove.Image = global::Space.Tools.DataEditor.Properties.Resources.DeleteHS;
            this.tsmiRemove.Name = "tsmiRemove";
            this.tsmiRemove.Size = new System.Drawing.Size(140, 22);
            this.tsmiRemove.Text = "&Delete";
            this.tsmiRemove.Click += new System.EventHandler(this.RemoveClick);
            // 
            // gbData
            // 
            this.gbData.Controls.Add(this.tvData);
            this.gbData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbData.Location = new System.Drawing.Point(5, 5);
            this.gbData.Name = "gbData";
            this.gbData.Padding = new System.Windows.Forms.Padding(8);
            this.gbData.Size = new System.Drawing.Size(230, 315);
            this.gbData.TabIndex = 2;
            this.gbData.TabStop = false;
            this.gbData.Text = "Data";
            // 
            // msMain
            // 
            this.msMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile,
            this.tsmiEdit});
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
            this.tsmiSettings,
            this.separator2,
            this.tsmiExit});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(37, 20);
            this.tsmiFile.Text = "&File";
            // 
            // tsmiSave
            // 
            this.tsmiSave.Enabled = false;
            this.tsmiSave.Image = global::Space.Tools.DataEditor.Properties.Resources.saveHS;
            this.tsmiSave.Name = "tsmiSave";
            this.tsmiSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.tsmiSave.Size = new System.Drawing.Size(143, 22);
            this.tsmiSave.Text = "&Save";
            this.tsmiSave.Click += new System.EventHandler(this.SaveClick);
            // 
            // tsmiLoad
            // 
            this.tsmiLoad.Image = global::Space.Tools.DataEditor.Properties.Resources._075b_UpFolder_16x16_72;
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
            // tsmiSettings
            // 
            this.tsmiSettings.Image = global::Space.Tools.DataEditor.Properties.Resources._327_Options_16x16_72;
            this.tsmiSettings.Name = "tsmiSettings";
            this.tsmiSettings.ShortcutKeys = System.Windows.Forms.Keys.F10;
            this.tsmiSettings.Size = new System.Drawing.Size(143, 22);
            this.tsmiSettings.Text = "S&ettings";
            this.tsmiSettings.Click += new System.EventHandler(this.SettingsClick);
            // 
            // separator2
            // 
            this.separator2.Name = "separator2";
            this.separator2.Size = new System.Drawing.Size(140, 6);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.tsmiExit.Size = new System.Drawing.Size(143, 22);
            this.tsmiExit.Text = "E&xit";
            this.tsmiExit.Click += new System.EventHandler(this.ExitClick);
            // 
            // tsmiEdit
            // 
            this.tsmiEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miNewFactory,
            this.miDelete});
            this.tsmiEdit.Name = "tsmiEdit";
            this.tsmiEdit.Size = new System.Drawing.Size(39, 20);
            this.tsmiEdit.Text = "&Edit";
            // 
            // miNewFactory
            // 
            this.miNewFactory.Image = global::Space.Tools.DataEditor.Properties.Resources._077_AddFile_16x16_72;
            this.miNewFactory.Name = "miNewFactory";
            this.miNewFactory.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.miNewFactory.Size = new System.Drawing.Size(183, 22);
            this.miNewFactory.Text = "New &Factory";
            this.miNewFactory.Click += new System.EventHandler(this.AddFactoryClick);
            // 
            // miDelete
            // 
            this.miDelete.Enabled = false;
            this.miDelete.Image = global::Space.Tools.DataEditor.Properties.Resources.DeleteHS;
            this.miDelete.Name = "miDelete";
            this.miDelete.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
            this.miDelete.Size = new System.Drawing.Size(183, 22);
            this.miDelete.Text = "&Delete";
            this.miDelete.Click += new System.EventHandler(this.RemoveClick);
            // 
            // gbProperties
            // 
            this.gbProperties.Controls.Add(this.pgProperties);
            this.gbProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbProperties.Location = new System.Drawing.Point(5, 5);
            this.gbProperties.Name = "gbProperties";
            this.gbProperties.Padding = new System.Windows.Forms.Padding(8);
            this.gbProperties.Size = new System.Drawing.Size(453, 162);
            this.gbProperties.TabIndex = 5;
            this.gbProperties.TabStop = false;
            this.gbProperties.Text = "Properties";
            // 
            // gbPreview
            // 
            this.gbPreview.Controls.Add(this.pbPreview);
            this.gbPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbPreview.Location = new System.Drawing.Point(5, 0);
            this.gbPreview.Name = "gbPreview";
            this.gbPreview.Padding = new System.Windows.Forms.Padding(8);
            this.gbPreview.Size = new System.Drawing.Size(453, 144);
            this.gbPreview.TabIndex = 6;
            this.gbPreview.TabStop = false;
            this.gbPreview.Text = "Preview";
            // 
            // pbPreview
            // 
            this.pbPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.pbPreview.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pbPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbPreview.Location = new System.Drawing.Point(8, 21);
            this.pbPreview.Name = "pbPreview";
            this.pbPreview.Size = new System.Drawing.Size(437, 115);
            this.pbPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pbPreview.TabIndex = 0;
            this.pbPreview.TabStop = false;
            // 
            // scOuter
            // 
            this.scOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scOuter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.scOuter.Location = new System.Drawing.Point(0, 0);
            this.scOuter.Name = "scOuter";
            // 
            // scOuter.Panel1
            // 
            this.scOuter.Panel1.Controls.Add(this.gbData);
            this.scOuter.Panel1.Padding = new System.Windows.Forms.Padding(5);
            this.scOuter.Panel1MinSize = 40;
            // 
            // scOuter.Panel2
            // 
            this.scOuter.Panel2.Controls.Add(this.scInner);
            this.scOuter.Panel2MinSize = 40;
            this.scOuter.Size = new System.Drawing.Size(707, 325);
            this.scOuter.SplitterDistance = 240;
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
            this.scInner.Panel1.Padding = new System.Windows.Forms.Padding(5);
            this.scInner.Panel1MinSize = 40;
            // 
            // scInner.Panel2
            // 
            this.scInner.Panel2.Controls.Add(this.gbPreview);
            this.scInner.Panel2.Padding = new System.Windows.Forms.Padding(5, 0, 5, 5);
            this.scInner.Panel2MinSize = 40;
            this.scInner.Size = new System.Drawing.Size(463, 325);
            this.scInner.SplitterDistance = 172;
            this.scInner.TabIndex = 0;
            // 
            // scMain
            // 
            this.scMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.scMain.Location = new System.Drawing.Point(0, 24);
            this.scMain.Name = "scMain";
            this.scMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scMain.Panel1
            // 
            this.scMain.Panel1.Controls.Add(this.scOuter);
            this.scMain.Panel1MinSize = 40;
            // 
            // scMain.Panel2
            // 
            this.scMain.Panel2.Controls.Add(this.gbIssues);
            this.scMain.Panel2.Padding = new System.Windows.Forms.Padding(5, 0, 5, 5);
            this.scMain.Panel2MinSize = 40;
            this.scMain.Size = new System.Drawing.Size(707, 508);
            this.scMain.SplitterDistance = 325;
            this.scMain.TabIndex = 8;
            // 
            // gbIssues
            // 
            this.gbIssues.Controls.Add(this.lvIssues);
            this.gbIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbIssues.Location = new System.Drawing.Point(5, 0);
            this.gbIssues.Name = "gbIssues";
            this.gbIssues.Padding = new System.Windows.Forms.Padding(8);
            this.gbIssues.Size = new System.Drawing.Size(697, 174);
            this.gbIssues.TabIndex = 0;
            this.gbIssues.TabStop = false;
            this.gbIssues.Text = "Issues";
            // 
            // lvIssues
            // 
            this.lvIssues.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lvIssues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chIcon,
            this.chText,
            this.chFactory,
            this.chProperty});
            this.lvIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvIssues.FullRowSelect = true;
            this.lvIssues.GridLines = true;
            this.lvIssues.HideSelection = false;
            this.lvIssues.Location = new System.Drawing.Point(8, 21);
            this.lvIssues.MultiSelect = false;
            this.lvIssues.Name = "lvIssues";
            this.lvIssues.Size = new System.Drawing.Size(681, 145);
            this.lvIssues.SmallImageList = this.ilIssueTypes;
            this.lvIssues.TabIndex = 0;
            this.lvIssues.UseCompatibleStateImageBehavior = false;
            this.lvIssues.View = System.Windows.Forms.View.Details;
            this.lvIssues.DoubleClick += new System.EventHandler(this.IssuesDoubleClick);
            // 
            // chIcon
            // 
            this.chIcon.Text = "";
            this.chIcon.Width = 22;
            // 
            // chText
            // 
            this.chText.Text = "Description";
            this.chText.Width = 426;
            // 
            // chFactory
            // 
            this.chFactory.Text = "Factory Name";
            this.chFactory.Width = 133;
            // 
            // chProperty
            // 
            this.chProperty.Text = "Property Name";
            this.chProperty.Width = 122;
            // 
            // ilIssueTypes
            // 
            this.ilIssueTypes.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilIssueTypes.ImageStream")));
            this.ilIssueTypes.TransparentColor = System.Drawing.Color.Transparent;
            this.ilIssueTypes.Images.SetKeyName(0, "109_AllAnnotations_Default_16x16_72.png");
            this.ilIssueTypes.Images.SetKeyName(1, "109_AllAnnotations_Info_16x16_72.png");
            this.ilIssueTypes.Images.SetKeyName(2, "109_AllAnnotations_Warning_16x16_72.png");
            this.ilIssueTypes.Images.SetKeyName(3, "109_AllAnnotations_Error_16x16_72.png");
            // 
            // DataEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(707, 532);
            this.Controls.Add(this.scMain);
            this.Controls.Add(this.msMain);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.msMain;
            this.MinimumSize = new System.Drawing.Size(640, 480);
            this.Name = "DataEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Space - Data Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DataEditorClosing);
            this.Load += new System.EventHandler(this.DataEditorLoad);
            this.cmsFactories.ResumeLayout(false);
            this.gbData.ResumeLayout(false);
            this.msMain.ResumeLayout(false);
            this.msMain.PerformLayout();
            this.gbProperties.ResumeLayout(false);
            this.gbPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbPreview)).EndInit();
            this.scOuter.Panel1.ResumeLayout(false);
            this.scOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scOuter)).EndInit();
            this.scOuter.ResumeLayout(false);
            this.scInner.Panel1.ResumeLayout(false);
            this.scInner.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scInner)).EndInit();
            this.scInner.ResumeLayout(false);
            this.scMain.Panel1.ResumeLayout(false);
            this.scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).EndInit();
            this.scMain.ResumeLayout(false);
            this.gbIssues.ResumeLayout(false);
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
        private System.Windows.Forms.GroupBox gbProperties;
        private System.Windows.Forms.GroupBox gbPreview;
        private System.Windows.Forms.SplitContainer scOuter;
        private System.Windows.Forms.SplitContainer scInner;
        private System.Windows.Forms.ToolStripMenuItem tsmiSettings;
        private System.Windows.Forms.ToolStripSeparator separator2;
        private System.Windows.Forms.PictureBox pbPreview;
        private System.Windows.Forms.SplitContainer scMain;
        private System.Windows.Forms.GroupBox gbIssues;
        private System.Windows.Forms.ListView lvIssues;
        private System.Windows.Forms.ColumnHeader chIcon;
        private System.Windows.Forms.ColumnHeader chFactory;
        private System.Windows.Forms.ColumnHeader chProperty;
        private System.Windows.Forms.ColumnHeader chText;
        private System.Windows.Forms.ImageList ilIssueTypes;
        private System.Windows.Forms.ContextMenuStrip cmsFactories;
        private System.Windows.Forms.ToolStripMenuItem tsmiAddFactory;
        private System.Windows.Forms.ToolStripMenuItem tsmiRemove;
        private System.Windows.Forms.ToolStripMenuItem tsmiEdit;
        private System.Windows.Forms.ToolStripMenuItem miNewFactory;
        private System.Windows.Forms.ToolStripMenuItem miDelete;
    }
}

