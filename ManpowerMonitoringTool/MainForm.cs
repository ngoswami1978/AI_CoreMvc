using ManpowerMonitoringTool.Models;
using ManpowerMonitoringTool.Services;
using System.ComponentModel;

namespace ManpowerMonitoringTool;

public sealed class MainForm : Form
{
    private readonly ExcelManpowerReader _reader = new();
    private readonly BindingList<ManpowerEntry> _entries = [];
    private readonly TextBox _excelPathTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly TextBox _urlTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "http://localhost:5000" };
    private readonly TextBox _unitSelectorTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "#unit_list" };
    private readonly TextBox _yearSelectorTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "#SelectedYear" };
    private readonly TextBox _monthSelectorTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "#SelectedMonth" };
    private readonly TextBox _searchSelectorTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "#btnSearch" };
    private readonly TextBox _tableSelectorTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "#MANPOWERCOST_NETSALES_TAB2" };
    private readonly CheckBox _keepBrowserOpenCheckBox = new() { Text = "Keep browser open after upload", Checked = true, AutoSize = true };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true, AllowUserToAddRows = false };
    private readonly TextBox _logTextBox = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
    private readonly Button _runButton = new() { Text = "Run Upload", AutoSize = true };
    private CancellationTokenSource? _cancellationTokenSource;
    private ManpowerSeleniumUploader? _uploader;

    public MainForm()
    {
        Text = "Manpower Cost Selenium Monitoring Tool";
        Width = 1220;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        Controls.Add(root);

        root.Controls.Add(BuildExcelPanel(), 0, 0);
        root.Controls.Add(BuildWebsitePanel(), 0, 1);
        root.Controls.Add(_grid, 0, 2);
        root.Controls.Add(_logTextBox, 0, 3);
        _grid.DataSource = _entries;
    }

    private Control BuildExcelPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var browseButton = new Button { Text = "Browse Excel", AutoSize = true };
        browseButton.Click += (_, _) => BrowseExcel();
        var loadButton = new Button { Text = "Load Preview", AutoSize = true };
        loadButton.Click += (_, _) => LoadExcel();

        panel.Controls.Add(new Label { Text = "Excel file", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        panel.Controls.Add(_excelPathTextBox, 1, 0);
        panel.Controls.Add(browseButton, 2, 0);
        panel.Controls.Add(loadButton, 3, 0);
        return panel;
    }

    private Control BuildWebsitePanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 6, AutoSize = true, Padding = new Padding(0, 8, 0, 8) };
        for (var i = 0; i < 6; i++)
        {
            panel.ColumnStyles.Add(i % 2 == 0 ? new ColumnStyle(SizeType.AutoSize) : new ColumnStyle(SizeType.Percent, 33));
        }

        AddField(panel, "Website URL", _urlTextBox, 0, 0);
        AddField(panel, "Unit selector", _unitSelectorTextBox, 2, 0);
        AddField(panel, "Year selector", _yearSelectorTextBox, 4, 0);
        AddField(panel, "Month selector", _monthSelectorTextBox, 0, 1);
        AddField(panel, "Search button", _searchSelectorTextBox, 2, 1);
        AddField(panel, "Cost table", _tableSelectorTextBox, 4, 1);

        var startButton = new Button { Text = "Start Browser", AutoSize = true };
        startButton.Click += (_, _) => StartBrowser();
        _runButton.Click += async (_, _) => await RunUploadAsync();
        var stopButton = new Button { Text = "Stop", AutoSize = true };
        stopButton.Click += (_, _) => _cancellationTokenSource?.Cancel();

        var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, AutoSize = true };
        actions.Controls.Add(startButton);
        actions.Controls.Add(_runButton);
        actions.Controls.Add(stopButton);
        actions.Controls.Add(_keepBrowserOpenCheckBox);
        panel.Controls.Add(actions, 0, 2);
        panel.SetColumnSpan(actions, 6);
        return panel;
    }

    private static void AddField(TableLayoutPanel panel, string label, Control control, int column, int row)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 8, 3) }, column, row);
        panel.Controls.Add(control, column + 1, row);
    }

    private void BrowseExcel()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select manpower Excel file",
            Filter = "Excel files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _excelPathTextBox.Text = dialog.FileName;
            LoadExcel();
        }
    }

    private void LoadExcel()
    {
        try
        {
            _entries.Clear();
            foreach (var entry in _reader.Read(_excelPathTextBox.Text))
            {
                _entries.Add(entry);
            }

            Log($"Loaded {_entries.Count} Excel row(s). The tool will write ACTUAL_MP_COST and ACTUAL_MP_COST_LEASING for each Function row.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Excel load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log($"Excel load failed: {ex.Message}");
        }
    }

    private void StartBrowser()
    {
        try
        {
            _uploader ??= new ManpowerSeleniumUploader(BuildOptions(), Log);
            _uploader.StartBrowser();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Browser start failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log($"Browser start failed: {ex.Message}");
        }
    }

    private async Task RunUploadAsync()
    {
        if (_entries.Count == 0)
        {
            LoadExcel();
        }

        if (_entries.Count == 0)
        {
            return;
        }

        _runButton.Enabled = false;
        _cancellationTokenSource = new CancellationTokenSource();
        try
        {
            _uploader ??= new ManpowerSeleniumUploader(BuildOptions(), Log);
            var rows = _entries.ToList();
            await Task.Run(() => _uploader.Upload(rows, _cancellationTokenSource.Token));
        }
        catch (OperationCanceledException)
        {
            Log("Upload cancelled by user.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Upload failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log($"Upload failed: {ex.Message}");
        }
        finally
        {
            _runButton.Enabled = true;
        }
    }

    private BrowserAutomationOptions BuildOptions()
    {
        return new BrowserAutomationOptions
        {
            Url = _urlTextBox.Text.Trim(),
            UnitSelector = _unitSelectorTextBox.Text.Trim(),
            YearSelector = _yearSelectorTextBox.Text.Trim(),
            MonthSelector = _monthSelectorTextBox.Text.Trim(),
            SearchButtonSelector = _searchSelectorTextBox.Text.Trim(),
            TableSelector = _tableSelectorTextBox.Text.Trim(),
            KeepBrowserOpen = _keepBrowserOpenCheckBox.Checked
        };
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Log(message));
            return;
        }

        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _uploader?.Dispose();
        base.OnFormClosed(e);
    }
}
