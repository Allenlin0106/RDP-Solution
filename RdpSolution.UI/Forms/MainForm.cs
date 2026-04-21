using System;
using System.Configuration;
using System.IO;
using System.Windows.Forms;
using RdpSolution.BLL.Interfaces;
using RdpSolution.BLL.Services;
using RdpSolution.DAL.Interfaces;
using RdpSolution.DAL.Models;
using RdpSolution.DAL.Repositories;

namespace RdpSolution.UI.Forms
{
    public partial class MainForm : Form
    {
        private readonly IHostConfigRepository _repository;
        private readonly IConnectionService    _connectionService;

        public MainForm()
        {
            InitializeComponent();

            var rawPath    = ConfigurationManager.AppSettings["HostsConfigPath"] ?? "hosts.xml";
            var configPath = Path.IsPathRooted(rawPath)
                ? rawPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath);

            _repository        = new XmlHostConfigRepository(configPath);
            _connectionService = new ConnectionService();

            LoadHosts();
        }

        // ------------------------------------------------------------------ data binding

        private void LoadHosts()
        {
            listViewHosts.Items.Clear();

            foreach (var host in _repository.GetAll())
                listViewHosts.Items.Add(BuildListItem(host));

            UpdateButtonStates();
        }

        private static ListViewItem BuildListItem(RemoteHostConfig host)
        {
            var item = new ListViewItem(host.Name);
            item.SubItems.Add(host.Hostname);
            item.SubItems.Add(host.Port.ToString());
            item.SubItems.Add(host.Username);
            item.SubItems.Add(host.Domain);
            item.Tag = host.Id;
            return item;
        }

        private RemoteHostConfig SelectedHost()
        {
            if (listViewHosts.SelectedItems.Count == 0)
                return null;

            var id = listViewHosts.SelectedItems[0].Tag as string;
            return _repository.GetById(id);
        }

        private void UpdateButtonStates()
        {
            bool sel = listViewHosts.SelectedItems.Count > 0;

            btnConnect.Enabled                   = sel;
            btnEdit.Enabled                      = sel;
            btnDelete.Enabled                    = sel;
            connectToolStripMenuItem.Enabled     = sel;
            editHostToolStripMenuItem.Enabled    = sel;
            deleteHostToolStripMenuItem.Enabled  = sel;
            exportRdpToolStripMenuItem.Enabled   = sel;
        }

        // ------------------------------------------------------------------ actions

        private SessionTabsForm _sessionTabs;

        private void ConnectToSelected()
        {
            var host = SelectedHost();
            if (host == null) return;

            if (_sessionTabs == null || _sessionTabs.IsDisposed)
            {
                _sessionTabs = new SessionTabsForm();
                _sessionTabs.Show(this);
            }
            else
            {
                if (!_sessionTabs.Visible) _sessionTabs.Show();
                _sessionTabs.BringToFront();
            }

            _sessionTabs.AddSession(host);
            SetStatus("Session opened: " + host.Name);
        }

        private void AddHost()
        {
            using (var dlg = new HostEditorForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                _repository.Add(dlg.HostConfig);
                _repository.Save();
                LoadHosts();
                SetStatus("Added: " + dlg.HostConfig.Name);
            }
        }

        private void EditSelected()
        {
            var host = SelectedHost();
            if (host == null) return;

            using (var dlg = new HostEditorForm(host))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                _repository.Update(dlg.HostConfig);
                _repository.Save();
                LoadHosts();
                SetStatus("Updated: " + dlg.HostConfig.Name);
            }
        }

        private void DeleteSelected()
        {
            var host = SelectedHost();
            if (host == null) return;

            var answer = MessageBox.Show(
                "Delete '" + host.Name + "'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
                return;

            _repository.Delete(host.Id);
            _repository.Save();
            LoadHosts();
            SetStatus("Deleted: " + host.Name);
        }

        private void ExportRdpSelected()
        {
            var host = SelectedHost();
            if (host == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title            = "Export RDP File";
                dlg.Filter           = "RDP Files (*.rdp)|*.rdp|All Files (*.*)|*.*";
                dlg.FileName         = host.Name + ".rdp";
                dlg.DefaultExt       = "rdp";
                dlg.OverwritePrompt  = true;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                var content = _connectionService.GenerateRdpFileContent(host);
                File.WriteAllText(dlg.FileName, content, System.Text.Encoding.UTF8);
                SetStatus("Exported: " + dlg.FileName);
            }
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }

        // ------------------------------------------------------------------ event handlers

        private void btnConnect_Click(object sender, EventArgs e)  => ConnectToSelected();
        private void btnAdd_Click(object sender, EventArgs e)      => AddHost();
        private void btnEdit_Click(object sender, EventArgs e)     => EditSelected();
        private void btnDelete_Click(object sender, EventArgs e)   => DeleteSelected();

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)     => ConnectToSelected();
        private void addHostToolStripMenuItem_Click(object sender, EventArgs e)     => AddHost();
        private void editHostToolStripMenuItem_Click(object sender, EventArgs e)    => EditSelected();
        private void deleteHostToolStripMenuItem_Click(object sender, EventArgs e)  => DeleteSelected();
        private void exportRdpToolStripMenuItem_Click(object sender, EventArgs e)   => ExportRdpSelected();
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)        => Application.Exit();

        private void listViewHosts_SelectedIndexChanged(object sender, EventArgs e) => UpdateButtonStates();
        private void listViewHosts_DoubleClick(object sender, EventArgs e)          => ConnectToSelected();
    }
}
