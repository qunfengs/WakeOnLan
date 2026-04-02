using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WakeOnLan.Models;
using WakeOnLan.Services;

namespace WakeOnLan.Forms;

public sealed class MainForm : Form
{
    private readonly BindingList<HostRecord> _hosts = new();
    private readonly BindingList<DiscoveredDevice> _scanResults = new();
    private readonly JsonStorageService _storageService = new();
    private readonly NetworkDiscoveryService _networkDiscoveryService = new();
    private readonly WakeOnLanService _wakeOnLanService = new();

    private readonly DataGridView _hostGrid = new();
    private readonly DataGridView _scanGrid = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Text = "就绪" };
    private readonly Label _networkLabel = new() { AutoSize = true, Text = "当前网络：检测中..." };
    private readonly Button _wakeButton = new() { Text = "唤醒", AutoSize = true };
    private readonly Button _editButton = new() { Text = "编辑", AutoSize = true };
    private readonly Button _deleteButton = new() { Text = "删除", AutoSize = true };
    private readonly Button _addManualButton = new() { Text = "手动添加", AutoSize = true };
    private readonly Button _scanButton = new() { Text = "开始扫描", AutoSize = true };
    private readonly Button _addScannedButton = new() { Text = "将所选设备加入主机列表", AutoSize = true };

    private NetworkContext? _networkContext;

    public MainForm()
    {
        Text = "Wake-on-LAN";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 560);
        Width = 980;
        Height = 640;

        BuildLayout();
        ConfigureGrids();
        Load += OnLoadAsync;
    }

    private void BuildLayout()
    {
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var hostTab = new TabPage("主机列表");
        var searchTab = new TabPage("搜索设备");

        hostTab.Controls.Add(BuildHostTab());
        searchTab.Controls.Add(BuildSearchTab());

        tabControl.TabPages.Add(hostTab);
        tabControl.TabPages.Add(searchTab);

        var footer = new StatusStrip();
        footer.Items.Add(_statusLabel);

        Controls.Add(tabControl);
        Controls.Add(footer);
    }

    private Control BuildHostTab()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var tipLabel = new Label
        {
            AutoSize = true,
            Text = "保存常用设备后可一键唤醒。MVP 默认隐藏高级网络参数。"
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        _wakeButton.Click += WakeSelectedHostAsync;
        _editButton.Click += EditSelectedHost;
        _deleteButton.Click += DeleteSelectedHost;
        _addManualButton.Click += AddHostManually;

        actions.Controls.Add(_wakeButton);
        actions.Controls.Add(_editButton);
        actions.Controls.Add(_deleteButton);
        actions.Controls.Add(_addManualButton);

        panel.Controls.Add(tipLabel, 0, 0);
        panel.Controls.Add(_hostGrid, 0, 1);
        panel.Controls.Add(actions, 0, 2);

        return panel;
    }

    private Control BuildSearchTab()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 4
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _scanButton.Click += StartScanAsync;
        _addScannedButton.Click += AddSelectedScannedDevice;

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        topPanel.Controls.Add(_scanButton);

        panel.Controls.Add(_networkLabel, 0, 0);
        panel.Controls.Add(topPanel, 0, 1);
        panel.Controls.Add(_scanGrid, 0, 2);
        panel.Controls.Add(_addScannedButton, 0, 3);

        return panel;
    }

    private void ConfigureGrids()
    {
        ConfigureGrid(_hostGrid);
        _hostGrid.DataSource = _hosts;
        _hostGrid.Columns.Add(CreateTextColumn(nameof(HostRecord.Name), "名称", 170));
        _hostGrid.Columns.Add(CreateTextColumn(nameof(HostRecord.HostName), "主机名或备注", 260));
        _hostGrid.Columns.Add(CreateTextColumn(nameof(HostRecord.MacAddress), "MAC 地址", 180));
        _hostGrid.Columns.Add(CreateTextColumn(nameof(HostRecord.LastKnownIp), "最近 IP", 140));

        ConfigureGrid(_scanGrid);
        _scanGrid.DataSource = _scanResults;
        _scanGrid.Columns.Add(CreateTextColumn(nameof(DiscoveredDevice.HostName), "主机名", 280));
        _scanGrid.Columns.Add(CreateTextColumn(nameof(DiscoveredDevice.IpAddress), "IP 地址", 150));
        _scanGrid.Columns.Add(CreateTextColumn(nameof(DiscoveredDevice.MacAddress), "MAC 地址", 180));
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string title, int width)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = title,
            Width = width,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
    }

    private async void OnLoadAsync(object? sender, EventArgs e)
    {
        LoadHosts();
        await RefreshNetworkContextAsync();
    }

    private void LoadHosts()
    {
        _hosts.Clear();
        foreach (var host in _storageService.LoadHosts())
        {
            _hosts.Add(host);
        }

        UpdateStatus($"已加载 {_hosts.Count} 条主机记录。");
    }

    private async Task RefreshNetworkContextAsync()
    {
        await Task.Yield();

        try
        {
            _networkContext = _networkDiscoveryService.GetCurrentNetworkContext();
            _networkLabel.Text =
                $"当前网络：{_networkContext.AdapterName} | 本机 IP {_networkContext.LocalIpAddress} | 广播地址 {_networkContext.BroadcastAddress}";
        }
        catch (Exception ex)
        {
            _networkContext = null;
            _networkLabel.Text = $"当前网络：{ex.Message}";
        }
    }

    private async void StartScanAsync(object? sender, EventArgs e)
    {
        if (_networkContext is null)
        {
            await RefreshNetworkContextAsync();
            if (_networkContext is null)
            {
                MessageBox.Show(this, "未检测到可用的 IPv4 网络。", "扫描", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        try
        {
            ToggleScanControls(false);
            _scanResults.Clear();
            UpdateStatus("正在扫描当前子网中的在线设备...");

            var progress = new Progress<DiscoveredDevice>(device => _scanResults.Add(device));
            var devices = await _networkDiscoveryService.ScanAsync(_networkContext, progress);

            if (devices.Count != _scanResults.Count)
            {
                _scanResults.Clear();
                foreach (var device in devices)
                {
                    _scanResults.Add(device);
                }
            }

            UpdateStatus($"扫描完成，共发现 {_scanResults.Count} 台在线设备。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "扫描失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("扫描失败。");
        }
        finally
        {
            ToggleScanControls(true);
        }
    }

    private void ToggleScanControls(bool enabled)
    {
        _scanButton.Enabled = enabled;
        _addScannedButton.Enabled = enabled;
    }

    private async void WakeSelectedHostAsync(object? sender, EventArgs e)
    {
        var host = GetSelectedHost();
        if (host is null)
        {
            MessageBox.Show(this, "请先选择一个主机。", "唤醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_networkContext is null)
        {
            await RefreshNetworkContextAsync();
            if (_networkContext is null)
            {
                MessageBox.Show(this, "未检测到可用的 IPv4 网络。", "唤醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        try
        {
            _wakeButton.Enabled = false;
            await _wakeOnLanService.WakeAsync(host.MacAddress, _networkContext.BroadcastAddress);
            host.LastWakeAt = DateTime.Now;
            _hostGrid.Refresh();
            SaveHosts();
            UpdateStatus($"已向 {host.NameOrFallback()} 发送唤醒包。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "唤醒失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("唤醒失败。");
        }
        finally
        {
            _wakeButton.Enabled = true;
        }
    }

    private void AddHostManually(object? sender, EventArgs e)
    {
        using var dialog = new HostEditForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        UpsertHost(dialog.Host);
    }

    private void EditSelectedHost(object? sender, EventArgs e)
    {
        var selected = GetSelectedHost();
        if (selected is null)
        {
            MessageBox.Show(this, "请先选择一个主机。", "编辑", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new HostEditForm(selected);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var index = _hosts.IndexOf(selected);
        _hosts[index] = dialog.Host;
        SaveHosts();
        _hostGrid.Refresh();
        UpdateStatus("主机已更新。");
    }

    private void DeleteSelectedHost(object? sender, EventArgs e)
    {
        var selected = GetSelectedHost();
        if (selected is null)
        {
            MessageBox.Show(this, "请先选择一个主机。", "删除", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"确定删除 {selected.NameOrFallback()} 吗？",
            "删除主机",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _hosts.Remove(selected);
        SaveHosts();
        UpdateStatus("主机已删除。");
    }

    private void AddSelectedScannedDevice(object? sender, EventArgs e)
    {
        var device = GetSelectedScannedDevice();
        if (device is null)
        {
            MessageBox.Show(this, "请先选择一个扫描到的设备。", "添加设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var candidate = new HostRecord
        {
            Name = string.IsNullOrWhiteSpace(device.HostName) ? device.IpAddress : device.HostName,
            HostName = device.HostName,
            MacAddress = device.MacAddress,
            LastKnownIp = device.IpAddress,
            LastSeenAt = device.DiscoveredAt
        };

        using var dialog = new HostEditForm(candidate);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        UpsertHost(dialog.Host);
    }

    private void UpsertHost(HostRecord host)
    {
        var existing = _hosts.FirstOrDefault(x =>
            x.Id == host.Id ||
            string.Equals(x.MacAddress, host.MacAddress, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            var index = _hosts.IndexOf(existing);
            _hosts[index] = host;
            UpdateStatus("主机已更新。");
        }
        else
        {
            _hosts.Add(host);
            UpdateStatus("主机已添加。");
        }

        SaveHosts();
        _hostGrid.Refresh();
    }

    private void SaveHosts()
    {
        _storageService.SaveHosts(_hosts);
    }

    private HostRecord? GetSelectedHost()
    {
        return _hostGrid.CurrentRow?.DataBoundItem as HostRecord;
    }

    private DiscoveredDevice? GetSelectedScannedDevice()
    {
        return _scanGrid.CurrentRow?.DataBoundItem as DiscoveredDevice;
    }

    private void UpdateStatus(string message)
    {
        _statusLabel.Text = message;
    }
}

internal static class HostRecordExtensions
{
    public static string NameOrFallback(this HostRecord host)
    {
        if (!string.IsNullOrWhiteSpace(host.Name))
        {
            return host.Name;
        }

        if (!string.IsNullOrWhiteSpace(host.HostName))
        {
            return host.HostName;
        }

        if (!string.IsNullOrWhiteSpace(host.LastKnownIp))
        {
            return host.LastKnownIp;
        }

        return host.MacAddress;
    }
}
