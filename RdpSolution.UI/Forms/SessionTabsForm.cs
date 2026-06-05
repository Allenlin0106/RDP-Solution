using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RdpSolution.DAL.Models;
using RdpSolution.UI.RdpClient;
using RdpSolution.UI.SessionClient;
using RdpSolution.UI.VncClient;

namespace RdpSolution.UI.Forms
{
    /// <summary>
    /// Non-modal window that hosts all active remote sessions as pages in a TabControl.
    /// Each tab contains one <see cref="IRemoteControl"/> (either RDP or VNC).
    /// </summary>
    public partial class SessionTabsForm : Form
    {
        private readonly List<SessionEntry> _entries = new List<SessionEntry>();

        public SessionTabsForm()
        {
            InitializeComponent();
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Opens a new tab, connects, and switches to it.</summary>
        public void AddSession(RemoteHostConfig host)
        {
            var page  = new TabPage();
            var entry = new SessionEntry(host, page, this);
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

        private void CloseEntry(SessionEntry entry)
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

        private SessionEntry CurrentEntry =>
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

        // ------------------------------------------------------------------ called by SessionEntry

        internal void OnEntryChanged(SessionEntry entry)
        {
            entry.Page.Text = entry.TabTitle;
            if (entry == CurrentEntry)
                SyncToolbar();
        }

        internal void OnCloseRequested(SessionEntry entry) => CloseEntry(entry);

        // ====================================================================
        // Inner class: manages one tab page + one IRemoteControl
        // ====================================================================

        internal sealed class SessionEntry : IDisposable
        {
            // ---- public surface ----
            public TabPage Page          { get; }
            public string  TabTitle      { get; private set; }
            public string  StatusText    { get; private set; }
            public int     ConnectedState => _client?.ConnectedState ?? 0;

            // ---- private state ----
            private readonly RemoteHostConfig _host;
            private readonly SessionTabsForm  _owner;
            private readonly IRemoteControl   _client;
            private readonly Label  _lblInfo;
            private readonly Label  _lblStatus;
            private readonly Button _btnAction;
            private bool _intentionalDisconnect;

            public SessionEntry(RemoteHostConfig host, TabPage page, SessionTabsForm owner)
            {
                _host  = host;
                _owner = owner;
                Page   = page;

                TabTitle   = "◌ " + host.Name;
                StatusText = "Connecting…";

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
                    Text      = "Connecting…"
                };

                var btnClose = new Button
                {
                    Dock = DockStyle.Right, Width = 72, Text = "✕ Close"
                };
                btnClose.Click += (s, e) => _owner.OnCloseRequested(this);

                _btnAction = new Button
                {
                    Dock    = DockStyle.Right,
                    Width   = 108,
                    Text    = "Connecting…",
                    Enabled = false
                };
                _btnAction.Click += BtnAction_Click;

                panel.Controls.Add(btnClose);
                panel.Controls.Add(_btnAction);
                panel.Controls.Add(_lblStatus);
                panel.Controls.Add(_lblInfo);

                // ---- protocol-specific control ----
                if (host.Protocol == ConnectionProtocol.VNC)
                    _client = new VncClientControl { Dock = DockStyle.Fill };
                else
                    _client = new MsRdpClientControl { Dock = DockStyle.Fill };

                _client.Connected    += (s, e) => OnConnected();
                _client.Disconnected += (s, e) => OnDisconnected(e);
                _client.CreateFailed += (s, e) => ShowCreateError(e.Message);

                page.Controls.Add((Control)_client);
                page.Controls.Add(panel);
            }

            // ---- connection ----

            public void Connect()
            {
                _intentionalDisconnect = false;
                SetState("◌ " + _host.Name, "Connecting…",
                    actionText: "Connecting…", actionEnabled: false);
                _client.Connect(_host);
            }

            public void RequestDisconnect()
            {
                _intentionalDisconnect = true;
                if (_client.ConnectedState != 0)
                    _client.Disconnect();
            }

            // ---- events ----

            private void OnConnected()
            {
                SetState(
                    "● " + _host.Name,
                    "Connected  –  " + _host.Hostname + ":" + _host.Port,
                    actionText: "&Disconnect",
                    actionEnabled: true);
            }

            private void OnDisconnected(DisconnectedEventArgs e)
            {
                if (_intentionalDisconnect) return;
                SetState(
                    "○ " + _host.Name,
                    "Disconnected  –  " + e.Reason,
                    actionText: "&Reconnect",
                    actionEnabled: true);
            }

            private void ShowCreateError(string message)
            {
                ((Control)_client).Visible = false;
                var lbl = new Label
                {
                    Dock      = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    ForeColor = System.Drawing.Color.Firebrick,
                    Text      = "Control unavailable:\n\n" + message
                };
                Page.Controls.Add(lbl);
                SetState("⚠ " + _host.Name, "Error: control unavailable",
                    actionText: "N/A", actionEnabled: false);
            }

            private void BtnAction_Click(object sender, EventArgs e)
            {
                if (_client.ConnectedState == 0)
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

            public void Dispose() => ((Control)_client)?.Dispose();
        }
    }
}
