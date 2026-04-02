using System.Drawing;
using System.Windows.Forms;
using WakeOnLan.Models;

namespace WakeOnLan.Forms;

public sealed class HostEditForm : Form
{
    private readonly TextBox _nameTextBox = new() { Width = 320 };
    private readonly TextBox _hostNameTextBox = new() { Width = 320 };
    private readonly TextBox _macTextBox = new() { Width = 320 };
    private readonly TextBox _ipTextBox = new() { Width = 320 };
    private readonly TextBox _remarkTextBox = new() { Width = 320, Multiline = true, Height = 80 };

    public HostRecord Host { get; }

    public HostEditForm(HostRecord? host = null)
    {
        Host = host is null
            ? new HostRecord()
            : new HostRecord
            {
                Id = host.Id,
                Name = host.Name,
                HostName = host.HostName,
                MacAddress = host.MacAddress,
                LastKnownIp = host.LastKnownIp,
                Remark = host.Remark,
                LastSeenAt = host.LastSeenAt,
                LastWakeAt = host.LastWakeAt
            };

        Text = host is null ? "添加主机" : "编辑主机";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 310);

        BuildLayout();
        LoadValues();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 6,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "名称", _nameTextBox);
        AddRow(layout, 1, "主机名", _hostNameTextBox);
        AddRow(layout, 2, "MAC", _macTextBox);
        AddRow(layout, 3, "IP", _ipTextBox);
        AddRow(layout, 4, "备注", _remarkTextBox);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };

        var saveButton = new Button
        {
            Text = "保存",
            AutoSize = true
        };
        saveButton.Click += (_, _) => SaveAndClose();

        var cancelButton = new Button
        {
            Text = "取消",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);

        layout.Controls.Add(buttonPanel, 0, 5);
        layout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void LoadValues()
    {
        _nameTextBox.Text = Host.Name;
        _hostNameTextBox.Text = Host.HostName;
        _macTextBox.Text = Host.MacAddress;
        _ipTextBox.Text = Host.LastKnownIp;
        _remarkTextBox.Text = Host.Remark;
    }

    private void SaveAndClose()
    {
        var mac = _macTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(mac))
        {
            MessageBox.Show(this, "MAC 地址不能为空。", "校验", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Host.Name = _nameTextBox.Text.Trim();
        Host.HostName = _hostNameTextBox.Text.Trim();
        Host.MacAddress = mac.ToUpperInvariant();
        Host.LastKnownIp = _ipTextBox.Text.Trim();
        Host.Remark = _remarkTextBox.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }

    private static void AddRow(TableLayoutPanel layout, int rowIndex, string labelText, Control input)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        }, 0, rowIndex);
        layout.Controls.Add(input, 1, rowIndex);
    }
}
