namespace AudioMirrorApp;

internal sealed class SetupWizardForm : Form
{
    private readonly IReadOnlyList<AudioDeviceInfo> devices;
    private readonly ComboBox sourceBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox firstTargetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox secondTargetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckBox thirdTargetEnabledBox = new() { AutoSize = true };
    private readonly ComboBox thirdTargetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly Label titleLabel = new() { AutoSize = true };
    private readonly Label bodyLabel = new() { AutoSize = true, MaximumSize = new Size(580, 0) };
    private readonly Button backButton = new() { Width = 90 };
    private readonly Button nextButton = new() { Width = 110 };
    private readonly Button cancelButton = new() { Width = 90 };
    private readonly Button soundButton = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
    private readonly Button testButton = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
    private readonly Button startButton = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
    private readonly Panel stepPanel = new() { Dock = DockStyle.Fill };
    private int step;

    public SetupWizardForm(
        IReadOnlyList<AudioDeviceInfo> devices,
        string? sourceId,
        string? firstTargetId,
        string? secondTargetId,
        string? thirdTargetId,
        bool thirdTargetEnabled)
    {
        this.devices = devices;
        Text = AppText.T("SetupWizardTitle");
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(660, 430);
        MinimumSize = new Size(560, 380);

        foreach (var device in devices)
        {
            sourceBox.Items.Add(device);
            firstTargetBox.Items.Add(device);
            secondTargetBox.Items.Add(device);
            thirdTargetBox.Items.Add(device);
        }

        SelectDevice(sourceBox, sourceId);
        SelectDevice(firstTargetBox, firstTargetId);
        SelectDevice(secondTargetBox, secondTargetId);
        SelectDevice(thirdTargetBox, thirdTargetId);
        thirdTargetEnabledBox.Checked = thirdTargetEnabled;
        thirdTargetBox.Enabled = thirdTargetEnabled;

        BuildLayout();
        ShowStep(0);
    }

    public string? SourceDeviceId => SelectedDevice(sourceBox)?.Id;
    public string? FirstTargetDeviceId => SelectedDevice(firstTargetBox)?.Id;
    public string? SecondTargetDeviceId => SelectedDevice(secondTargetBox)?.Id;
    public string? ThirdTargetDeviceId => ThirdTargetEnabled ? SelectedDevice(thirdTargetBox)?.Id : null;
    public bool ThirdTargetEnabled => thirdTargetEnabledBox.Checked;
    public bool StartAfterClose { get; private set; }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        titleLabel.Font = new Font(Font.FontFamily, 15, FontStyle.Bold);
        titleLabel.Margin = new Padding(0, 0, 0, 12);
        bodyLabel.Margin = new Padding(0, 0, 0, 12);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 8)
        };
        soundButton.Text = AppText.T("SoundSettings");
        testButton.Text = AppText.T("TestSpeakers");
        startButton.Text = AppText.T("Start");
        actionPanel.Controls.AddRange([soundButton, testButton, startButton]);

        var navPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        cancelButton.Text = AppText.T("Close");
        backButton.Text = AppText.T("Back");
        nextButton.Text = AppText.T("Next");
        navPanel.Controls.AddRange([cancelButton, nextButton, backButton]);

        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(stepPanel, 0, 1);
        root.Controls.Add(actionPanel, 0, 2);
        root.Controls.Add(navPanel, 0, 3);
        Controls.Add(root);

        backButton.Click += (_, _) => ShowStep(step - 1);
        nextButton.Click += (_, _) =>
        {
            if (!ValidateStep())
            {
                return;
            }

            if (step < 2)
            {
                ShowStep(step + 1);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        soundButton.Click += (_, _) => MainForm.OpenSoundSettings();
        testButton.Click += (_, _) => TryOpenTest();
        startButton.Click += (_, _) =>
        {
            if (!ValidateAll())
            {
                return;
            }

            StartAfterClose = true;
            DialogResult = DialogResult.OK;
            Close();
        };
        thirdTargetEnabledBox.CheckedChanged += (_, _) => thirdTargetBox.Enabled = thirdTargetEnabledBox.Checked;
    }

    private void ShowStep(int nextStep)
    {
        step = Math.Clamp(nextStep, 0, 2);
        stepPanel.Controls.Clear();

        if (step == 0)
        {
            titleLabel.Text = AppText.T("WizardStepSourceTitle");
            bodyLabel.Text = AppText.T("WizardStepSourceBody");
            stepPanel.Controls.Add(BuildSourceStep());
        }
        else if (step == 1)
        {
            titleLabel.Text = AppText.T("WizardStepTargetsTitle");
            bodyLabel.Text = AppText.T("WizardStepTargetsBody");
            stepPanel.Controls.Add(BuildTargetsStep());
        }
        else
        {
            titleLabel.Text = AppText.T("WizardStepTestTitle");
            bodyLabel.Text = AppText.T("WizardStepTestBody");
            stepPanel.Controls.Add(BuildSummaryStep());
        }

        backButton.Enabled = step > 0;
        nextButton.Text = step == 2 ? AppText.T("Finish") : AppText.T("Next");
        testButton.Enabled = step == 2;
        startButton.Enabled = step == 2;
    }

    private Control BuildSourceStep()
    {
        return BuildGrid([
            (AppText.T("Source"), (Control)sourceBox)
        ]);
    }

    private Control BuildTargetsStep()
    {
        return BuildGrid([
            (AppText.T("Target1"), (Control)firstTargetBox),
            (AppText.T("Target2"), (Control)secondTargetBox),
            (AppText.T("Target3"), (Control)thirdTargetEnabledBox),
            ("", (Control)thirdTargetBox)
        ]);
    }

    private Control BuildSummaryStep()
    {
        var summary = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Text = BuildSummaryText(),
            Font = new Font(FontFamily.GenericSansSerif, 10)
        };
        return summary;
    }

    private Control BuildGrid((string Label, Control Control)[] rows)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rows.Length + 1
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(bodyLabel, 0, 0);
        grid.SetColumnSpan(bodyLabel, 2);

        for (var i = 0; i < rows.Length; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(new Label { Text = rows[i].Label, AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, i + 1);
            rows[i].Control.Margin = new Padding(0, 3, 0, 3);
            grid.Controls.Add(rows[i].Control, 1, i + 1);
        }

        return grid;
    }

    private string BuildSummaryText()
    {
        var third = ThirdTargetEnabled
            ? $"{Environment.NewLine}{AppText.T("Target3")}: {SelectedDevice(thirdTargetBox)?.Name ?? "-"}"
            : "";
        return
            $"{AppText.T("Source")}: {SelectedDevice(sourceBox)?.Name ?? "-"}{Environment.NewLine}" +
            $"{AppText.T("Target1")}: {SelectedDevice(firstTargetBox)?.Name ?? "-"}{Environment.NewLine}" +
            $"{AppText.T("Target2")}: {SelectedDevice(secondTargetBox)?.Name ?? "-"}" +
            third;
    }

    private bool ValidateStep()
    {
        return step switch
        {
            0 => SelectedDevice(sourceBox) is not null || ShowValidation(AppText.T("WizardSelectSource")),
            1 => ValidateTargets(),
            _ => ValidateAll()
        };
    }

    private bool ValidateAll()
    {
        return SelectedDevice(sourceBox) is not null && ValidateTargets();
    }

    private bool ValidateTargets()
    {
        var source = SelectedDevice(sourceBox);
        var first = SelectedDevice(firstTargetBox);
        var second = SelectedDevice(secondTargetBox);
        var third = ThirdTargetEnabled ? SelectedDevice(thirdTargetBox) : null;
        if (source is null || first is null || second is null || ThirdTargetEnabled && third is null)
        {
            return ShowValidation(AppText.T("WizardSelectTargets"));
        }

        var ids = new[] { source.Id, first.Id, second.Id, third?.Id }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();
        if (ids.Length != ids.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            return ShowValidation(AppText.T("WizardDistinctDevices"));
        }

        return true;
    }

    private bool ShowValidation(string message)
    {
        MessageBox.Show(this, message, AppText.T("SetupWizardTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private void TryOpenTest()
    {
        if (!ValidateAll())
        {
            return;
        }

        using var form = new TestForm(
            SelectedDevice(firstTargetBox)!,
            SelectedDevice(secondTargetBox)!,
            ThirdTargetEnabled ? SelectedDevice(thirdTargetBox) : null);
        form.ShowDialog(this);
    }

    private static AudioDeviceInfo? SelectedDevice(ComboBox box)
    {
        return box.SelectedItem as AudioDeviceInfo;
    }

    private void SelectDevice(ComboBox box, string? deviceId)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is AudioDeviceInfo device &&
                !string.IsNullOrWhiteSpace(deviceId) &&
                string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                box.SelectedIndex = i;
                return;
            }
        }

        if (box == sourceBox && TrySelectDefault(box))
        {
            return;
        }

        if (TrySelectFirstActive(box))
        {
            return;
        }

        if (box.Items.Count > 0)
        {
            box.SelectedIndex = 0;
        }
    }

    private static bool TrySelectDefault(ComboBox box)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is AudioDeviceInfo { IsDefault: true })
            {
                box.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TrySelectFirstActive(ComboBox box)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is AudioDeviceInfo { IsActive: true })
            {
                box.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }
}
