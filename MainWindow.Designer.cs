namespace PEUtility
{
    partial class MainWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.importsTab1 = new System.Windows.Forms.TabPage();
            this.importsList = new System.Windows.Forms.TreeView();
            this.filterLabel2 = new System.Windows.Forms.Label();
            this.importSearchBox = new System.Windows.Forms.TextBox();
            this.exportsTab = new System.Windows.Forms.TabPage();
            this.filterLabel1 = new System.Windows.Forms.Label();
            this.exportSearchBox = new System.Windows.Forms.TextBox();
            this.exportsList = new System.Windows.Forms.ListView();
            this.tabControlImageList = new System.Windows.Forms.ImageList(this.components);
            this.treeContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.importsTab1.SuspendLayout();
            this.exportsTab.SuspendLayout();
            this.treeContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(527, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // recentToolStripMenuItem
            // 
            this.recentToolStripMenuItem.Enabled = false;
            this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            this.recentToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.recentToolStripMenuItem.Text = "Open recent";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(136, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.importsTab1);
            this.tabControl.Controls.Add(this.exportsTab);
            this.tabControl.ImageList = this.tabControlImageList;
            this.tabControl.Location = new System.Drawing.Point(0, 27);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(529, 334);
            this.tabControl.TabIndex = 1;
            // 
            // importsTab1
            // 
            this.importsTab1.Controls.Add(this.importsList);
            this.importsTab1.Controls.Add(this.filterLabel2);
            this.importsTab1.Controls.Add(this.importSearchBox);
            this.importsTab1.ImageKey = "imports.png";
            this.importsTab1.Location = new System.Drawing.Point(4, 27);
            this.importsTab1.Name = "importsTab1";
            this.importsTab1.Padding = new System.Windows.Forms.Padding(3);
            this.importsTab1.Size = new System.Drawing.Size(521, 303);
            this.importsTab1.TabIndex = 0;
            this.importsTab1.Text = "Imports";
            this.importsTab1.UseVisualStyleBackColor = true;
            // 
            // importsList
            // 
            this.importsList.Location = new System.Drawing.Point(7, 33);
            this.importsList.Name = "importsList";
            this.importsList.Size = new System.Drawing.Size(506, 265);
            this.importsList.TabIndex = 6;
            // 
            // filterLabel2
            // 
            this.filterLabel2.AutoSize = true;
            this.filterLabel2.Location = new System.Drawing.Point(6, 10);
            this.filterLabel2.Name = "filterLabel2";
            this.filterLabel2.Size = new System.Drawing.Size(32, 13);
            this.filterLabel2.TabIndex = 5;
            this.filterLabel2.Text = "Filter:";
            // 
            // importSearchBox
            // 
            this.importSearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.importSearchBox.Enabled = false;
            this.importSearchBox.Location = new System.Drawing.Point(41, 7);
            this.importSearchBox.Name = "importSearchBox";
            this.importSearchBox.Size = new System.Drawing.Size(472, 20);
            this.importSearchBox.TabIndex = 4;
            // 
            // exportsTab
            // 
            this.exportsTab.Controls.Add(this.filterLabel1);
            this.exportsTab.Controls.Add(this.exportSearchBox);
            this.exportsTab.Controls.Add(this.exportsList);
            this.exportsTab.ImageKey = "exports.png";
            this.exportsTab.Location = new System.Drawing.Point(4, 27);
            this.exportsTab.Name = "exportsTab";
            this.exportsTab.Padding = new System.Windows.Forms.Padding(3);
            this.exportsTab.Size = new System.Drawing.Size(521, 303);
            this.exportsTab.TabIndex = 1;
            this.exportsTab.Text = "Exports";
            this.exportsTab.UseVisualStyleBackColor = true;
            // 
            // filterLabel1
            // 
            this.filterLabel1.AutoSize = true;
            this.filterLabel1.Location = new System.Drawing.Point(6, 10);
            this.filterLabel1.Name = "filterLabel1";
            this.filterLabel1.Size = new System.Drawing.Size(32, 13);
            this.filterLabel1.TabIndex = 2;
            this.filterLabel1.Text = "Filter:";
            // 
            // exportSearchBox
            // 
            this.exportSearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.exportSearchBox.Enabled = false;
            this.exportSearchBox.Location = new System.Drawing.Point(41, 7);
            this.exportSearchBox.Name = "exportSearchBox";
            this.exportSearchBox.Size = new System.Drawing.Size(472, 20);
            this.exportSearchBox.TabIndex = 1;
            // 
            // exportsList
            // 
            this.exportsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.exportsList.LabelEdit = true;
            this.exportsList.Location = new System.Drawing.Point(7, 33);
            this.exportsList.Name = "exportsList";
            this.exportsList.Size = new System.Drawing.Size(506, 265);
            this.exportsList.TabIndex = 0;
            this.exportsList.UseCompatibleStateImageBehavior = false;
            this.exportsList.View = System.Windows.Forms.View.List;
            // 
            // tabControlImageList
            // 
            this.tabControlImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("tabControlImageList.ImageStream")));
            this.tabControlImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.tabControlImageList.Images.SetKeyName(0, "imports.png");
            this.tabControlImageList.Images.SetKeyName(1, "exports.png");
            // 
            // importsContextMenu
            // 
            this.treeContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyNameToolStripMenuItem});
            this.treeContextMenu.Name = "importsContextMenu";
            this.treeContextMenu.Size = new System.Drawing.Size(153, 48);
            // 
            // copyNameToolStripMenuItem
            // 
            this.copyNameToolStripMenuItem.Name = "copyNameToolStripMenuItem";
            this.copyNameToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.copyNameToolStripMenuItem.Text = "Copy name";
            this.copyNameToolStripMenuItem.Click += new System.EventHandler(this.copyNameToolStripMenuItem_Click);
            // 
            // MainWindow
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(527, 360);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "PE Disassembler";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.importsTab1.ResumeLayout(false);
            this.importsTab1.PerformLayout();
            this.exportsTab.ResumeLayout(false);
            this.exportsTab.PerformLayout();
            this.treeContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage importsTab1;
        private System.Windows.Forms.TabPage exportsTab;
        private System.Windows.Forms.ImageList tabControlImageList;
        private System.Windows.Forms.ListView exportsList;
        private System.Windows.Forms.TextBox exportSearchBox;
        private System.Windows.Forms.Label filterLabel1;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.Label filterLabel2;
        private System.Windows.Forms.TextBox importSearchBox;
        private System.Windows.Forms.TreeView importsList;
        private System.Windows.Forms.ContextMenuStrip treeContextMenu;
        private System.Windows.Forms.ToolStripMenuItem copyNameToolStripMenuItem;
    }
}

