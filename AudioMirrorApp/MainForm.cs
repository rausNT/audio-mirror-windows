namespace AudioMirrorApp;

using System.Diagnostics;

internal sealed class MainForm : Form
{
    private readonly ComboBox sourceBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox firstTargetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox secondTargetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly LevelMeter sourceMeter = new();
    private readonly LevelMeter firstMeter = new();
    private readonly LevelMeter secondMeter = new();
    private readonly NumericUpDown firstGainBox = new() { DecimalPlaces = 2, Increment = 0.25M, Minimum = 0.01M, Maximum = 8M, Width = 90 };
    private readonly NumericUpDown secondGainBox = new() { DecimalPlaces = 2, Increment = 0.25M, Minimum = 0.01M, Maximum = 8M, Width = 90 };
    private readonly NumericUpDown firstDelayBox = new() { Minimum = 0, Maximum = 2000, Increment = 5, Width = 90 };
    private readonly NumericUpDown secondDelayBox = new() { Minimum = 0, Maximum = 2000, Increment = 5, Width = 90 };
    private readonly Button refreshButton = new() { Text = "Refresh" };
    private readonly Button startButton = new() { Text = "Start" };
    private readonly Button stopButton = new() { Text = "Stop", Enabled = false };
    private readonly Button saveButton = new() { Text = "Save" };
    private readonly Button startupButton = new() { Text = "Autostart" };
    private readonly Button soundSettingsButton = new() { Text = "Sound settings" };
    private readonly Button syncButton = new() { Text = "Sync" };
    private readonly Button testButton = new() { Text = "Test" };
    private readonly CheckBox splitLeftRightBox = new() { Text = "Split L/R", AutoSize = true };
    private readonly Label formatLabel = new() { AutoSize = false, Height = 42, Dock = DockStyle.Fill };
    private readonly Label statusLabel = new() { AutoSize = false, Height = 76, Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer statsTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer meterTimer = new() { Interval = 60 };
    private readonly MenuStrip menuStrip = new();
    private readonly ToolStripMenuItem fileMenu = new("&File");
    private readonly ToolStripMenuItem actionsMenu = new("&Actions");
    private readonly ToolStripMenuItem helpMenu = new("&Help");
    private readonly ToolStripMenuItem menuStartItem = new("&Start");
    private readonly ToolStripMenuItem menuStopItem = new("S&top");
    private readonly ToolStripMenuItem menuSaveItem = new("&Save settings");
    private readonly ToolStripMenuItem menuAutostartItem = new("&Autostart");
    private readonly ToolStripMenuItem menuExitItem = new("E&xit");
    private readonly ToolStripMenuItem menuRefreshItem = new("&Refresh devices");
    private readonly ToolStripMenuItem menuSoundItem = new("&Sound settings");
    private readonly ToolStripMenuItem menuSyncItem = new("S&ync");
    private readonly ToolStripMenuItem menuTestItem = new("&Test speakers");
    private readonly ToolStripMenuItem menuSplitItem = new("Split &L/R") { CheckOnClick = true };
    private readonly ToolStripMenuItem menuHelpItem = new("&User help");
    private readonly ToolStripMenuItem menuAboutItem = new("&About AudioMirror");
    private readonly NotifyIcon notifyIcon = new();
    private readonly ContextMenuStrip trayMenu = new();
    private readonly ToolStripMenuItem trayOpenItem = new("&Open AudioMirror");
    private readonly ToolStripMenuItem trayStartItem = new("&Start");
    private readonly ToolStripMenuItem trayStopItem = new("S&top");
    private readonly ToolStripMenuItem trayTestItem = new("&Test speakers");
    private readonly ToolStripMenuItem traySoundItem = new("&Sound settings");
    private readonly ToolStripMenuItem trayHelpItem = new("&Help");
    private readonly ToolStripMenuItem trayExitItem = new("E&xit");
    private readonly AppSettings settings;
    private readonly bool startAfterShown;
    private bool allowExit;
    private IReadOnlyList<AudioDeviceInfo> devices = [];
    private WasapiMirrorEngine? engine;

    public MainForm(bool startAfterShown)
    {
        this.startAfterShown = startAfterShown;
        Text = "AudioMirror";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 420);
        MinimumSize = new Size(760, 430);

        settings = SettingsStore.Load();
        BuildLayout();
        BuildTrayMenu();
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
        if (!allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        engine?.Dispose();
        base.OnFormClosing(e);
    }

    private void BuildLayout()
    {
        BuildMenu();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(14),
            AutoSize = false
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 5,
            RowCount = 4,
            AutoSize = true
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 87));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        AddRow(grid, 0, sourceMeter, "Source", sourceBox, null, null);
        AddRow(grid, 1, firstMeter, "Target 1", firstTargetBox, firstGainBox, firstDelayBox);
        AddRow(grid, 2, secondMeter, "Target 2", secondTargetBox, secondGainBox, secondDelayBox);

        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 3);
        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 1, 3);
        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 2, 3);
        grid.Controls.Add(new Label { Text = "Gain", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 3, 3);
        grid.Controls.Add(new Label { Text = "Delay ms", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 4, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 12, 0, 8)
        };
        buttons.Controls.AddRange([refreshButton, startButton, stopButton, saveButton, startupButton, soundSettingsButton, syncButton, testButton, splitLeftRightBox]);

        var hint = new Label
        {
            Text = "Set Windows default output to the selected Source, restart the player if captured frames stay at 0.",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8)
        };

        statusLabel.BorderStyle = BorderStyle.FixedSingle;
        statusLabel.Padding = new Padding(8);
        formatLabel.Padding = new Padding(4, 6, 4, 4);

        root.Controls.Add(grid, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        root.Controls.Add(hint, 0, 2);
        root.Controls.Add(formatLabel, 0, 3);
        root.Controls.Add(statusLabel, 0, 4);
        Controls.Add(root);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
    }

    private void BuildMenu()
    {
        fileMenu.DropDownItems.AddRange([
            menuStartItem,
            menuStopItem,
            new ToolStripSeparator(),
            menuSaveItem,
            menuAutostartItem,
            new ToolStripSeparator(),
            menuExitItem
        ]);

        actionsMenu.DropDownItems.AddRange([
            menuRefreshItem,
            menuSoundItem,
            menuSyncItem,
            menuTestItem,
            new ToolStripSeparator(),
            menuSplitItem
        ]);

        helpMenu.DropDownItems.AddRange([
            menuHelpItem,
            menuAboutItem
        ]);

        menuStrip.Items.AddRange([fileMenu, actionsMenu, helpMenu]);
    }

    private void BuildTrayMenu()
    {
        trayMenu.Items.AddRange([
            trayOpenItem,
            new ToolStripSeparator(),
            trayStartItem,
            trayStopItem,
            trayTestItem,
            traySoundItem,
            trayHelpItem,
            new ToolStripSeparator(),
            trayExitItem
        ]);

        notifyIcon.Icon = Icon ?? SystemIcons.Application;
        notifyIcon.Text = "AudioMirror";
        notifyIcon.ContextMenuStrip = trayMenu;
        notifyIcon.Visible = true;
    }

    private static void AddRow(TableLayoutPanel grid, int row, LevelMeter meter, string label, Control deviceControl, Control? gainControl, Control? delayControl)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        meter.Margin = new Padding(0, 5, 6, 0);
        grid.Controls.Add(meter, 0, row);
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) }, 1, row);
        deviceControl.Dock = DockStyle.Fill;
        grid.Controls.Add(deviceControl, 2, row);

        if (gainControl is not null)
        {
            grid.Controls.Add(gainControl, 3, row);
        }

        if (delayControl is not null)
        {
            grid.Controls.Add(delayControl, 4, row);
        }
    }

    private void WireEvents()
    {
        refreshButton.Click += (_, _) => RefreshDevices();
        startButton.Click += (_, _) => StartMirror();
        stopButton.Click += (_, _) => StopMirror();
        saveButton.Click += (_, _) => SaveSettingsFromControls();
        startupButton.Click += (_, _) => RegisterStartup();
        soundSettingsButton.Click += (_, _) => OpenSoundSettings();
        syncButton.Click += (_, _) => SyncAppSettings();
        testButton.Click += (_, _) => OpenTestWindow();
        menuStartItem.Click += (_, _) => StartMirror();
        menuStopItem.Click += (_, _) => StopMirror();
        menuSaveItem.Click += (_, _) => SaveSettingsFromControls();
        menuAutostartItem.Click += (_, _) => RegisterStartup();
        menuExitItem.Click += (_, _) => ExitApplication();
        menuRefreshItem.Click += (_, _) => RefreshDevices();
        menuSoundItem.Click += (_, _) => OpenSoundSettings();
        menuSyncItem.Click += (_, _) => SyncAppSettings();
        menuTestItem.Click += (_, _) => OpenTestWindow();
        menuSplitItem.CheckedChanged += (_, _) =>
        {
            if (splitLeftRightBox.Checked != menuSplitItem.Checked)
            {
                splitLeftRightBox.Checked = menuSplitItem.Checked;
            }
        };
        menuHelpItem.Click += (_, _) => ShowHelp();
        menuAboutItem.Click += (_, _) => ShowAbout();
        trayOpenItem.Click += (_, _) => ShowMainWindow();
        trayStartItem.Click += (_, _) => StartMirror();
        trayStopItem.Click += (_, _) => StopMirror();
        trayTestItem.Click += (_, _) => OpenTestWindow();
        traySoundItem.Click += (_, _) => OpenSoundSettings();
        trayHelpItem.Click += (_, _) => ShowHelp();
        trayExitItem.Click += (_, _) => ExitApplication();
        notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
        sourceMeter.Click += (_, _) => OpenSoundSettings();
        firstMeter.Click += (_, _) => OpenSoundSettings();
        secondMeter.Click += (_, _) => OpenSoundSettings();
        sourceBox.SelectedIndexChanged += (_, _) => UpdateFormatWarning();
        firstTargetBox.SelectedIndexChanged += (_, _) => UpdateFormatWarning();
        secondTargetBox.SelectedIndexChanged += (_, _) => UpdateFormatWarning();
        firstGainBox.ValueChanged += (_, _) => PushLiveSettings();
        secondGainBox.ValueChanged += (_, _) => PushLiveSettings();
        firstDelayBox.ValueChanged += (_, _) => PushLiveSettings();
        secondDelayBox.ValueChanged += (_, _) => PushLiveSettings();
        splitLeftRightBox.CheckedChanged += (_, _) =>
        {
            if (menuSplitItem.Checked != splitLeftRightBox.Checked)
            {
                menuSplitItem.Checked = splitLeftRightBox.Checked;
            }
            PushLiveSettings();
        };
        statsTimer.Tick += (_, _) => UpdateStatus();
        meterTimer.Tick += (_, _) => UpdateMeters();
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                HideToTray();
            }
        };
    }

    private void RefreshDevices()
    {
        devices = CoreAudio.GetRenderDevices();
        FillDeviceBox(sourceBox);
        FillDeviceBox(firstTargetBox);
        FillDeviceBox(secondTargetBox);
        ApplySettingsToControls();
        UpdateFormatWarning();
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
        splitLeftRightBox.Checked = settings.SplitLeftRight;
        menuSplitItem.Checked = settings.SplitLeftRight;
        UpdateCommandState();
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
                (int)secondDelayBox.Value,
                splitLeftRightBox.Checked);

            startButton.Enabled = false;
            stopButton.Enabled = true;
            statsTimer.Start();
            meterTimer.Start();
            UpdateStatus();
            UpdateCommandState();
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
        meterTimer.Stop();
        engine?.Dispose();
        engine = null;
        UpdateMeters();
        startButton.Enabled = true;
        stopButton.Enabled = false;
        UpdateStatus();
        UpdateCommandState();
    }

    private void PushLiveSettings()
    {
        engine?.UpdateSettings(
            (double)firstGainBox.Value,
            (double)secondGainBox.Value,
            (int)firstDelayBox.Value,
            (int)secondDelayBox.Value,
            splitLeftRightBox.Checked);
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
            SecondDelayMs = (int)secondDelayBox.Value,
            SplitLeftRight = splitLeftRightBox.Checked
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
            UpdateMeters();
            return;
        }

        var error = engine.LastError is null ? "" : $"{Environment.NewLine}Error: {engine.LastError.Message}";
        statusLabel.Text =
            $"Running: {engine.SourceName} -> {engine.FirstTargetName}, {engine.SecondTargetName}{Environment.NewLine}" +
            $"Format: {engine.Format.SampleRate} Hz, {engine.Format.Channels} ch, {engine.Format.Bits} bit. Mode: {(splitLeftRightBox.Checked ? "Split L/R" : "Stereo mirror")}{Environment.NewLine}" +
            $"Packets {engine.Packets}, captured {engine.CapturedFrames}, T1 written {engine.FirstWrittenFrames}, dropped {engine.FirstDroppedFrames}, T2 written {engine.SecondWrittenFrames}, dropped {engine.SecondDroppedFrames}" +
            error;
        UpdateMeters();
    }

    private void UpdateMeters()
    {
        if (engine is null)
        {
            sourceMeter.Level = 0;
            firstMeter.Level = 0;
            secondMeter.Level = 0;
            return;
        }

        sourceMeter.Level = engine.SourceLevel;
        firstMeter.Level = engine.FirstLevel;
        secondMeter.Level = engine.SecondLevel;
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

    private void UpdateCommandState()
    {
        var running = engine is not null;
        startButton.Enabled = !running;
        stopButton.Enabled = running;
        menuStartItem.Enabled = !running;
        menuStopItem.Enabled = running;
        trayStartItem.Enabled = !running;
        trayStopItem.Enabled = running;
        notifyIcon.Text = running ? "AudioMirror - running" : "AudioMirror - stopped";
    }

    private void UpdateFormatWarning()
    {
        try
        {
            if (sourceBox.SelectedItem is not AudioDeviceInfo source ||
                firstTargetBox.SelectedItem is not AudioDeviceInfo firstTarget ||
                secondTargetBox.SelectedItem is not AudioDeviceInfo secondTarget)
            {
                formatLabel.Text = "Select source and target devices to check formats.";
                formatLabel.BackColor = SystemColors.Control;
                return;
            }

            var sourceFormat = CoreAudio.GetMixFormat(source.Device);
            var firstFormat = CoreAudio.GetMixFormat(firstTarget.Device);
            var secondFormat = CoreAudio.GetMixFormat(secondTarget.Device);
            var targetsMatch = firstFormat.Matches(secondFormat);
            var allMatch = sourceFormat.Matches(firstFormat) && sourceFormat.Matches(secondFormat);
            var sourceMatchesTargets = sourceFormat.Matches(firstFormat) || sourceFormat.Matches(secondFormat);

            SetFormatMeter(sourceMeter, sourceMatchesTargets || allMatch, $"Source: {sourceFormat.DisplayName}");
            SetFormatMeter(firstMeter, firstFormat.Matches(sourceFormat) && targetsMatch, $"Target 1: {firstFormat.DisplayName}");
            SetFormatMeter(secondMeter, secondFormat.Matches(sourceFormat) && targetsMatch, $"Target 2: {secondFormat.DisplayName}");

            formatLabel.Text =
                (allMatch
                    ? $"Formats match: {sourceFormat.DisplayName}"
                    : targetsMatch
                        ? $"Targets match: {firstFormat.DisplayName}. Source differs: {sourceFormat.DisplayName}; Windows will resample."
                        : $"Target formats differ. T1 {firstFormat.DisplayName}; T2 {secondFormat.DisplayName}. Set both targets to 48000 Hz 16/24 bit.");

            formatLabel.BackColor = SystemColors.Control;
            formatLabel.ForeColor = allMatch || targetsMatch ? Color.FromArgb(20, 110, 45) : Color.FromArgb(160, 95, 0);
        }
        catch (Exception ex)
        {
            SetFormatMeter(sourceMeter, false, "Could not read format");
            SetFormatMeter(firstMeter, false, "Could not read format");
            SetFormatMeter(secondMeter, false, "Could not read format");
            formatLabel.Text = $"Could not read device formats: {ex.Message}";
            formatLabel.BackColor = SystemColors.Control;
            formatLabel.ForeColor = Color.FromArgb(170, 40, 40);
        }
    }

    private static void SetFormatMeter(LevelMeter meter, bool ok, string tooltipText)
    {
        meter.StatusColor = ok ? Color.FromArgb(45, 170, 80) : Color.FromArgb(230, 175, 45);
        ToolTipProvider.SetToolTip(meter, tooltipText + ". Click to open Windows sound settings.");
    }

    private void SyncAppSettings()
    {
        firstGainBox.Value = Math.Min(firstGainBox.Value, 1.0M);
        secondGainBox.Value = Math.Min(secondGainBox.Value, 1.0M);
        PushLiveSettings();
        UpdateFormatWarning();
        statusLabel.Text = "App sync applied: both targets use the source stream format internally. Change Windows device formats manually if the format lights are still amber.";
    }

    private static void OpenSoundSettings()
    {
        Process.Start(new ProcessStartInfo("ms-settings:sound")
        {
            UseShellExecute = true
        });
    }

    private void OpenTestWindow()
    {
        try
        {
            using var form = new TestForm(SelectedDevice(firstTargetBox), SelectedDevice(secondTargetBox));
            if (Visible)
            {
                form.ShowDialog(this);
            }
            else
            {
                form.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Test failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowHelp()
    {
        using var form = new InfoForm(
            "AudioMirror Help",
            "Help",
            """
            Quick setup

            1. In Windows sound settings, choose a silent or unused playback device as the default output. Realtek Digital Output is often a good source.
            2. Restart the browser or player if captured frames stay at 0.
            3. In AudioMirror, choose that device as Source.
            4. Choose the two real speakers or monitors as Target 1 and Target 2.
            5. Press Start.

            Controls

            Gain
            Volume multiplier for each target. 1.0 means unchanged. Start with 1.0 and raise Windows or monitor volume first. High gain can distort.

            Delay ms
            Adds delay to a target. Use it to reduce echo between two devices. Start at 0/0, then try small steps. If echo gets worse, put delay on the other target.

            Split L/R
            Sends the source left channel to Target 1 as dual-mono and the source right channel to Target 2 as dual-mono. Turn it off for normal stereo mirroring.

            Format lights
            Green means the selected device format is aligned with the current setup. Amber means there may be resampling or mismatched target formats. Click a light to open Windows sound settings.

            Sync
            Applies safe app-side defaults. It does not change Windows driver settings.

            Test
            Opens a built-in speaker test. Left plays Target 1, Right plays Target 2, Both plays both targets, and Loop cycles through them.

            Save
            Writes settings.json next to the app.

            Autostart
            Registers AudioMirror in the current user's Windows startup and starts mirroring automatically with saved settings.

            Troubleshooting

            If captured frames stay at 0, the selected Source is not receiving audio. Set it as the Windows default output and restart the player.

            If sound is metallic, set both physical targets to the same Windows format, for example 48000 Hz, 16 bit or 24 bit, and disable audio enhancements/spatial sound.

            If one target is silent, use Test first. If Test is also silent, check the target device volume, monitor audio source, mute state, and cable/input.
            """);
        if (Visible)
        {
            form.ShowDialog(this);
        }
        else
        {
            form.ShowDialog();
        }
    }

    private void ShowAbout()
    {
        using var form = new InfoForm(
            "About AudioMirror",
            "AudioMirror",
            """
            AudioMirror

            A small Windows WASAPI utility for mirroring one playback stream to two output devices with per-target gain, delay, channel split, and built-in speaker testing.

            Copyright (c) 2026 AudioMirror contributors

            Repository
            https://github.com/rausNT/audio-mirror-windows

            License
            MIT License

            Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the software, subject to the license terms.

            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
            """);
        if (Visible)
        {
            form.ShowDialog(this);
        }
        else
        {
            form.ShowDialog();
        }
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        notifyIcon.Visible = true;
    }

    private void ShowMainWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        allowExit = true;
        Close();
    }

    private static readonly ToolTip ToolTipProvider = new();
}
