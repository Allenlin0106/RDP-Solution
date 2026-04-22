using System;
using System.Windows.Forms;
using RdpSolution.DAL.Models;

namespace RdpSolution.UI.Forms
{
    /// <summary>
    /// Modal dialog for adding or editing a <see cref="RemoteHostConfig"/> entry.
    /// </summary>
    public partial class HostEditorForm : Form
    {
        /// <summary>
        /// The host configuration produced (or modified) by this dialog.
        /// Valid only when <see cref="DialogResult"/> is <see cref="DialogResult.OK"/>.
        /// </summary>
        public RemoteHostConfig HostConfig { get; private set; }

        // ------------------------------------------------------------------ ctor

        /// <summary>Opens the dialog in Add mode with default values.</summary>
        public HostEditorForm()
        {
            InitializeComponent();
            HostConfig = new RemoteHostConfig();
            nudPort.Value       = HostConfig.Port;
            nudWidth.Value      = HostConfig.Width;
            nudHeight.Value     = HostConfig.Height;
            SelectColorDepth(HostConfig.ColorDepth);
            OnProtocolChanged(null, EventArgs.Empty);
        }

        /// <summary>Opens the dialog in Edit mode pre-populated from <paramref name="config"/>.</summary>
        public HostEditorForm(RemoteHostConfig config) : this()
        {
            HostConfig = config ?? throw new ArgumentNullException("config");
            Text       = "Edit Host";
            PopulateFields(config);
        }

        // ------------------------------------------------------------------ field population

        private void PopulateFields(RemoteHostConfig c)
        {
            txtName.Text        = c.Name     ?? string.Empty;
            txtHostname.Text    = c.Hostname ?? string.Empty;
            nudPort.Value       = c.Port;
            txtUsername.Text    = c.Username ?? string.Empty;
            txtDomain.Text      = c.Domain   ?? string.Empty;
            nudWidth.Value      = c.Width;
            nudHeight.Value     = c.Height;
            chkFullScreen.Checked     = c.FullScreen;
            chkAttachDrives.Checked   = c.AttachDrives;
            chkAttachPrinters.Checked = c.AttachPrinters;
            SelectColorDepth(c.ColorDepth);
            txtPassword.Text = c.Password ?? string.Empty;
            txtNotes.Text    = c.Notes    ?? string.Empty;

            cboProtocol.SelectedIndex    = c.Protocol    == ConnectionProtocol.VNC    ? 1 : 0;
            cboVncAuthType.SelectedIndex = c.VncAuthType == VncAuthType.MsLogon       ? 1 : 0;

            OnProtocolChanged(null, EventArgs.Empty);
            UpdateResolutionEnabled();
        }

        private void SelectColorDepth(int bpp)
        {
            var target = bpp.ToString();
            var idx    = cmbColorDepth.FindStringExact(target);
            cmbColorDepth.SelectedIndex = idx >= 0 ? idx : cmbColorDepth.Items.Count - 1;
        }

        // ------------------------------------------------------------------ UI helpers

        private void UpdateResolutionEnabled()
        {
            bool windowed = !chkFullScreen.Checked;
            nudWidth.Enabled  = windowed;
            nudHeight.Enabled = windowed;
            lblWidth.Enabled  = windowed;
            lblHeight.Enabled = windowed;
        }

        private void chkFullScreen_CheckedChanged(object sender, EventArgs e)
        {
            UpdateResolutionEnabled();
        }

        private void OnProtocolChanged(object sender, EventArgs e)
        {
            // Guard: can fire from InitializeComponent before all controls exist
            if (grpDisplay == null) return;

            bool isVnc = cboProtocol.SelectedIndex == 1;

            // Auto-switch port only when user actively changes protocol
            if (sender != null)
                nudPort.Value = isVnc ? 5900 : 3389;

            grpDisplay.Visible     = !isVnc;
            grpDevices.Visible     = !isVnc;
            lblVncAuthType.Visible = isVnc;
            cboVncAuthType.Visible = isVnc;

            OnVncAuthTypeChanged(null, EventArgs.Empty);
        }

        private void OnVncAuthTypeChanged(object sender, EventArgs e)
        {
            // Guard: can fire from InitializeComponent before all controls exist
            if (lblUsername == null) return;

            bool isVnc     = cboProtocol.SelectedIndex    == 1;
            bool isMsLogon = isVnc && cboVncAuthType.SelectedIndex == 1;

            // RDP: always show. VNC+MsLogon: show. VNC+VncPassword: hide.
            bool showUserDomain    = !isVnc || isMsLogon;
            lblUsername.Visible    = showUserDomain;
            txtUsername.Visible    = showUserDomain;
            lblDomain.Visible      = showUserDomain;
            txtDomain.Visible      = showUserDomain;
        }

        // ------------------------------------------------------------------ OK / Cancel

        private void btnOk_Click(object sender, EventArgs e)
        {
            string error;
            if (!ValidateInput(out error))
            {
                MessageBox.Show(error, "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ApplyToHostConfig();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // ------------------------------------------------------------------ validation / apply

        private bool ValidateInput(out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                error = "Display name is required.";
                txtName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtHostname.Text))
            {
                error = "Hostname or IP address is required.";
                txtHostname.Focus();
                return false;
            }

            return true;
        }

        private void ApplyToHostConfig()
        {
            HostConfig.Name     = txtName.Text.Trim();
            HostConfig.Hostname = txtHostname.Text.Trim();
            HostConfig.Port     = (int)nudPort.Value;
            HostConfig.Username = txtUsername.Text.Trim();
            HostConfig.Domain   = txtDomain.Text.Trim();
            HostConfig.Width    = (int)nudWidth.Value;
            HostConfig.Height   = (int)nudHeight.Value;
            HostConfig.FullScreen     = chkFullScreen.Checked;
            HostConfig.AttachDrives   = chkAttachDrives.Checked;
            HostConfig.AttachPrinters = chkAttachPrinters.Checked;
            HostConfig.Password       = txtPassword.Text;
            HostConfig.Notes          = txtNotes.Text.Trim();

            int bpp;
            HostConfig.ColorDepth = int.TryParse(
                cmbColorDepth.SelectedItem != null ? cmbColorDepth.SelectedItem.ToString() : "32",
                out bpp) ? bpp : 32;

            HostConfig.Protocol    = cboProtocol.SelectedIndex    == 1
                ? ConnectionProtocol.VNC  : ConnectionProtocol.RDP;
            HostConfig.VncAuthType = cboVncAuthType.SelectedIndex == 1
                ? VncAuthType.MsLogon     : VncAuthType.VncPassword;
        }
    }
}
