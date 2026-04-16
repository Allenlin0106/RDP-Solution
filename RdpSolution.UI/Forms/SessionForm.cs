using System;
using System.Windows.Forms;
using RdpSolution.DAL.Models;
using RdpSolution.UI.RdpClient;

namespace RdpSolution.UI.Forms
{
    /// <summary>
    /// Non-modal window that embeds a live RDP session.
    /// The RDP ActiveX control fills the client area; a slim top bar shows
    /// connection info, live status, and Disconnect / Reconnect buttons.
    /// </summary>
    public partial class SessionForm : Form
    {
        private readonly RemoteHostConfig _host;
        private bool _intentionalDisconnect;

        public SessionForm(RemoteHostConfig host)
        {
            _host = host ?? throw new ArgumentNullException("host");
            InitializeComponent();
            Text = "RDP \u2013 " + host.Name;
        }

        // ------------------------------------------------------------------ form events

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            rdpClient.RdpConnected    += OnRdpConnected;
            rdpClient.RdpDisconnected += OnRdpDisconnected;

            try
            {
                ApplyHostConfig();
                SetStatus("Connecting\u2026", connecting: true);
                rdpClient.Connect();
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message, connecting: false);
                MessageBox.Show(
                    "Failed to initialize the RDP client ActiveX control.\n\n" + ex.Message +
                    "\n\nEnsure mstscax.dll is registered on this system (it ships with Windows).",
                    "RDP Client Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _intentionalDisconnect = true;
            if (rdpClient.ConnectedState != 0)
                rdpClient.Disconnect();
            base.OnFormClosing(e);
        }

        // ------------------------------------------------------------------ RDP event handlers

        private void OnRdpConnected(object sender, EventArgs e)
        {
            SetStatus("Connected", connecting: false);
            btnAction.Text    = "&Disconnect";
            btnAction.Enabled = true;
        }

        private void OnRdpDisconnected(object sender, RdpDisconnectedEventArgs e)
        {
            if (_intentionalDisconnect) return;
            SetStatus("Disconnected \u2013 " + e.ReasonDescription, connecting: false);
            btnAction.Text    = "&Reconnect";
            btnAction.Enabled = true;
        }

        // ------------------------------------------------------------------ button handler

        private void btnAction_Click(object sender, EventArgs e)
        {
            if (rdpClient.ConnectedState == 0)
            {
                // Currently disconnected – reconnect
                _intentionalDisconnect = false;
                SetStatus("Reconnecting\u2026", connecting: true);
                rdpClient.Connect();
            }
            else
            {
                // Currently connected (or connecting) – disconnect
                _intentionalDisconnect = true;
                rdpClient.Disconnect();
                SetStatus("Disconnecting\u2026", connecting: false);
                btnAction.Text    = "&Reconnect";
                btnAction.Enabled = true;
            }
        }

        // ------------------------------------------------------------------ helpers

        private void ApplyHostConfig()
        {
            rdpClient.Server        = _host.Hostname;
            rdpClient.Domain        = _host.Domain   ?? string.Empty;
            rdpClient.UserName      = _host.Username ?? string.Empty;
            rdpClient.DesktopWidth  = _host.Width;
            rdpClient.DesktopHeight = _host.Height;
            rdpClient.FullScreen    = _host.FullScreen;
            rdpClient.SetPort(_host.Port);
            rdpClient.SetRedirectDrives(_host.AttachDrives);
            rdpClient.SetRedirectPrinters(_host.AttachPrinters);
            rdpClient.SetRedirectClipboard(true);
            rdpClient.SetSmartResize(true);

            lblServerInfo.Text = string.IsNullOrEmpty(_host.Domain)
                ? _host.Hostname + ":" + _host.Port
                : _host.Domain + "\\" + _host.Username + " @ " + _host.Hostname + ":" + _host.Port;
        }

        private void SetStatus(string message, bool connecting)
        {
            lblStatus.Text    = message;
            btnAction.Enabled = !connecting;
            btnAction.Text    = connecting ? "Connecting\u2026" : "&Disconnect";
        }
    }
}
