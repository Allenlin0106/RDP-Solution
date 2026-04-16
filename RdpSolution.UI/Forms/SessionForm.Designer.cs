namespace RdpSolution.UI.Forms
{
    partial class SessionForm
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
            this.panelTop      = new System.Windows.Forms.Panel();
            this.lblServerInfo = new System.Windows.Forms.Label();
            this.lblStatus     = new System.Windows.Forms.Label();
            this.btnAction     = new System.Windows.Forms.Button();
            this.rdpClient     = new RdpSolution.UI.RdpClient.MsRdpClientControl();

            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rdpClient)).BeginInit();
            this.SuspendLayout();

            // ---- panelTop ----
            this.panelTop.Controls.Add(this.lblServerInfo);
            this.panelTop.Controls.Add(this.lblStatus);
            this.panelTop.Controls.Add(this.btnAction);
            this.panelTop.Dock      = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Height    = 38;
            this.panelTop.Name      = "panelTop";
            this.panelTop.TabIndex  = 0;

            // ---- lblServerInfo ----
            this.lblServerInfo.AutoSize  = true;
            this.lblServerInfo.Location  = new System.Drawing.Point(8, 11);
            this.lblServerInfo.Name      = "lblServerInfo";
            this.lblServerInfo.Text      = "";
            this.lblServerInfo.Font      = new System.Drawing.Font("Consolas", 9F);

            // ---- lblStatus ----
            this.lblStatus.Anchor    = System.Windows.Forms.AnchorStyles.Top
                                     | System.Windows.Forms.AnchorStyles.Left
                                     | System.Windows.Forms.AnchorStyles.Right;
            this.lblStatus.Location  = new System.Drawing.Point(200, 11);
            this.lblStatus.Size      = new System.Drawing.Size(600, 16);
            this.lblStatus.Name      = "lblStatus";
            this.lblStatus.Text      = "";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // ---- btnAction ----
            this.btnAction.Anchor   = System.Windows.Forms.AnchorStyles.Top
                                    | System.Windows.Forms.AnchorStyles.Right;
            this.btnAction.Enabled  = false;
            this.btnAction.Location = new System.Drawing.Point(910, 6);
            this.btnAction.Name     = "btnAction";
            this.btnAction.Size     = new System.Drawing.Size(100, 26);
            this.btnAction.TabIndex = 0;
            this.btnAction.Text     = "Connecting\u2026";
            this.btnAction.Click   += new System.EventHandler(this.btnAction_Click);

            // ---- rdpClient ----
            this.rdpClient.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.rdpClient.Enabled  = true;
            this.rdpClient.Name     = "rdpClient";
            this.rdpClient.OcxState = null;
            this.rdpClient.TabIndex = 1;

            // ---- SessionForm ----
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1024, 768);
            this.Controls.Add(this.rdpClient);
            this.Controls.Add(this.panelTop);
            this.MinimumSize         = new System.Drawing.Size(640, 400);
            this.Name                = "SessionForm";
            this.Text                = "RDP Session";

            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rdpClient)).EndInit();
            this.ResumeLayout(false);
        }

        // ---- field declarations ----
        private System.Windows.Forms.Panel  panelTop;
        private System.Windows.Forms.Label  lblServerInfo;
        private System.Windows.Forms.Label  lblStatus;
        private System.Windows.Forms.Button btnAction;
        private RdpSolution.UI.RdpClient.MsRdpClientControl rdpClient;
    }
}
