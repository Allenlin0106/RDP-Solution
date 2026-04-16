namespace RdpSolution.UI.Forms
{
    partial class SessionTabsForm
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
            this.toolStrip1      = new System.Windows.Forms.ToolStrip();
            this.tsbDisconnect   = new System.Windows.Forms.ToolStripButton();
            this.tsbReconnect    = new System.Windows.Forms.ToolStripButton();
            this.tsSep1          = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose        = new System.Windows.Forms.ToolStripButton();
            this.tabControl      = new System.Windows.Forms.TabControl();
            this.statusStrip1    = new System.Windows.Forms.StatusStrip();
            this.statusLabel     = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsSep2          = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslCount        = new System.Windows.Forms.ToolStripStatusLabel();

            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();

            // ---- toolStrip1 ----
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbDisconnect,
                this.tsbReconnect,
                this.tsSep1,
                this.tsbClose });
            this.toolStrip1.Location  = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name      = "toolStrip1";
            this.toolStrip1.Size      = new System.Drawing.Size(1280, 25);
            this.toolStrip1.TabIndex  = 0;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;

            this.tsbDisconnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbDisconnect.Enabled      = false;
            this.tsbDisconnect.Name         = "tsbDisconnect";
            this.tsbDisconnect.Size         = new System.Drawing.Size(80, 22);
            this.tsbDisconnect.Text         = "\u2298 Disconnect";
            this.tsbDisconnect.Click       += new System.EventHandler(this.tsbDisconnect_Click);

            this.tsbReconnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbReconnect.Enabled      = false;
            this.tsbReconnect.Name         = "tsbReconnect";
            this.tsbReconnect.Size         = new System.Drawing.Size(78, 22);
            this.tsbReconnect.Text         = "\u21ba Reconnect";
            this.tsbReconnect.Click       += new System.EventHandler(this.tsbReconnect_Click);

            this.tsSep1.Name = "tsSep1";
            this.tsSep1.Size = new System.Drawing.Size(6, 25);

            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Enabled      = false;
            this.tsbClose.Name         = "tsbClose";
            this.tsbClose.Size         = new System.Drawing.Size(72, 22);
            this.tsbClose.Text         = "\u2715 Close Tab";
            this.tsbClose.Click       += new System.EventHandler(this.tsbClose_Click);

            // ---- tabControl ----
            this.tabControl.Dock                  = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Name                  = "tabControl";
            this.tabControl.TabIndex              = 1;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);

            // ---- statusStrip1 ----
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.statusLabel,
                this.tsSep2,
                this.tslCount });
            this.statusStrip1.Location = new System.Drawing.Point(0, 750);
            this.statusStrip1.Name     = "statusStrip1";
            this.statusStrip1.Size     = new System.Drawing.Size(1280, 22);
            this.statusStrip1.TabIndex = 2;

            this.statusLabel.Name   = "statusLabel";
            this.statusLabel.Spring = true;
            this.statusLabel.Text   = string.Empty;

            this.tsSep2.Name         = "tsSep2";
            this.tsSep2.Size         = new System.Drawing.Size(10, 17);
            this.tsSep2.Text         = "|";
            this.tsSep2.BorderSides  = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;

            this.tslCount.Name = "tslCount";
            this.tslCount.Text = "0 session(s)";

            // ---- SessionTabsForm ----
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1280, 772);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name        = "SessionTabsForm";
            this.Text        = "RDP Sessions";

            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // ---- field declarations ----
        private System.Windows.Forms.ToolStrip             toolStrip1;
        private System.Windows.Forms.ToolStripButton       tsbDisconnect;
        private System.Windows.Forms.ToolStripButton       tsbReconnect;
        private System.Windows.Forms.ToolStripSeparator    tsSep1;
        private System.Windows.Forms.ToolStripButton       tsbClose;
        private System.Windows.Forms.TabControl            tabControl;
        private System.Windows.Forms.StatusStrip           statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel  statusLabel;
        private System.Windows.Forms.ToolStripStatusLabel  tsSep2;
        private System.Windows.Forms.ToolStripStatusLabel  tslCount;
    }
}
