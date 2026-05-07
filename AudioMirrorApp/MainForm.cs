namespace AudioMirrorApp;

internal sealed class MainForm : Form
{
    private readonly ComboBox sourceBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox firstTargetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox secondTargetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown firstGainBox = new() { DecimalPlaces = 2, Increment = 0.25M, Minimum = 0.01M, Maximum = 8M, Width = 90 };
    private readonly NumericUpDown secondGainBox = new() { DecimalPlaces = 2, Increment = 0.25M, Minimum = 0.01M, Maximum = 8M, Width = 90 };
    private readonly NumericUpDown firstDelayBox = new() { Minimum = 0, Maximum = 2000, Increment = 5, Width = 90 };
    private readonly NumericUpDown secondDelayBox = new() { Minimum = 0, Maximum = 2000, Increment = 5, Width = 90 };
    private readonly Button refreshButton = new() { Text = "Refresh" };
    private readonly Button startButton = new() { Text = "Start" };
    private readonly Button stopButton = new() { Text = "Stop", Enabled = false };
    private readonly Button saveButton = new() { Text = "Save" };
    private readonly Button startupButton = new() { Text = "Autostart" };
    private readonly Label statusLabel = new() { AutoSize = false, Height = 76, Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer statsTimer = new() { Interval = 500 };
    private readonly AppSettings settings;
    private readonly bool startAfterShown;
    private IReadOnlyList<AudioDeviceInfo> devices = [];
    private WasapiMirrorEngine? engine;

    public MainForm(bool startAfterShown)
    {
        this.startAfterShown = startAfterShown;
        Text = "AudioMirror";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 760;
        Height = 360;
        MinimumSize = new Size(680, 320);

        settings = SettingsStore.Load();
        BuildLayout();
        WireEvents();
        RefreshDevices();
        ApplySettingsToControls();
        Shown += (_, _) =>
        {
            if (this.startAfterShown)
            {
                StartMirror();
            }
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        engine?.Dispose();
        base.OnFormClosing(e);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14),
            AutoSize = false
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = 4,
            AutoSize = true
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        AddRow(grid, 0, "Source", sourceBox, null, null);
        AddRow(grid, 1, "Target 1", firstTargetBox, firstGainBox, firstDelayBox);
        AddRow(grid, 2, "Target 2", secondTargetBox, secondGainBox, secondDelayBox);

        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 3);
        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 1, 3);
        grid.Controls.Add(new Label { Text = "Gain", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 2, 3);
        grid.Controls.Add(new Label { Text = "Delay ms", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 3, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 12, 0, 8)
        };
        buttons.Controls.AddRange([refreshButton, startButton, stopButton, saveButton, startupButton]);

        var hint = new Label
        {
            Text = "Set Windows default output to the selected Source, restart the player if captured frames stay at 0.",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8)
        };

        statusLabel.BorderStyle = BorderStyle.FixedSingle;
        statusLabel.Padding = new Padding(8);

        root.Controls.Add(grid, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        root.Controls.Add(hint, 0, 2);
        root.Controls.Add(statusLabel, 0, 3);
        Controls.Add(root);
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control deviceControl, Control? gainControl, Control? delayControl)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) }, 0, row);
        deviceControl.Dock = DockStyle.Fill;
        grid.Controls.Add(deviceControl, 1, row);

        if (gainControl is not null)
        {
            grid.Controls.Add(gainControl, 2, row);
        }

        if (delayControl is not null)
        {
            grid.Controls.Add(delayControl, 3, row);
        }
    }

    private void WireEvents()
    {
        refreshButton.Click += (_, _) => RefreshDevices();
        startButton.Click += (_, _) => StartMirror();
        stopButton.Click += (_, _) => StopMirror();
        saveButton.Click += (_, _) => SaveSettingsFromControls();
        startupButton.Click += (_, _) => RegisterStartup();
        firstGainBox.ValueChanged += (_, _) => PushLiveSettings();
        secondGainBox.ValueChanged += (_, _) => PushLiveSettings();
        firstDelayBox.ValueChanged += (_, _) => PushLiveSettings();
        secondDelayBox.ValueChanged += (_, _) => PushLiveSettings();
        statsTimer.Tick += (_, _) => UpdateStatus();
    }

    private void RefreshDevices()
    {
        devices = CoreAudio.GetRenderDevices();
        FillDeviceBox(sourceBox);
        FillDeviceBox(firstTargetBox);
        FillDeviceBox(secondTargetBox);
        ApplySettingsToControls();
        statusLabel.Text = "Devices refreshed.";
    }

    private void FillDeviceBox(ComboBox box)
    {
        box.Items.Clear();
        foreach (var device in devices)
        {
            box.Items.Add(device);
        }
    }

    private void ApplySettingsToControls()
    {
        SelectIndex(sourceBox, settings.SourceIndex);
        SelectIndex(firstTargetBox, settings.FirstTargetIndex);
        SelectIndex(secondTargetBox, settings.SecondTargetIndex);
        firstGainBox.Value = ClampDecimal((decimal)settings.FirstGain, firstGainBox.Minimum, firstGainBox.Maximum);
        secondGainBox.Value = ClampDecimal((decimal)settings.SecondGain, secondGainBox.Minimum, secondGainBox.Maximum);
        firstDelayBox.Value = ClampDecimal(settings.FirstDelayMs, firstDelayBox.Minimum, firstDelayBox.Maximum);
        secondDelayBox.Value = ClampDecimal(settings.SecondDelayMs, secondDelayBox.Minimum, secondDelayBox.Maximum);
    }

    private static decimal ClampDecimal(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static void SelectIndex(ComboBox box, int deviceIndex)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is AudioDeviceInfo device && device.Index == deviceIndex)
            {
                box.SelectedIndex = i;
                return;
            }
        }

        if (box.Items.Count > 0)
        {
            box.SelectedIndex = 0;
        }
    }

    private void StartMirror()
    {
        try
        {
            StopMirror();
            var source = SelectedDevice(sourceBox);
            var firstTarget = SelectedDevice(firstTargetBox);
            var secondTarget = SelectedDevice(secondTargetBox);
            engine = new WasapiMirrorEngine(
                source,
                firstTarget,
                secondTarget,
                (double)firstGainBox.Value,
                (double)secondGainBox.Value,
                (int)firstDelayBox.Value,
                (int)secondDelayBox.Value);

            startButton.Enabled = false;
            stopButton.Enabled = true;
            statsTimer.Start();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Start failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            StopMirror();
        }
    }

    private void StopMirror()
    {
        statsTimer.Stop();
        engine?.Dispose();
        engine = null;
        startButton.Enabled = true;
        stopButton.Enabled = false;
        UpdateStatus();
    }

    private void PushLiveSettings()
    {
        engine?.UpdateSettings(
            (double)firstGainBox.Value,
            (double)secondGainBox.Value,
            (int)firstDelayBox.Value,
            (int)secondDelayBox.Value);
    }

    private void SaveSettingsFromControls()
    {
        var snapshot = ReadSettingsFromControls();
        SettingsStore.Save(snapshot);
        statusLabel.Text = $"Saved: {SettingsStore.SettingsPath}";
    }

    private AppSettings ReadSettingsFromControls()
    {
        return new AppSettings
        {
            SourceIndex = SelectedDevice(sourceBox).Index,
            FirstTargetIndex = SelectedDevice(firstTargetBox).Index,
            SecondTargetIndex = SelectedDevice(secondTargetBox).Index,
            FirstGain = (double)firstGainBox.Value,
            SecondGain = (double)secondGainBox.Value,
            FirstDelayMs = (int)firstDelayBox.Value,
            SecondDelayMs = (int)secondDelayBox.Value
        };
    }

    private AudioDeviceInfo SelectedDevice(ComboBox box)
    {
        if (box.SelectedItem is AudioDeviceInfo device)
        {
            return device;
        }

        throw new InvalidOperationException("Select all devices first.");
    }

    private void UpdateStatus()
    {
        if (engine is null)
        {
            statusLabel.Text = "Stopped.";
            return;
        }

        var error = engine.LastError is null ? "" : $"{Environment.NewLine}Error: {engine.LastError.Message}";
        statusLabel.Text =
            $"Running: {engine.SourceName} -> {engine.FirstTargetName}, {engine.SecondTargetName}{Environment.NewLine}" +
            $"Format: {engine.Format.SampleRate} Hz, {engine.Format.Bits} bit{Environment.NewLine}" +
            $"Packets {engine.Packets}, captured {engine.CapturedFrames}, T1 written {engine.FirstWrittenFrames}, dropped {engine.FirstDroppedFrames}, T2 written {engine.SecondWrittenFrames}, dropped {engine.SecondDroppedFrames}" +
            error;
    }

    private void RegisterStartup()
    {
        try
        {
            SaveSettingsFromControls();
            var appPath = Application.ExecutablePath;
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true)
                ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            key.SetValue("AudioMirror", $"\"{appPath}\" --start");
            statusLabel.Text = "Autostart registered. Settings saved.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Autostart failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
