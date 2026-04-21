namespace RdpSolution.UI.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.menuStrip1                  = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem       = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem       = new System.Windows.Forms.ToolStripMenuItem();
            this.hostsToolStripMenuItem      = new System.Windows.Forms.ToolStripMenuItem();
            this.addHostToolStripMenuItem    = new System.Windows.Forms.ToolStripMenuItem();
            this.editHostToolStripMenuItem   = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteHostToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1         = new System.Windows.Forms.ToolStripSeparator();
            this.exportRdpToolStripMenuItem  = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2         = new System.Windows.Forms.ToolStripSeparator();
            this.connectToolStripMenuItem    = new System.Windows.Forms.ToolStripMenuItem();
            this.panelButtons                = new System.Windows.Forms.Panel();
            this.btnAdd                      = new System.Windows.Forms.Button();
            this.btnEdit                     = new System.Windows.Forms.Button();
            this.btnDelete                   = new System.Windows.Forms.Button();
            this.btnConnect                  = new System.Windows.Forms.Button();
            this.listViewHosts               = new System.Windows.Forms.ListView();
            this.columnHeaderName            = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderHost            = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPort            = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderUsername        = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderDomain          = new System.Windows.Forms.ColumnHeader();
            this.statusStrip1                = new System.Windows.Forms.StatusStrip();
            this.statusLabel                 = new System.Windows.Forms.ToolStripStatusLabel();

            this.menuStrip1.SuspendLayout();
            this.panelButtons.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();

            // ---- menuStrip1 ----
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileToolStripMenuItem,
                this.hostsToolStripMenuItem });
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name     = "menuStrip1";
            this.menuStrip1.Size     = new System.Drawing.Size(884, 24);
            this.menuStrip1.TabIndex = 0;

            // ---- File menu ----
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.exitToolStripMenuItem });
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";

            this.exitToolStripMenuItem.Name         = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4;
            this.exitToolStripMenuItem.Size         = new System.Drawing.Size(175, 22);
            this.exitToolStripMenuItem.Text         = "E&xit";
            this.exitToolStripMenuItem.Click       += new System.EventHandler(this.exitToolStripMenuItem_Click);

            // ---- Hosts menu ----
            this.hostsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.addHostToolStripMenuItem,
                this.editHostToolStripMenuItem,
                this.deleteHostToolStripMenuItem,
                this.toolStripSeparator1,
                this.exportRdpToolStripMenuItem,
                this.toolStripSeparator2,
                this.connectToolStripMenuItem });
            this.hostsToolStripMenuItem.Name = "hostsToolStripMenuItem";
            this.hostsToolStripMenuItem.Size = new System.Drawing.Size(50, 20);
            this.hostsToolStripMenuItem.Text = "&Hosts";

            this.addHostToolStripMenuItem.Name         = "addHostToolStripMenuItem";
            this.addHostToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N;
            this.addHostToolStripMenuItem.Size         = new System.Drawing.Size(195, 22);
            this.addHostToolStripMenuItem.Text         = "&Add Host…";
            this.addHostToolStripMenuItem.Click       += new System.EventHandler(this.addHostToolStripMenuItem_Click);

            this.editHostToolStripMenuItem.Enabled      = false;
            this.editHostToolStripMenuItem.Name         = "editHostToolStripMenuItem";
            this.editHostToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.editHostToolStripMenuItem.Size         = new System.Drawing.Size(195, 22);
            this.editHostToolStripMenuItem.Text         = "&Edit Host…";
            this.editHostToolStripMenuItem.Click       += new System.EventHandler(this.editHostToolStripMenuItem_Click);

            this.deleteHostToolStripMenuItem.Enabled      = false;
            this.deleteHostToolStripMenuItem.Name         = "deleteHostToolStripMenuItem";
            this.deleteHostToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteHostToolStripMenuItem.Size         = new System.Drawing.Size(195, 22);
            this.deleteHostToolStripMenuItem.Text         = "&Delete Host";
            this.deleteHostToolStripMenuItem.Click       += new System.EventHandler(this.deleteHostToolStripMenuItem_Click);

            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(192, 6);

            this.exportRdpToolStripMenuItem.Enabled      = false;
            this.exportRdpToolStripMenuItem.Name         = "exportRdpToolStripMenuItem";
            this.exportRdpToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E;
            this.exportRdpToolStripMenuItem.Size         = new System.Drawing.Size(195, 22);
            this.exportRdpToolStripMenuItem.Text         = "E&xport .rdp…";
            this.exportRdpToolStripMenuItem.Click       += new System.EventHandler(this.exportRdpToolStripMenuItem_Click);

            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(192, 6);

            this.connectToolStripMenuItem.Enabled      = false;
            this.connectToolStripMenuItem.Font         = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.connectToolStripMenuItem.Name         = "connectToolStripMenuItem";
            this.connectToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.connectToolStripMenuItem.Size         = new System.Drawing.Size(195, 22);
            this.connectToolStripMenuItem.Text         = "&Connect";
            this.connectToolStripMenuItem.Click       += new System.EventHandler(this.connectToolStripMenuItem_Click);

            // ---- panelButtons ----
            this.panelButtons.Controls.Add(this.btnAdd);
            this.panelButtons.Controls.Add(this.btnEdit);
            this.panelButtons.Controls.Add(this.btnDelete);
            this.panelButtons.Controls.Add(this.btnConnect);
            this.panelButtons.Dock    = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Height  = 44;
            this.panelButtons.Name    = "panelButtons";
            this.panelButtons.TabIndex = 2;

            this.btnAdd.Location = new System.Drawing.Point(8, 8);
            this.btnAdd.Name     = "btnAdd";
            this.btnAdd.Size     = new System.Drawing.Size(80, 28);
            this.btnAdd.TabIndex = 0;
            this.btnAdd.Text     = "&Add";
            this.btnAdd.Click   += new System.EventHandler(this.btnAdd_Click);

            this.btnEdit.Enabled  = false;
            this.btnEdit.Location = new System.Drawing.Point(96, 8);
            this.btnEdit.Name     = "btnEdit";
            this.btnEdit.Size     = new System.Drawing.Size(80, 28);
            this.btnEdit.TabIndex = 1;
            this.btnEdit.Text     = "&Edit";
            this.btnEdit.Click   += new System.EventHandler(this.btnEdit_Click);

            this.btnDelete.Enabled  = false;
            this.btnDelete.Location = new System.Drawing.Point(184, 8);
            this.btnDelete.Name     = "btnDelete";
            this.btnDelete.Size     = new System.Drawing.Size(80, 28);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text     = "&Delete";
            this.btnDelete.Click   += new System.EventHandler(this.btnDelete_Click);

            this.btnConnect.Anchor   = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnConnect.Enabled  = false;
            this.btnConnect.Font     = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnConnect.Location = new System.Drawing.Point(794, 8);
            this.btnConnect.Name     = "btnConnect";
            this.btnConnect.Size     = new System.Drawing.Size(82, 28);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text     = "&Connect";
            this.btnConnect.Click   += new System.EventHandler(this.btnConnect_Click);

            // ---- listViewHosts ----
            this.listViewHosts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.columnHeaderName,
                this.columnHeaderHost,
                this.columnHeaderPort,
                this.columnHeaderUsername,
                this.columnHeaderDomain });
            this.listViewHosts.Dock                           = System.Windows.Forms.DockStyle.Fill;
            this.listViewHosts.FullRowSelect                  = true;
            this.listViewHosts.GridLines                      = true;
            this.listViewHosts.MultiSelect                    = false;
            this.listViewHosts.Name                           = "listViewHosts";
            this.listViewHosts.TabIndex                       = 1;
            this.listViewHosts.UseCompatibleStateImageBehavior = false;
            this.listViewHosts.View                           = System.Windows.Forms.View.Details;
            this.listViewHosts.SelectedIndexChanged          += new System.EventHandler(this.listViewHosts_SelectedIndexChanged);
            this.listViewHosts.DoubleClick                   += new System.EventHandler(this.listViewHosts_DoubleClick);

            this.columnHeaderName.Text  = "Name";
            this.columnHeaderName.Width = 190;

            this.columnHeaderHost.Text  = "Host / IP Address";
            this.columnHeaderHost.Width = 210;

            this.columnHeaderPort.Text  = "Port";
            this.columnHeaderPort.Width = 55;

            this.columnHeaderUsername.Text  = "Username";
            this.columnHeaderUsername.Width = 145;

            this.columnHeaderDomain.Text  = "Domain";
            this.columnHeaderDomain.Width = 145;

            // ---- statusStrip1 ----
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.statusLabel });
            this.statusStrip1.Location = new System.Drawing.Point(0, 556);
            this.statusStrip1.Name     = "statusStrip1";
            this.statusStrip1.Size     = new System.Drawing.Size(884, 22);
            this.statusStrip1.TabIndex = 3;

            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Text = "Ready";

            // ---- MainForm ----
            // Controls added in this order so Dock resolution gives:
            //   menuStrip  → top edge
            //   statusStrip → bottom edge (added first = furthest-bottom docked)
            //   panelButtons → bottom edge (added second = above statusStrip)
            //   listViewHosts → Fill (remaining space)
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(884, 578);
            this.Controls.Add(this.listViewHosts);
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize   = new System.Drawing.Size(680, 400);
            this.Name          = "MainForm";
            this.Text          = "RDP Connection Manager";

            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panelButtons.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // ---- field declarations ----
        private System.Windows.Forms.MenuStrip           menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem   fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem   exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem   hostsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem   addHostToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem   editHostToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem   deleteHostToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator  toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem   exportRdpToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator  toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem   connectToolStripMenuItem;
        private System.Windows.Forms.Panel               panelButtons;
        private System.Windows.Forms.Button              btnAdd;
        private System.Windows.Forms.Button              btnEdit;
        private System.Windows.Forms.Button              btnDelete;
        private System.Windows.Forms.Button              btnConnect;
        private System.Windows.Forms.ListView            listViewHosts;
        private System.Windows.Forms.ColumnHeader        columnHeaderName;
        private System.Windows.Forms.ColumnHeader        columnHeaderHost;
        private System.Windows.Forms.ColumnHeader        columnHeaderPort;
        private System.Windows.Forms.ColumnHeader        columnHeaderUsername;
        private System.Windows.Forms.ColumnHeader        columnHeaderDomain;
        private System.Windows.Forms.StatusStrip         statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
    }
}
