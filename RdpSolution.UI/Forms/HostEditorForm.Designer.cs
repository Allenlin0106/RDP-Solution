namespace RdpSolution.UI.Forms
{
    partial class HostEditorForm
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
            this.grpConnection     = new System.Windows.Forms.GroupBox();
            this.lblName           = new System.Windows.Forms.Label();
            this.txtName           = new System.Windows.Forms.TextBox();
            this.lblHostname       = new System.Windows.Forms.Label();
            this.txtHostname       = new System.Windows.Forms.TextBox();
            this.lblPort           = new System.Windows.Forms.Label();
            this.nudPort           = new System.Windows.Forms.NumericUpDown();
            this.lblProtocol       = new System.Windows.Forms.Label();
            this.cboProtocol       = new System.Windows.Forms.ComboBox();
            this.lblVncAuthType    = new System.Windows.Forms.Label();
            this.cboVncAuthType    = new System.Windows.Forms.ComboBox();
            this.lblUsername       = new System.Windows.Forms.Label();
            this.txtUsername       = new System.Windows.Forms.TextBox();
            this.lblDomain         = new System.Windows.Forms.Label();
            this.txtDomain         = new System.Windows.Forms.TextBox();
            this.lblPassword       = new System.Windows.Forms.Label();
            this.txtPassword       = new System.Windows.Forms.TextBox();
            this.grpDisplay        = new System.Windows.Forms.GroupBox();
            this.chkFullScreen     = new System.Windows.Forms.CheckBox();
            this.lblWidth          = new System.Windows.Forms.Label();
            this.nudWidth          = new System.Windows.Forms.NumericUpDown();
            this.lblHeight         = new System.Windows.Forms.Label();
            this.nudHeight         = new System.Windows.Forms.NumericUpDown();
            this.lblColorDepth     = new System.Windows.Forms.Label();
            this.cmbColorDepth     = new System.Windows.Forms.ComboBox();
            this.grpDevices        = new System.Windows.Forms.GroupBox();
            this.chkAttachDrives   = new System.Windows.Forms.CheckBox();
            this.chkAttachPrinters = new System.Windows.Forms.CheckBox();
            this.grpNotes          = new System.Windows.Forms.GroupBox();
            this.txtNotes          = new System.Windows.Forms.TextBox();
            this.btnOk             = new System.Windows.Forms.Button();
            this.btnCancel         = new System.Windows.Forms.Button();

            this.grpConnection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.grpDisplay.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).BeginInit();
            this.grpDevices.SuspendLayout();
            this.grpNotes.SuspendLayout();
            this.SuspendLayout();

            // ==== grpConnection (y=8, h=264 — 6 rows × 29 px + header) ====
            this.grpConnection.Controls.Add(this.lblName);
            this.grpConnection.Controls.Add(this.txtName);
            this.grpConnection.Controls.Add(this.lblHostname);
            this.grpConnection.Controls.Add(this.txtHostname);
            this.grpConnection.Controls.Add(this.lblPort);
            this.grpConnection.Controls.Add(this.nudPort);
            this.grpConnection.Controls.Add(this.lblProtocol);
            this.grpConnection.Controls.Add(this.cboProtocol);
            this.grpConnection.Controls.Add(this.lblVncAuthType);
            this.grpConnection.Controls.Add(this.cboVncAuthType);
            this.grpConnection.Controls.Add(this.lblUsername);
            this.grpConnection.Controls.Add(this.txtUsername);
            this.grpConnection.Controls.Add(this.lblDomain);
            this.grpConnection.Controls.Add(this.txtDomain);
            this.grpConnection.Controls.Add(this.lblPassword);
            this.grpConnection.Controls.Add(this.txtPassword);
            this.grpConnection.Location = new System.Drawing.Point(12, 8);
            this.grpConnection.Name     = "grpConnection";
            this.grpConnection.Size     = new System.Drawing.Size(452, 264);
            this.grpConnection.TabIndex = 0;
            this.grpConnection.TabStop  = false;
            this.grpConnection.Text     = "Connection";

            // Row 1: Display name
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(10, 24);
            this.lblName.Name     = "lblName";
            this.lblName.Text     = "Display &name:";

            this.txtName.Location  = new System.Drawing.Point(120, 21);
            this.txtName.MaxLength = 128;
            this.txtName.Name      = "txtName";
            this.txtName.Size      = new System.Drawing.Size(320, 20);
            this.txtName.TabIndex  = 0;

            // Row 2: Hostname
            this.lblHostname.AutoSize = true;
            this.lblHostname.Location = new System.Drawing.Point(10, 53);
            this.lblHostname.Name     = "lblHostname";
            this.lblHostname.Text     = "&Hostname / IP:";

            this.txtHostname.Location  = new System.Drawing.Point(120, 50);
            this.txtHostname.MaxLength = 255;
            this.txtHostname.Name      = "txtHostname";
            this.txtHostname.Size      = new System.Drawing.Size(320, 20);
            this.txtHostname.TabIndex  = 1;

            // Row 3: Port
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(10, 82);
            this.lblPort.Name     = "lblPort";
            this.lblPort.Text     = "&Port:";

            this.nudPort.Location  = new System.Drawing.Point(120, 79);
            this.nudPort.Maximum   = new decimal(new int[] { 65535, 0, 0, 0 });
            this.nudPort.Minimum   = new decimal(new int[] { 1,     0, 0, 0 });
            this.nudPort.Name      = "nudPort";
            this.nudPort.Size      = new System.Drawing.Size(80, 20);
            this.nudPort.TabIndex  = 2;
            this.nudPort.Value     = new decimal(new int[] { 3389, 0, 0, 0 });

            // Row 4: Protocol
            this.lblProtocol.AutoSize = true;
            this.lblProtocol.Location = new System.Drawing.Point(10, 111);
            this.lblProtocol.Name     = "lblProtocol";
            this.lblProtocol.Text     = "P&rotocol:";

            this.cboProtocol.DropDownStyle     = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboProtocol.FormattingEnabled = false;
            this.cboProtocol.Items.AddRange(new object[] { "RDP", "VNC" });
            this.cboProtocol.Location     = new System.Drawing.Point(120, 108);
            this.cboProtocol.Name         = "cboProtocol";
            this.cboProtocol.Size         = new System.Drawing.Size(140, 21);
            this.cboProtocol.TabIndex     = 3;
            this.cboProtocol.SelectedIndex = 0;
            this.cboProtocol.SelectedIndexChanged += new System.EventHandler(this.OnProtocolChanged);

            // Row 5: VNC Auth Type (hidden by default — shown only for VNC)
            this.lblVncAuthType.AutoSize = true;
            this.lblVncAuthType.Location = new System.Drawing.Point(10, 140);
            this.lblVncAuthType.Name     = "lblVncAuthType";
            this.lblVncAuthType.Text     = "VNC &Auth:";
            this.lblVncAuthType.Visible  = false;

            this.cboVncAuthType.DropDownStyle     = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboVncAuthType.FormattingEnabled = false;
            this.cboVncAuthType.Items.AddRange(new object[] { "VNC Password", "MS Logon" });
            this.cboVncAuthType.Location     = new System.Drawing.Point(120, 137);
            this.cboVncAuthType.Name         = "cboVncAuthType";
            this.cboVncAuthType.Size         = new System.Drawing.Size(140, 21);
            this.cboVncAuthType.TabIndex     = 4;
            this.cboVncAuthType.SelectedIndex = 0;
            this.cboVncAuthType.Visible       = false;
            this.cboVncAuthType.SelectedIndexChanged += new System.EventHandler(this.OnVncAuthTypeChanged);

            // Row 6: Username
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(10, 169);
            this.lblUsername.Name     = "lblUsername";
            this.lblUsername.Text     = "&Username:";

            this.txtUsername.Location  = new System.Drawing.Point(120, 166);
            this.txtUsername.MaxLength = 128;
            this.txtUsername.Name      = "txtUsername";
            this.txtUsername.Size      = new System.Drawing.Size(320, 20);
            this.txtUsername.TabIndex  = 5;

            // Row 7: Domain
            this.lblDomain.AutoSize = true;
            this.lblDomain.Location = new System.Drawing.Point(10, 198);
            this.lblDomain.Name     = "lblDomain";
            this.lblDomain.Text     = "&Domain:";

            this.txtDomain.Location  = new System.Drawing.Point(120, 195);
            this.txtDomain.MaxLength = 128;
            this.txtDomain.Name      = "txtDomain";
            this.txtDomain.Size      = new System.Drawing.Size(320, 20);
            this.txtDomain.TabIndex  = 6;

            // Row 8: Password
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(10, 227);
            this.lblPassword.Name     = "lblPassword";
            this.lblPassword.Text     = "Pass&word:";

            this.txtPassword.Location              = new System.Drawing.Point(120, 224);
            this.txtPassword.MaxLength             = 256;
            this.txtPassword.Name                  = "txtPassword";
            this.txtPassword.Size                  = new System.Drawing.Size(320, 20);
            this.txtPassword.TabIndex              = 7;
            this.txtPassword.UseSystemPasswordChar = true;

            // ==== grpDisplay (y=278) ====
            this.grpDisplay.Controls.Add(this.chkFullScreen);
            this.grpDisplay.Controls.Add(this.lblWidth);
            this.grpDisplay.Controls.Add(this.nudWidth);
            this.grpDisplay.Controls.Add(this.lblHeight);
            this.grpDisplay.Controls.Add(this.nudHeight);
            this.grpDisplay.Controls.Add(this.lblColorDepth);
            this.grpDisplay.Controls.Add(this.cmbColorDepth);
            this.grpDisplay.Location = new System.Drawing.Point(12, 278);
            this.grpDisplay.Name     = "grpDisplay";
            this.grpDisplay.Size     = new System.Drawing.Size(452, 94);
            this.grpDisplay.TabIndex = 1;
            this.grpDisplay.TabStop  = false;
            this.grpDisplay.Text     = "Display";

            this.chkFullScreen.AutoSize = true;
            this.chkFullScreen.Location = new System.Drawing.Point(12, 22);
            this.chkFullScreen.Name     = "chkFullScreen";
            this.chkFullScreen.Size     = new System.Drawing.Size(80, 17);
            this.chkFullScreen.TabIndex = 0;
            this.chkFullScreen.Text     = "&Full screen";
            this.chkFullScreen.CheckedChanged += new System.EventHandler(this.chkFullScreen_CheckedChanged);

            this.lblWidth.AutoSize = true;
            this.lblWidth.Location = new System.Drawing.Point(10, 60);
            this.lblWidth.Name     = "lblWidth";
            this.lblWidth.Text     = "&Width:";

            this.nudWidth.Location  = new System.Drawing.Point(60, 57);
            this.nudWidth.Maximum   = new decimal(new int[] { 3840, 0, 0, 0 });
            this.nudWidth.Minimum   = new decimal(new int[] { 320,  0, 0, 0 });
            this.nudWidth.Name      = "nudWidth";
            this.nudWidth.Size      = new System.Drawing.Size(72, 20);
            this.nudWidth.TabIndex  = 1;
            this.nudWidth.Value     = new decimal(new int[] { 1024, 0, 0, 0 });

            this.lblHeight.AutoSize = true;
            this.lblHeight.Location = new System.Drawing.Point(148, 60);
            this.lblHeight.Name     = "lblHeight";
            this.lblHeight.Text     = "&Height:";

            this.nudHeight.Location  = new System.Drawing.Point(200, 57);
            this.nudHeight.Maximum   = new decimal(new int[] { 2160, 0, 0, 0 });
            this.nudHeight.Minimum   = new decimal(new int[] { 240,  0, 0, 0 });
            this.nudHeight.Name      = "nudHeight";
            this.nudHeight.Size      = new System.Drawing.Size(72, 20);
            this.nudHeight.TabIndex  = 2;
            this.nudHeight.Value     = new decimal(new int[] { 768, 0, 0, 0 });

            this.lblColorDepth.AutoSize = true;
            this.lblColorDepth.Location = new System.Drawing.Point(288, 60);
            this.lblColorDepth.Name     = "lblColorDepth";
            this.lblColorDepth.Text     = "&Color:";

            this.cmbColorDepth.DropDownStyle     = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbColorDepth.FormattingEnabled = false;
            this.cmbColorDepth.Items.AddRange(new object[] { "8", "15", "16", "24", "32" });
            this.cmbColorDepth.Location     = new System.Drawing.Point(330, 57);
            this.cmbColorDepth.Name         = "cmbColorDepth";
            this.cmbColorDepth.Size         = new System.Drawing.Size(60, 21);
            this.cmbColorDepth.TabIndex     = 3;
            this.cmbColorDepth.SelectedIndex = 4;

            // ==== grpDevices (y=378) ====
            this.grpDevices.Controls.Add(this.chkAttachDrives);
            this.grpDevices.Controls.Add(this.chkAttachPrinters);
            this.grpDevices.Location = new System.Drawing.Point(12, 378);
            this.grpDevices.Name     = "grpDevices";
            this.grpDevices.Size     = new System.Drawing.Size(452, 72);
            this.grpDevices.TabIndex = 2;
            this.grpDevices.TabStop  = false;
            this.grpDevices.Text     = "Device Redirection";

            this.chkAttachDrives.AutoSize = true;
            this.chkAttachDrives.Location = new System.Drawing.Point(12, 22);
            this.chkAttachDrives.Name     = "chkAttachDrives";
            this.chkAttachDrives.Size     = new System.Drawing.Size(110, 17);
            this.chkAttachDrives.TabIndex = 0;
            this.chkAttachDrives.Text     = "Redirect &drives";

            this.chkAttachPrinters.AutoSize = true;
            this.chkAttachPrinters.Location = new System.Drawing.Point(12, 45);
            this.chkAttachPrinters.Name     = "chkAttachPrinters";
            this.chkAttachPrinters.Size     = new System.Drawing.Size(115, 17);
            this.chkAttachPrinters.TabIndex = 1;
            this.chkAttachPrinters.Text     = "Redirect &printers";

            // ==== grpNotes (y=456) ====
            this.grpNotes.Controls.Add(this.txtNotes);
            this.grpNotes.Location = new System.Drawing.Point(12, 456);
            this.grpNotes.Name     = "grpNotes";
            this.grpNotes.Size     = new System.Drawing.Size(452, 90);
            this.grpNotes.TabIndex = 3;
            this.grpNotes.TabStop  = false;
            this.grpNotes.Text     = "Notes";

            this.txtNotes.Location   = new System.Drawing.Point(10, 18);
            this.txtNotes.Multiline  = true;
            this.txtNotes.Name       = "txtNotes";
            this.txtNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtNotes.Size       = new System.Drawing.Size(432, 60);
            this.txtNotes.TabIndex   = 0;

            // ==== btnOk / btnCancel (y=560) ====
            this.btnOk.Anchor       = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnOk.Location     = new System.Drawing.Point(304, 560);
            this.btnOk.Name         = "btnOk";
            this.btnOk.Size         = new System.Drawing.Size(75, 28);
            this.btnOk.TabIndex     = 4;
            this.btnOk.Text         = "&OK";
            this.btnOk.Click       += new System.EventHandler(this.btnOk_Click);

            this.btnCancel.Anchor       = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location     = new System.Drawing.Point(389, 560);
            this.btnCancel.Name         = "btnCancel";
            this.btnCancel.Size         = new System.Drawing.Size(75, 28);
            this.btnCancel.TabIndex     = 5;
            this.btnCancel.Text         = "&Cancel";
            this.btnCancel.Click       += new System.EventHandler(this.btnCancel_Click);

            // ==== HostEditorForm ====
            this.AcceptButton        = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton        = this.btnCancel;
            this.ClientSize          = new System.Drawing.Size(476, 600);
            this.Controls.Add(this.grpConnection);
            this.Controls.Add(this.grpDisplay);
            this.Controls.Add(this.grpDevices);
            this.Controls.Add(this.grpNotes);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox         = false;
            this.MinimizeBox         = false;
            this.Name                = "HostEditorForm";
            this.ShowInTaskbar       = false;
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text                = "Add Host";

            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.grpDisplay.ResumeLayout(false);
            this.grpDisplay.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).EndInit();
            this.grpDevices.ResumeLayout(false);
            this.grpDevices.PerformLayout();
            this.grpNotes.ResumeLayout(false);
            this.grpNotes.PerformLayout();
            this.ResumeLayout(false);
        }

        // ---- field declarations ----
        private System.Windows.Forms.GroupBox       grpConnection;
        private System.Windows.Forms.Label          lblName;
        private System.Windows.Forms.TextBox        txtName;
        private System.Windows.Forms.Label          lblHostname;
        private System.Windows.Forms.TextBox        txtHostname;
        private System.Windows.Forms.Label          lblPort;
        private System.Windows.Forms.NumericUpDown  nudPort;
        private System.Windows.Forms.Label          lblProtocol;
        private System.Windows.Forms.ComboBox       cboProtocol;
        private System.Windows.Forms.Label          lblVncAuthType;
        private System.Windows.Forms.ComboBox       cboVncAuthType;
        private System.Windows.Forms.Label          lblUsername;
        private System.Windows.Forms.TextBox        txtUsername;
        private System.Windows.Forms.Label          lblDomain;
        private System.Windows.Forms.TextBox        txtDomain;
        private System.Windows.Forms.Label          lblPassword;
        private System.Windows.Forms.TextBox        txtPassword;
        private System.Windows.Forms.GroupBox       grpDisplay;
        private System.Windows.Forms.CheckBox       chkFullScreen;
        private System.Windows.Forms.Label          lblWidth;
        private System.Windows.Forms.NumericUpDown  nudWidth;
        private System.Windows.Forms.Label          lblHeight;
        private System.Windows.Forms.NumericUpDown  nudHeight;
        private System.Windows.Forms.Label          lblColorDepth;
        private System.Windows.Forms.ComboBox       cmbColorDepth;
        private System.Windows.Forms.GroupBox       grpDevices;
        private System.Windows.Forms.CheckBox       chkAttachDrives;
        private System.Windows.Forms.CheckBox       chkAttachPrinters;
        private System.Windows.Forms.GroupBox       grpNotes;
        private System.Windows.Forms.TextBox        txtNotes;
        private System.Windows.Forms.Button         btnOk;
        private System.Windows.Forms.Button         btnCancel;
    }
}
