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
    private readonly Label sourceLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) };
    private readonly Label firstTargetLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) };
    private readonly Label secondTargetLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 6, 0, 0) };
    private readonly Label gainLabel = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label delayLabel = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label hintLabel = new() { AutoSize = true, Padding = new Padding(0, 0, 0, 8) };
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
    private readonly CheckBox autoRestartBox = new() { Text = "Auto restart", AutoSize = true, Checked = true };
    private readonly Label formatLabel = new() { AutoSize = false, Height = 42, Dock = DockStyle.Fill };
    private readonly Label statusLabel = new() { AutoSize = false, Height = 76, Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer statsTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer meterTimer = new() { Interval = 60 };
    private readonly System.Windows.Forms.Timer watchdogTimer = new() { Interval = 2000 };
    private readonly System.Windows.Forms.Timer deviceRefreshTimer = new() { Interval = 2000 };
    private readonly MenuStrip menuStrip = new();
    private readonly ToolStripMenuItem fileMenu = new("&File");
    private readonly ToolStripMenuItem actionsMenu = new("&Actions");
    private readonly ToolStripMenuItem helpMenu = new("&Help");
    private readonly ToolStripMenuItem languageMenu = new("&Language");
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
    private readonly List<ToolStripMenuItem> languageItems = [];
    private AppSettings settings;
    private readonly bool startAfterShown;
    private bool allowExit;
    private bool restarting;
    private bool restartAfterDeviceRefresh;
    private int pendingDeviceRefreshTicks;
    private string deviceRefreshReasonKey = "DeviceChange";
    private long lastWatchdogCapturedFrames;
    private long lastWatchdogWrittenFrames;
    private int stalledWatchdogTicks;
    private IReadOnlyList<AudioDeviceInfo> devices = [];
    private WasapiMirrorEngine? engine;

    public MainForm(bool startAfterShown)
    {
        this.startAfterShown = startAfterShown;
        Text = $"AudioMirror {AppVersion.Display}";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 420);
        MinimumSize = new Size(760, 430);

        settings = SettingsStore.Load();
        AppText.SetLanguage(settings.LanguageCode);
        BuildLayout();
        BuildTrayMenu();
        WireEvents();
        ApplyLocalization();
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
        Microsoft.Win32.SystemEvents.PowerModeChanged -= SystemEventsPowerModeChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEventsDisplaySettingsChanged;
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        const int wmDeviceChange = 0x0219;
        base.WndProc(ref m);

        if (m.Msg == wmDeviceChange)
        {
            ScheduleDeviceRefresh("DeviceChange", engine is not null);
        }
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

        AddRow(grid, 0, sourceMeter, sourceLabel, sourceBox, null, null);
        AddRow(grid, 1, firstMeter, firstTargetLabel, firstTargetBox, firstGainBox, firstDelayBox);
        AddRow(grid, 2, secondMeter, secondTargetLabel, secondTargetBox, secondGainBox, secondDelayBox);

        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 3);
        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 1, 3);
        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 2, 3);
        grid.Controls.Add(gainLabel, 3, 3);
        grid.Controls.Add(delayLabel, 4, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 12, 0, 8)
        };
        buttons.Controls.AddRange([refreshButton, startButton, stopButton, saveButton, startupButton, soundSettingsButton, syncButton, testButton, splitLeftRightBox, autoRestartBox]);

        statusLabel.BorderStyle = BorderStyle.FixedSingle;
        statusLabel.Padding = new Padding(8);
        formatLabel.Padding = new Padding(4, 6, 4, 4);

        root.Controls.Add(grid, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        root.Controls.Add(hintLabel, 0, 2);
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

        BuildLanguageMenu();

        helpMenu.DropDownItems.AddRange([
            menuHelpItem,
            menuAboutItem
        ]);

        menuStrip.Items.AddRange([fileMenu, actionsMenu, languageMenu, helpMenu]);
    }

    private void BuildLanguageMenu()
    {
        foreach (var language in AppText.Options)
        {
            var item = new ToolStripMenuItem(language.Name)
            {
                Tag = language.Code,
                CheckOnClick = false
            };
            item.Click += (_, _) => SetLanguage(language.Code);
            languageItems.Add(item);
            languageMenu.DropDownItems.Add(item);
        }
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

    private static void AddRow(TableLayoutPanel grid, int row, LevelMeter meter, Label label, Control deviceControl, Control? gainControl, Control? delayControl)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        meter.Margin = new Padding(0, 5, 6, 0);
        grid.Controls.Add(meter, 0, row);
        grid.Controls.Add(label, 1, row);
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
        watchdogTimer.Tick += (_, _) => WatchdogTick();
        deviceRefreshTimer.Tick += (_, _) => DeviceRefreshTick();
        Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEventsPowerModeChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEventsDisplaySettingsChanged;
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                HideToTray();
            }
        };
    }

    private void SetLanguage(string code)
    {
        AppText.SetLanguage(code);
        settings.LanguageCode = AppText.CurrentCode;
        try
        {
            settings = ReadSettingsFromControls();
            settings.LanguageCode = AppText.CurrentCode;
        }
        catch
        {
            // Language can still be changed while devices are temporarily unavailable.
        }

        SettingsStore.Save(settings);
        ApplyLocalization();
        RefreshDevices(preserveCurrentSelection: true, updateStatus: false);
        UpdateStatus();
    }

    private void ApplyLocalization()
    {
        fileMenu.Text = AppText.T("File");
        actionsMenu.Text = AppText.T("Actions");
        languageMenu.Text = AppText.T("Language");
        helpMenu.Text = AppText.T("Help");
        menuStartItem.Text = AppText.T("Start");
        menuStopItem.Text = AppText.T("Stop");
        menuSaveItem.Text = AppText.T("SaveSettings");
        menuAutostartItem.Text = AppText.T("Autostart");
        menuExitItem.Text = AppText.T("Exit");
        menuRefreshItem.Text = AppText.T("RefreshDevices");
        menuSoundItem.Text = AppText.T("SoundSettings");
        menuSyncItem.Text = AppText.T("Sync");
        menuTestItem.Text = AppText.T("TestSpeakers");
        menuSplitItem.Text = AppText.T("SplitLR");
        menuHelpItem.Text = AppText.T("UserHelp");
        menuAboutItem.Text = AppText.T("AboutAudioMirror");

        trayOpenItem.Text = AppText.T("OpenAudioMirror");
        trayStartItem.Text = AppText.T("Start");
        trayStopItem.Text = AppText.T("Stop");
        trayTestItem.Text = AppText.T("TestSpeakers");
        traySoundItem.Text = AppText.T("SoundSettings");
        trayHelpItem.Text = AppText.T("Help");
        trayExitItem.Text = AppText.T("Exit");

        refreshButton.Text = AppText.T("Refresh");
        startButton.Text = AppText.T("Start");
        stopButton.Text = AppText.T("Stop");
        saveButton.Text = AppText.T("Save");
        startupButton.Text = AppText.T("Autostart");
        soundSettingsButton.Text = AppText.T("Sound");
        syncButton.Text = AppText.T("Sync");
        testButton.Text = AppText.T("Test");
        splitLeftRightBox.Text = AppText.T("SplitLR");
        autoRestartBox.Text = AppText.T("AutoRestart");

        sourceLabel.Text = AppText.T("Source");
        firstTargetLabel.Text = AppText.T("Target1");
        secondTargetLabel.Text = AppText.T("Target2");
        gainLabel.Text = AppText.T("Gain");
        delayLabel.Text = AppText.T("DelayMs");
        hintLabel.Text = AppText.T("Hint");

        foreach (var item in languageItems)
        {
            item.Checked = string.Equals(item.Tag as string, AppText.CurrentCode, StringComparison.OrdinalIgnoreCase);
        }

        FitCommandButton(refreshButton);
        FitCommandButton(startButton);
        FitCommandButton(stopButton);
        FitCommandButton(saveButton);
        FitCommandButton(startupButton);
        FitCommandButton(soundSettingsButton);
        FitCommandButton(syncButton);
        FitCommandButton(testButton);
        UpdateCommandState();
    }

    private static void FitCommandButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(72, 28);
        button.MaximumSize = new Size(126, 0);
    }

    private void RefreshDevices()
    {
        RefreshDevices(preserveCurrentSelection: true, updateStatus: true);
    }

    private void RefreshDevices(bool preserveCurrentSelection, bool updateStatus)
    {
        var currentSourceId = preserveCurrentSelection ? SelectedDeviceId(sourceBox) : null;
        var currentFirstId = preserveCurrentSelection ? SelectedDeviceId(firstTargetBox) : null;
        var currentSecondId = preserveCurrentSelection ? SelectedDeviceId(secondTargetBox) : null;
        if (!DistinctDeviceIds(currentSourceId, currentFirstId, currentSecondId))
        {
            currentSourceId = null;
            currentFirstId = null;
            currentSecondId = null;
        }

        devices = CoreAudio.GetRenderDevices(PinnedDeviceIds(currentSourceId, currentFirstId, currentSecondId));
        FillDeviceBox(sourceBox);
        FillDeviceBox(firstTargetBox);
        FillDeviceBox(secondTargetBox);
        ApplySettingsToControls(currentSourceId, currentFirstId, currentSecondId);
        UpdateFormatWarning();
        if (updateStatus)
        {
            statusLabel.Text = AppText.F("DevicesRefreshed", devices.Count);
        }
    }

    private void FillDeviceBox(ComboBox box)
    {
        box.Items.Clear();
        foreach (var device in devices)
        {
            box.Items.Add(device);
        }
    }

    private IEnumerable<string?> PinnedDeviceIds(string? preferredSourceId, string? preferredFirstId, string? preferredSecondId)
    {
        yield return preferredSourceId;
        yield return preferredFirstId;
        yield return preferredSecondId;
        yield return settings.SourceDeviceId;
        yield return settings.FirstTargetDeviceId;
        yield return settings.SecondTargetDeviceId;
    }

    private void ApplySettingsToControls(string? preferredSourceId = null, string? preferredFirstId = null, string? preferredSecondId = null)
    {
        SelectDevice(sourceBox, preferredSourceId, settings.SourceDeviceId, settings.SourceDeviceName, settings.SourceIndex);
        SelectDevice(firstTargetBox, preferredFirstId, settings.FirstTargetDeviceId, settings.FirstTargetDeviceName, settings.FirstTargetIndex);
        SelectDevice(secondTargetBox, preferredSecondId, settings.SecondTargetDeviceId, settings.SecondTargetDeviceName, settings.SecondTargetIndex);
        firstGainBox.Value = ClampDecimal((decimal)settings.FirstGain, firstGainBox.Minimum, firstGainBox.Maximum);
        secondGainBox.Value = ClampDecimal((decimal)settings.SecondGain, secondGainBox.Minimum, secondGainBox.Maximum);
        firstDelayBox.Value = ClampDecimal(settings.FirstDelayMs, firstDelayBox.Minimum, firstDelayBox.Maximum);
        secondDelayBox.Value = ClampDecimal(settings.SecondDelayMs, secondDelayBox.Minimum, secondDelayBox.Maximum);
        splitLeftRightBox.Checked = settings.SplitLeftRight;
        autoRestartBox.Checked = settings.AutoRestart;
        menuSplitItem.Checked = settings.SplitLeftRight;
        UpdateCommandState();
    }

    private static decimal ClampDecimal(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static void SelectDevice(ComboBox box, string? preferredId, string? savedId, string? savedName, int savedIndex)
    {
        if (TrySelectById(box, preferredId) ||
            TrySelectById(box, savedId) ||
            TrySelectByName(box, savedName) ||
            TrySelectByIndex(box, savedIndex))
        {
            return;
        }

        if (box.Items.Count > 0)
        {
            box.SelectedIndex = 0;
        }
        else
        {
            box.SelectedIndex = -1;
        }
    }

    private static bool TrySelectById(ComboBox box, string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is AudioDeviceInfo device && string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                box.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TrySelectByName(ComboBox box, string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return false;
        }

        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is AudioDeviceInfo device && string.Equals(device.Name, deviceName, StringComparison.OrdinalIgnoreCase))
            {
                box.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TrySelectByIndex(ComboBox box, int deviceIndex)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is AudioDeviceInfo device && device.Index == deviceIndex)
            {
                box.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private static string? SelectedDeviceId(ComboBox box)
    {
        return box.SelectedItem is AudioDeviceInfo device ? device.Id : null;
    }

    private static bool DistinctDeviceIds(string? first, string? second, string? third)
    {
        if (string.IsNullOrWhiteSpace(first) ||
            string.IsNullOrWhiteSpace(second) ||
            string.IsNullOrWhiteSpace(third))
        {
            return false;
        }

        return !string.Equals(first, second, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(first, third, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(second, third, StringComparison.OrdinalIgnoreCase);
    }

    private void StartMirror()
    {
        try
        {
            StopMirror();
            var source = SelectedDevice(sourceBox);
            var firstTarget = SelectedDevice(firstTargetBox);
            var secondTarget = SelectedDevice(secondTargetBox);
            EnsureActive(source, AppText.T("Source"));
            EnsureActive(firstTarget, AppText.T("Target1"));
            EnsureActive(secondTarget, AppText.T("Target2"));
            settings = ReadSettingsFromControls();
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
            watchdogTimer.Start();
            ResetWatchdog();
            UpdateStatus();
            UpdateCommandState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppText.T("StartFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            StopMirror();
        }
    }

    private void StopMirror()
    {
        statsTimer.Stop();
        meterTimer.Stop();
        watchdogTimer.Stop();
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
        settings = snapshot;
        SettingsStore.Save(snapshot);
        statusLabel.Text = AppText.F("Saved", SettingsStore.SettingsPath);
    }

    private AppSettings ReadSettingsFromControls()
    {
        return new AppSettings
        {
            SourceIndex = SelectedDevice(sourceBox).Index,
            FirstTargetIndex = SelectedDevice(firstTargetBox).Index,
            SecondTargetIndex = SelectedDevice(secondTargetBox).Index,
            SourceDeviceId = SelectedDevice(sourceBox).Id,
            FirstTargetDeviceId = SelectedDevice(firstTargetBox).Id,
            SecondTargetDeviceId = SelectedDevice(secondTargetBox).Id,
            SourceDeviceName = SelectedDevice(sourceBox).Name,
            FirstTargetDeviceName = SelectedDevice(firstTargetBox).Name,
            SecondTargetDeviceName = SelectedDevice(secondTargetBox).Name,
            FirstGain = (double)firstGainBox.Value,
            SecondGain = (double)secondGainBox.Value,
            FirstDelayMs = (int)firstDelayBox.Value,
            SecondDelayMs = (int)secondDelayBox.Value,
            SplitLeftRight = splitLeftRightBox.Checked,
            AutoRestart = autoRestartBox.Checked,
            LanguageCode = settings.LanguageCode
        };
    }

    private AudioDeviceInfo SelectedDevice(ComboBox box)
    {
        if (box.SelectedItem is AudioDeviceInfo device)
        {
            return device;
        }

        throw new InvalidOperationException(AppText.T("SelectAllDevices"));
    }

    private static void EnsureActive(AudioDeviceInfo device, string role)
    {
        if (!device.IsActive)
        {
            throw new InvalidOperationException(AppText.F("EnsureActive", role, device.Name));
        }
    }

    private void UpdateStatus()
    {
        if (engine is null)
        {
            statusLabel.Text = AppText.T("Stopped");
            UpdateMeters();
            return;
        }

        var error = engine.LastError is null ? "" : $"{Environment.NewLine}{AppText.F("ErrorLine", engine.LastError.Message)}";
        statusLabel.Text =
            AppText.F("RunningLine", engine.SourceName, engine.FirstTargetName, engine.SecondTargetName) + Environment.NewLine +
            AppText.F("FormatLine", engine.Format.SampleRate, engine.Format.Channels, engine.Format.Bits, splitLeftRightBox.Checked ? AppText.T("ModeSplit") : AppText.T("ModeStereo")) + Environment.NewLine +
            AppText.F("PacketsLine", engine.Packets, engine.CapturedFrames, engine.FirstWrittenFrames, engine.FirstDroppedFrames, engine.SecondWrittenFrames, engine.SecondDroppedFrames) +
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
            statusLabel.Text = AppText.T("AutostartRegistered");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppText.T("AutostartFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        notifyIcon.Text = running ? AppText.T("NotifyRunning") : AppText.T("NotifyStopped");
    }

    private void WatchdogTick()
    {
        if (engine is null || !autoRestartBox.Checked || restarting)
        {
            ResetWatchdog();
            return;
        }

        if (engine.LastError is not null)
        {
            RestartMirror("StreamError");
            return;
        }

        var captured = engine.CapturedFrames;
        var written = engine.FirstWrittenFrames + engine.SecondWrittenFrames;
        var moved = captured != lastWatchdogCapturedFrames || written != lastWatchdogWrittenFrames;
        lastWatchdogCapturedFrames = captured;
        lastWatchdogWrittenFrames = written;

        if (moved)
        {
            stalledWatchdogTicks = 0;
            return;
        }

        stalledWatchdogTicks++;
        if (stalledWatchdogTicks >= 4)
        {
            RestartMirror("StreamStalled");
        }
    }

    private void ResetWatchdog()
    {
        lastWatchdogCapturedFrames = engine?.CapturedFrames ?? 0;
        lastWatchdogWrittenFrames = engine is null ? 0 : engine.FirstWrittenFrames + engine.SecondWrittenFrames;
        stalledWatchdogTicks = 0;
    }

    private void SystemEventsPowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
    {
        if (e.Mode == Microsoft.Win32.PowerModes.Suspend)
        {
            if (engine is not null)
            {
                restartAfterDeviceRefresh = true;
                StopMirror();
                statusLabel.Text = AppText.T("PausedSleep");
            }

            return;
        }

        if (e.Mode == Microsoft.Win32.PowerModes.Resume)
        {
            ScheduleDeviceRefresh("SystemResume", restartAfterDeviceRefresh || engine is not null);
        }
    }

    private void SystemEventsDisplaySettingsChanged(object? sender, EventArgs e)
    {
        ScheduleDeviceRefresh("DisplayChange", engine is not null);
    }

    private void ScheduleDeviceRefresh(string reasonKey, bool restartWhenReady)
    {
        deviceRefreshReasonKey = reasonKey;
        pendingDeviceRefreshTicks = 15;
        restartAfterDeviceRefresh |= restartWhenReady;
        deviceRefreshTimer.Stop();
        deviceRefreshTimer.Start();

        if (restartWhenReady)
        {
            statusLabel.Text = AppText.F("WaitingDevices", AppText.T(reasonKey));
        }
    }

    private void DeviceRefreshTick()
    {
        if (pendingDeviceRefreshTicks <= 0)
        {
            deviceRefreshTimer.Stop();
            restartAfterDeviceRefresh = false;
            return;
        }

        pendingDeviceRefreshTicks--;
        try
        {
            RefreshDevices(preserveCurrentSelection: !restartAfterDeviceRefresh, updateStatus: false);

            if (!restartAfterDeviceRefresh)
            {
                statusLabel.Text = AppText.F("DevicesRefreshedAfter", AppText.T(deviceRefreshReasonKey), devices.Count);
                return;
            }

            if (!SelectedDevicesReady())
            {
                statusLabel.Text = AppText.F("WaitingSelectedAfter", AppText.T(deviceRefreshReasonKey), devices.Count);
                return;
            }

            if (engine is not null)
            {
                StopMirror();
            }

            StartMirror();
            restartAfterDeviceRefresh = false;
            deviceRefreshTimer.Stop();
            statusLabel.Text = AppText.F("RestartedAfter", AppText.T(deviceRefreshReasonKey));
        }
        catch (Exception ex)
        {
            statusLabel.Text = AppText.F("DeviceRefreshFailed", AppText.T(deviceRefreshReasonKey), ex.Message);
        }
    }

    private bool SelectedDevicesReady()
    {
        if (sourceBox.SelectedItem is not AudioDeviceInfo source ||
            firstTargetBox.SelectedItem is not AudioDeviceInfo firstTarget ||
            secondTargetBox.SelectedItem is not AudioDeviceInfo secondTarget)
        {
            return false;
        }

        return source.IsActive &&
            firstTarget.IsActive &&
            secondTarget.IsActive &&
            !string.Equals(source.Id, firstTarget.Id, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(source.Id, secondTarget.Id, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(firstTarget.Id, secondTarget.Id, StringComparison.OrdinalIgnoreCase);
    }

    private async void RestartMirror(string reasonKey)
    {
        if (restarting)
        {
            return;
        }

        restarting = true;
        statusLabel.Text = AppText.F("Restarting", AppText.T(reasonKey));
        try
        {
            statsTimer.Stop();
            meterTimer.Stop();
            watchdogTimer.Stop();
            engine?.Dispose();
            engine = null;
            UpdateMeters();
            await Task.Delay(1200);

            RefreshDevices();
            StartMirror();
            statusLabel.Text = AppText.F("Restarted", AppText.T(reasonKey));
        }
        catch (Exception ex)
        {
            statusLabel.Text = AppText.F("AutoRestartFailed", ex.Message);
            startButton.Enabled = true;
            stopButton.Enabled = false;
            UpdateCommandState();
        }
        finally
        {
            restarting = false;
            ResetWatchdog();
        }
    }

    private void UpdateFormatWarning()
    {
        try
        {
            if (sourceBox.SelectedItem is not AudioDeviceInfo source ||
                firstTargetBox.SelectedItem is not AudioDeviceInfo firstTarget ||
                secondTargetBox.SelectedItem is not AudioDeviceInfo secondTarget)
            {
                formatLabel.Text = AppText.T("SelectDevices");
                formatLabel.BackColor = SystemColors.Control;
                return;
            }

            if (!source.IsActive || !firstTarget.IsActive || !secondTarget.IsActive)
            {
                SetFormatMeter(sourceMeter, source.IsActive, AppText.F("FormatTooltip", AppText.T("Source"), DeviceStateText(source)));
                SetFormatMeter(firstMeter, firstTarget.IsActive, AppText.F("FormatTooltip", AppText.T("Target1"), DeviceStateText(firstTarget)));
                SetFormatMeter(secondMeter, secondTarget.IsActive, AppText.F("FormatTooltip", AppText.T("Target2"), DeviceStateText(secondTarget)));
                formatLabel.Text = AppText.T("NotActiveSelected");
                formatLabel.BackColor = SystemColors.Control;
                formatLabel.ForeColor = Color.FromArgb(160, 95, 0);
                return;
            }

            var sourceFormat = CoreAudio.GetMixFormat(source.Device);
            var firstFormat = CoreAudio.GetMixFormat(firstTarget.Device);
            var secondFormat = CoreAudio.GetMixFormat(secondTarget.Device);
            var targetsMatch = firstFormat.Matches(secondFormat);
            var allMatch = sourceFormat.Matches(firstFormat) && sourceFormat.Matches(secondFormat);
            var sourceMatchesTargets = sourceFormat.Matches(firstFormat) || sourceFormat.Matches(secondFormat);

            SetFormatMeter(sourceMeter, sourceMatchesTargets || allMatch, AppText.F("FormatTooltip", AppText.T("Source"), sourceFormat.DisplayName));
            SetFormatMeter(firstMeter, firstFormat.Matches(sourceFormat) && targetsMatch, AppText.F("FormatTooltip", AppText.T("Target1"), firstFormat.DisplayName));
            SetFormatMeter(secondMeter, secondFormat.Matches(sourceFormat) && targetsMatch, AppText.F("FormatTooltip", AppText.T("Target2"), secondFormat.DisplayName));

            formatLabel.Text =
                (allMatch
                    ? AppText.F("FormatMatch", sourceFormat.DisplayName)
                    : targetsMatch
                        ? AppText.F("TargetsMatch", firstFormat.DisplayName, sourceFormat.DisplayName)
                        : AppText.F("TargetFormatsDiffer", firstFormat.DisplayName, secondFormat.DisplayName));

            formatLabel.BackColor = SystemColors.Control;
            formatLabel.ForeColor = allMatch || targetsMatch ? Color.FromArgb(20, 110, 45) : Color.FromArgb(160, 95, 0);
        }
        catch (Exception ex)
        {
            SetFormatMeter(sourceMeter, false, AppText.T("CouldNotReadFormatShort"));
            SetFormatMeter(firstMeter, false, AppText.T("CouldNotReadFormatShort"));
            SetFormatMeter(secondMeter, false, AppText.T("CouldNotReadFormatShort"));
            formatLabel.Text = AppText.F("CouldNotReadFormat", ex.Message);
            formatLabel.BackColor = SystemColors.Control;
            formatLabel.ForeColor = Color.FromArgb(170, 40, 40);
        }
    }

    private static void SetFormatMeter(LevelMeter meter, bool ok, string tooltipText)
    {
        meter.StatusColor = ok ? Color.FromArgb(45, 170, 80) : Color.FromArgb(230, 175, 45);
        ToolTipProvider.SetToolTip(meter, $"{tooltipText}. {AppText.T("OpenSoundTooltip")}");
    }

    private static string DeviceStateText(AudioDeviceInfo device)
    {
        if (device.IsActive)
        {
            return AppText.F("DeviceActive", device.Name);
        }

        if ((device.State & CoreAudio.DeviceStateUnplugged) != 0)
        {
            return AppText.F("DeviceUnplugged", device.Name);
        }

        if ((device.State & CoreAudio.DeviceStateNotPresent) != 0)
        {
            return AppText.F("DeviceNotPresent", device.Name);
        }

        return AppText.F("DeviceNotActive", device.Name);
    }

    private void SyncAppSettings()
    {
        firstGainBox.Value = Math.Min(firstGainBox.Value, 1.0M);
        secondGainBox.Value = Math.Min(secondGainBox.Value, 1.0M);
        PushLiveSettings();
        UpdateFormatWarning();
        statusLabel.Text = AppText.T("SyncApplied");
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
            MessageBox.Show(this, ex.Message, AppText.T("TestFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowHelp()
    {
        using var form = new InfoForm(
            AppText.T("HelpTitle"),
            AppText.T("HelpHeading"),
            AppText.T("HelpBody"));
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
            AppText.T("AboutTitle"),
            $"{AppText.T("AboutHeading")} {AppVersion.Display}",
            $"{AppText.T("Version")}: {AppVersion.Display}{Environment.NewLine}{Environment.NewLine}{AppText.T("AboutBody")}");
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
