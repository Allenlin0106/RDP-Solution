using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RdpSolution.DAL.Models;
using RdpSolution.UI.RdpClient;

namespace RdpSolution.UI.Forms
{
    /// <summary>
    /// Non-modal window that hosts all active RDP sessions as pages in a TabControl.
    /// Call <see cref="AddSession"/> to open a new tab for a host; each tab
    /// contains one embedded <see cref="MsRdpClientControl"/>.
    /// </summary>
    public partial class SessionTabsForm : Form
    {
        private readonly List<RdpTabEntry> _entries = new List<RdpTabEntry>();

        public SessionTabsForm()
        {
            InitializeComponent();
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Opens a new tab, connects, and switches to it.</summary>
        public void AddSession(RemoteHostConfig host)
        {
            var page  = new TabPage();
            var entry = new RdpTabEntry(host, page, this);
            _entries.Add(entry);
            tabControl.TabPages.Add(page);
            tabControl.SelectedTab = page;
            entry.Connect();
        }

        // ------------------------------------------------------------------ toolbar handlers

        private void tsbDisconnect_Click(object sender, EventArgs e) => CurrentEntry?.RequestDisconnect();
        private void tsbReconnect_Click(object sender, EventArgs e)  => CurrentEntry?.Connect();
        private void tsbClose_Click(object sender, EventArgs e)      => CloseEntry(CurrentEntry);

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e) => SyncToolbar();

        // ------------------------------------------------------------------ close / cleanup

        private void CloseEntry(RdpTabEntry entry)
        {
            if (entry == null) return;
            entry.RequestDisconnect();
            _entries.Remove(entry);
            tabControl.TabPages.Remove(entry.Page);
            entry.Dispose();

            if (_entries.Count == 0)
                Close();
            else
                SyncToolbar();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var en in _entries.ToList())
            {
                en.RequestDisconnect();
                en.Dispose();
            }
            _entries.Clear();
            base.OnFormClosing(e);
        }

        // ------------------------------------------------------------------ helpers

        private RdpTabEntry CurrentEntry =>
            tabControl.SelectedTab != null
                ? _entries.FirstOrDefault(t => t.Page == tabControl.SelectedTab)
                : null;

        private void SyncToolbar()
        {
            var entry = CurrentEntry;
            bool hasEntry = entry != null;
            tsbDisconnect.Enabled = hasEntry && entry.ConnectedState != 0;
            tsbReconnect.Enabled  = hasEntry && entry.ConnectedState == 0;
            tsbClose.Enabled      = hasEntry;
            statusLabel.Text      = entry?.StatusText ?? string.Empty;
            tslCount.Text         = _entries.Count + " session(s)";
        }

        // ------------------------------------------------------------------ called by RdpTabEntry

        internal void OnEntryChanged(RdpTabEntry entry)
        {
            entry.Page.Text = entry.TabTitle;
            if (entry == CurrentEntry)
                SyncToolbar();
        }

        internal void OnCloseRequested(RdpTabEntry entry) => CloseEntry(entry);

        // ====================================================================
        // Inner class: manages one tab page + one RDP control
        // ====================================================================

        internal sealed class RdpTabEntry : IDisposable
        {
            // ---- public surface ----
            public TabPage    Page          { get; }
            public string     TabTitle      { get; private set; }
            public string     StatusText    { get; private set; }
            public int        ConnectedState => _rdp?.ConnectedState ?? 0;

            // ---- private state ----
            private readonly RemoteHostConfig _host;
            private readonly SessionTabsForm  _owner;
            private readonly MsRdpClientControl _rdp;
            private readonly Label  _lblInfo;
            private readonly Label  _lblStatus;
            private readonly Button _btnAction;
            private bool _intentionalDisconnect;

            public RdpTabEntry(RemoteHostConfig host, TabPage page, SessionTabsForm owner)
            {
                _host  = host;
                _owner = owner;
                Page   = page;

                TabTitle   = "\u25cc " + host.Name;   // ◌
                StatusText = "Connecting\u2026";

                // ---- top status bar ----
                var panel = new Panel { Dock = DockStyle.Top, Height = 34 };

                _lblInfo = new Label
                {
                    AutoSize  = false,
                    Dock      = DockStyle.Left,
                    Width     = 280,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Padding   = new Padding(6, 0, 0, 0),
                    Font      = new System.Drawing.Font("Consolas", 8.5F),
                    Text      = BuildInfoLabel()
                };

                _lblStatus = new Label
                {
                    Dock      = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Text      = "Connecting\u2026"
                };

                var btnClose = new Button
                {
                    Dock = DockStyle.Right, Width = 72, Text = "\u2715 Close"
                };
                btnClose.Click += (s, e) => _owner.OnCloseRequested(this);

                _btnAction = new Button
                {
                    Dock    = DockStyle.Right,
                    Width   = 108,
                    Text    = "Connecting\u2026",
                    Enabled = false
                };
                _btnAction.Click += BtnAction_Click;

                // Dock=Right controls: add in reverse visual order (rightmost first)
                panel.Controls.Add(btnClose);
                panel.Controls.Add(_btnAction);
                panel.Controls.Add(_lblStatus);
                panel.Controls.Add(_lblInfo);

                // ---- RDP control ----
                _rdp = new MsRdpClientControl { Dock = DockStyle.Fill };
                _rdp.RdpConnected    += (s, e) => OnConnected();
                _rdp.RdpDisconnected += (s, e) => OnDisconnected(e);

                // Controls.Add: Fill must come before Top so docking resolves correctly
                page.Controls.Add(_rdp);
                page.Controls.Add(panel);
            }

            // ---- connection ----

            public void Connect()
            {
                _intentionalDisconnect = false;

                _rdp.Server        = _host.Hostname;
                _rdp.Domain        = _host.Domain   ?? string.Empty;
                _rdp.UserName      = _host.Username ?? string.Empty;
                _rdp.DesktopWidth  = _host.Width;
                _rdp.DesktopHeight = _host.Height;
                _rdp.FullScreen    = false;
                _rdp.SetPort(_host.Port);
                _rdp.SetRedirectDrives(_host.AttachDrives);
                _rdp.SetRedirectPrinters(_host.AttachPrinters);
                _rdp.SetRedirectClipboard(true);
                _rdp.SetSmartResize(true);

                if (!string.IsNullOrEmpty(_host.Password))
                    _rdp.SetPassword(_host.Password);

                SetState("\u25cc " + _host.Name, "Connecting\u2026", actionText: "Connecting\u2026", actionEnabled: false);
                _rdp.Connect();
            }

            public void RequestDisconnect()
            {
                _intentionalDisconnect = true;
                if (_rdp.ConnectedState != 0)
                    _rdp.Disconnect();
            }

            // ---- events ----

            private void OnConnected()
            {
                SetState(
                    "\u25cf " + _host.Name,    // ●
                    "Connected  \u2013  " + _host.Hostname + ":" + _host.Port,
                    actionText: "&Disconnect",
                    actionEnabled: true);
            }

            private void OnDisconnected(RdpDisconnectedEventArgs e)
            {
                if (_intentionalDisconnect) return;
                SetState(
                    "\u25cb " + _host.Name,    // ○
                    "Disconnected  \u2013  " + e.ReasonDescription,
                    actionText: "&Reconnect",
                    actionEnabled: true);
            }

            private void BtnAction_Click(object sender, EventArgs e)
            {
                if (_rdp.ConnectedState == 0)
                    Connect();
                else
                    RequestDisconnect();
            }

            private void SetState(string tab, string status, string actionText, bool actionEnabled)
            {
                TabTitle   = tab;
                StatusText = status;
                _lblStatus.Text    = status;
                _btnAction.Text    = actionText;
                _btnAction.Enabled = actionEnabled;
                _owner.OnEntryChanged(this);
            }

            private string BuildInfoLabel()
            {
                var user = string.IsNullOrEmpty(_host.Domain)
                    ? _host.Username
                    : _host.Domain + "\\" + _host.Username;
                return (string.IsNullOrEmpty(user) ? string.Empty : user + " @ ")
                       + _host.Hostname + ":" + _host.Port;
            }

            public void Dispose() => _rdp?.Dispose();
        }
    }
}
